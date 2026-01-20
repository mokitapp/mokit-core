using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mokit.Domain.Entities;
using Mokit.Domain.Enums;
using Mokit.HostManager.Management;
using Mokit.Application.Interfaces;

namespace Mokit.HostManager.Hosting;

public class MockHostInstance : IDisposable
{
    private WebApplication? _app;
    private readonly MockProject _project;
    private readonly ILogger<MockHostInstance> _logger;
    private readonly Action<MockRequestLog>? _onRequestLogged;
    private readonly IWebhookJobQueue _webhookQueue;
    private MockServerMiddleware? _middleware;
    private CancellationTokenSource? _cts;

    public Guid ProjectId => _project.Id;
    public int Port => _project.Port;
    public MockServerStatus Status { get; private set; } = MockServerStatus.Stopped;
    public DateTime? StartedAt { get; private set; }
    public long RequestCount { get; private set; }
    public long ErrorCount { get; private set; }
    public string? LastError { get; private set; }
    public DateTime? LastRequestAt { get; private set; }

    public MockHostInstance(
        MockProject project, 
        ILogger<MockHostInstance> logger,
        IWebhookJobQueue webhookQueue,
        Action<MockRequestLog>? onRequestLogged = null)
    {
        _project = project;
        _logger = logger;
        _webhookQueue = webhookQueue;
        _onRequestLogged = onRequestLogged;
    }

    public async Task<bool> StartAsync()
    {
        if (Status == MockServerStatus.Running)
        {
            return true;
        }

        try
        {
            Status = MockServerStatus.Starting;
            _cts = new CancellationTokenSource();

            var builder = WebApplication.CreateBuilder();
            
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(_project.Port);
            });

            builder.Logging.SetMinimumLevel(LogLevel.Warning);

            // Add CORS services if enabled
            if (_project.EnableCors)
            {
                builder.Services.AddCors();
            }

            _app = builder.Build();

            // Configure CORS if enabled
            if (_project.EnableCors)
            {
                _app.UseCors(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            }

            // Health check endpoint
            _app.MapGet("/__health", () => Results.Ok(new { status = "healthy", port = _project.Port }));

            // Create middleware
            var endpoints = _project.Endpoints.ToList();
            _middleware = new MockServerMiddleware(
                context => Task.CompletedTask,
                _project.Id,
                endpoints,
                _webhookQueue,
                OnRequestLogged
            );

            // Use middleware for all other requests
            _app.Use(async (context, next) =>
            {
                if (!context.Request.Path.StartsWithSegments("/__health"))
                {
                    await _middleware!.InvokeAsync(context);
                }
                else
                {
                    await next();
                }
            });

            // Start in background
            _ = Task.Run(async () =>
            {
                try
                {
                    await _app.StartAsync(_cts.Token);
                    await Task.Delay(Timeout.Infinite, _cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // Expected when stopping
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error running mock server on port {Port}", _project.Port);
                    LastError = ex.Message;
                    Status = MockServerStatus.Error;
                }
            });

            // Wait a moment for the server to start
            await Task.Delay(500);

            Status = MockServerStatus.Running;
            StartedAt = DateTime.UtcNow;
            
            _logger.LogInformation("Mock server started on port {Port} for project {ProjectName}", 
                _project.Port, _project.Name);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start mock server on port {Port}", _project.Port);
            LastError = ex.Message;
            Status = MockServerStatus.Error;
            return false;
        }
    }

    public async Task<bool> StopAsync()
    {
        if (Status != MockServerStatus.Running)
        {
            return true;
        }

        try
        {
            Status = MockServerStatus.Stopping;
            
            _cts?.Cancel();
            
            if (_app != null)
            {
                await _app.StopAsync();
                await _app.DisposeAsync();
                _app = null;
            }

            Status = MockServerStatus.Stopped;
            StartedAt = null;
            
            _logger.LogInformation("Mock server stopped on port {Port}", _project.Port);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop mock server on port {Port}", _project.Port);
            LastError = ex.Message;
            Status = MockServerStatus.Error;
            return false;
        }
    }

    public void UpdateEndpoints(IEnumerable<MockEndpoint> endpoints)
    {
        _middleware?.UpdateEndpoints(endpoints.ToList());
    }

    private void OnRequestLogged(MockRequestLog log)
    {
        RequestCount++;
        LastRequestAt = log.Timestamp;
        
        if (log.StatusCode >= 400)
        {
            ErrorCount++;
        }

        _onRequestLogged?.Invoke(log);
    }

    public MockServerInfo GetInfo()
    {
        return new MockServerInfo
        {
            ProjectId = _project.Id,
            ProjectName = _project.Name,
            Port = _project.Port,
            Status = Status,
            StartedAt = StartedAt,
            RequestCount = RequestCount,
            ErrorCount = ErrorCount,
            LastError = LastError,
            LastRequestAt = LastRequestAt
        };
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _app?.DisposeAsync().AsTask().Wait();
    }
}

