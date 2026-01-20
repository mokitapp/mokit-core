using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Mokit.Domain.Entities;
using Mokit.HostManager.Hosting;
using Mokit.Application.Interfaces;

namespace Mokit.HostManager.Management;

public class MockHostManager : IMockHostManager, IDisposable
{
    private readonly ConcurrentDictionary<Guid, MockHostInstance> _servers = new();
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<MockHostManager> _logger;
    private readonly IWebhookJobQueue _webhookQueue;

    public event EventHandler<MockServerEventArgs>? ServerStatusChanged;
    public event EventHandler<MockRequestEventArgs>? RequestReceived;

    public MockHostManager(ILoggerFactory loggerFactory, IWebhookJobQueue webhookQueue)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<MockHostManager>();
        _webhookQueue = webhookQueue;
    }

    public async Task<bool> StartServerAsync(MockProject project)
    {
        if (_servers.ContainsKey(project.Id))
        {
            _logger.LogWarning("Server for project {ProjectId} is already running", project.Id);
            return false;
        }

        // Check if port is already in use
        if (_servers.Values.Any(s => s.Port == project.Port))
        {
            _logger.LogWarning("Port {Port} is already in use by another mock server", project.Port);
            return false;
        }

        var instance = new MockHostInstance(
            project,
            _loggerFactory.CreateLogger<MockHostInstance>(),
            _webhookQueue,
            OnRequestLogged
        );

        if (!_servers.TryAdd(project.Id, instance))
        {
            return false;
        }

        var success = await instance.StartAsync();
        
        if (success)
        {
            OnServerStatusChanged(instance.GetInfo());
        }
        else
        {
            _servers.TryRemove(project.Id, out _);
            instance.Dispose();
        }

        return success;
    }

    public async Task<bool> StopServerAsync(Guid projectId)
    {
        if (!_servers.TryRemove(projectId, out var instance))
        {
            return false;
        }

        var success = await instance.StopAsync();
        OnServerStatusChanged(instance.GetInfo());
        instance.Dispose();

        return success;
    }

    public async Task<bool> RestartServerAsync(Guid projectId)
    {
        if (!_servers.TryGetValue(projectId, out var instance))
        {
            return false;
        }

        await instance.StopAsync();
        var success = await instance.StartAsync();
        OnServerStatusChanged(instance.GetInfo());

        return success;
    }

    public MockServerInfo? GetServerStatus(Guid projectId)
    {
        return _servers.TryGetValue(projectId, out var instance) 
            ? instance.GetInfo() 
            : null;
    }

    public IEnumerable<MockServerInfo> GetAllServers()
    {
        return _servers.Values.Select(s => s.GetInfo());
    }

    public bool IsServerRunning(Guid projectId)
    {
        return _servers.TryGetValue(projectId, out var instance) && 
               instance.Status == Domain.Enums.MockServerStatus.Running;
    }

    public Task UpdateEndpointsAsync(Guid projectId, IEnumerable<MockEndpoint> endpoints)
    {
        if (_servers.TryGetValue(projectId, out var instance))
        {
            instance.UpdateEndpoints(endpoints);
        }
        return Task.CompletedTask;
    }

    private void OnServerStatusChanged(MockServerInfo info)
    {
        ServerStatusChanged?.Invoke(this, new MockServerEventArgs
        {
            ProjectId = info.ProjectId,
            ServerInfo = info
        });
    }

    private void OnRequestLogged(MockRequestLog log)
    {
        RequestReceived?.Invoke(this, new MockRequestEventArgs
        {
            ProjectId = log.ProjectId,
            EndpointId = log.EndpointId,
            Method = log.Method,
            Path = log.Path,
            StatusCode = log.StatusCode,
            DurationMs = log.DurationMs,
            IsMatched = log.IsMatched,
            Timestamp = log.Timestamp
        });
    }

    public void Dispose()
    {
        foreach (var server in _servers.Values)
        {
            server.StopAsync().Wait();
            server.Dispose();
        }
        _servers.Clear();
    }
}


