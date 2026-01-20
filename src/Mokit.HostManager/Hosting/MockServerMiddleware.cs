using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Mokit.Domain.Entities;
using Mokit.MockEngine.Processing;
using Mokit.Application.Interfaces;
using Mokit.Application.DTOs.Webhook;

namespace Mokit.HostManager.Hosting;

public class MockServerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RequestProcessor _requestProcessor;
    private readonly ValidationProcessor _validationProcessor;
    private readonly IWebhookJobQueue _webhookQueue;
    private List<MockEndpoint> _endpoints;
    private readonly Guid _projectId;
    private readonly Action<MockRequestLog>? _onRequestLogged;

    public MockServerMiddleware(
        RequestDelegate next,
        Guid projectId,
        List<MockEndpoint> endpoints,
        IWebhookJobQueue webhookQueue,
        Action<MockRequestLog>? onRequestLogged = null)
    {
        _next = next;
        _projectId = projectId;
        _endpoints = endpoints;
        _onRequestLogged = onRequestLogged;
        _webhookQueue = webhookQueue;
        _requestProcessor = new RequestProcessor();
        _validationProcessor = new ValidationProcessor();
    }

    public void UpdateEndpoints(List<MockEndpoint> endpoints)
    {
        _endpoints = endpoints;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var request = await BuildMockRequest(context);

        // Find matching endpoint
        var matchingEndpoint = FindMatchingEndpoint(request);
        
        if (matchingEndpoint != null)
        {
            // Validate request
            var validationResult = _validationProcessor.Validate(request, matchingEndpoint);
            
            if (!validationResult.IsValid)
            {
                await WriteValidationErrorResponse(context, validationResult);
                stopwatch.Stop();
                LogRequest(context, request, 400, stopwatch.ElapsedMilliseconds, matchingEndpoint.Id, true, "Validation failed");
                return;
            }
        }

        // Process the request
        var result = _requestProcessor.Process(request, _endpoints);

        // Dispatch Webhooks (Fire and Forget - handled by Queue)
        if (result.IsSuccess && result.MatchedEndpoint?.Webhooks?.Any(w => w.IsEnabled) == true)
        {
            var contextData = new WebhookExecutionContext
            {
                Path = request.Path,
                Method = request.Method,
                QueryParams = request.QueryParams,
                Headers = request.Headers,
                RouteParams = result.RouteParams,
                RawBody = request.Body,
                // Body object parsing could be done here if needed, but RawBody is most important for now
            };

            foreach (var hook in result.MatchedEndpoint.Webhooks.Where(w => w.IsEnabled))
            {
                // We don't await the enqueue to avoid blocking response, but since it is a Channel it is fast.
                // We use default token as we don't want to cancel webhook if request cancels? 
                // Actually RequestAborted might correspond to user closing browser. Webhook should probably still fire.
                // So we use CancellationToken.None or default.
                _ = _webhookQueue.EnqueueAsync(new WebhookJob
                {
                    Definition = hook,
                    Context = contextData,
                    OriginalRequestId = Guid.NewGuid() // or trace id
                }).AsTask();
            }
        }

        // Apply delay if configured
        if (result.Delay > 0)
        {
            await Task.Delay(result.Delay);
        }

        // Write response
        await WriteResponse(context, result);
        
        stopwatch.Stop();
        LogRequest(context, request, result.StatusCode, stopwatch.ElapsedMilliseconds, 
            result.MatchedEndpoint?.Id, result.IsSuccess, result.ErrorMessage);
    }

    private async Task<MockRequest> BuildMockRequest(HttpContext context)
    {
        var request = new MockRequest
        {
            Path = context.Request.Path.Value ?? "/",
            Method = context.Request.Method,
            QueryParams = context.Request.Query
                .ToDictionary(q => q.Key, q => q.Value.ToString()),
            Headers = context.Request.Headers
                .ToDictionary(h => h.Key, h => h.Value.ToString())
        };

        // Read body for non-GET requests
        if (context.Request.ContentLength > 0 && 
            context.Request.Method != "GET" && 
            context.Request.Method != "HEAD")
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            request.Body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
        }

        return request;
    }

    private MockEndpoint? FindMatchingEndpoint(MockRequest request)
    {
        var routeMatcher = new MockEngine.Routing.RouteMatcher();
        
        foreach (var endpoint in _endpoints.Where(e => e.IsActive).OrderBy(e => e.Order))
        {
            if (!string.Equals(endpoint.Method.ToString(), request.Method, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var match = routeMatcher.Match(endpoint.Route, request.Path, endpoint.IsWildcard, endpoint.RegexPattern);
            if (match.IsMatch)
            {
                return endpoint;
            }
        }

        return null;
    }

    private static async Task WriteValidationErrorResponse(HttpContext context, ValidationResult validationResult)
    {
        context.Response.StatusCode = 400;
        context.Response.ContentType = "application/json";
        
        var errorResponse = new
        {
            error = "Validation Error",
            message = "Request validation failed",
            errors = validationResult.Errors.Select(e => new
            {
                parameter = e.ParameterName,
                location = e.Location,
                code = e.ErrorCode,
                message = e.Message
            })
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
    }

    private static async Task WriteResponse(HttpContext context, MockProcessingResult result)
    {
        context.Response.StatusCode = result.StatusCode;
        context.Response.ContentType = result.ContentType;

        foreach (var header in result.Headers)
        {
            context.Response.Headers[header.Key] = header.Value;
        }

        if (!string.IsNullOrEmpty(result.Body))
        {
            await context.Response.WriteAsync(result.Body);
        }
    }

    private void LogRequest(HttpContext context, MockRequest request, int statusCode, 
        long durationMs, Guid? endpointId, bool isMatched, string? errorMessage)
    {
        _onRequestLogged?.Invoke(new MockRequestLog
        {
            ProjectId = _projectId,
            EndpointId = endpointId,
            Method = request.Method,
            Path = request.Path,
            QueryString = context.Request.QueryString.Value,
            StatusCode = statusCode,
            DurationMs = durationMs,
            IsMatched = isMatched,
            ErrorMessage = errorMessage,
            ClientIp = context.Connection.RemoteIpAddress?.ToString(),
            UserAgent = request.Headers.GetValueOrDefault("User-Agent"),
            Timestamp = DateTime.UtcNow
        });
    }
}

public class MockRequestLog
{
    public Guid ProjectId { get; set; }
    public Guid? EndpointId { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? QueryString { get; set; }
    public int StatusCode { get; set; }
    public long DurationMs { get; set; }
    public bool IsMatched { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ClientIp { get; set; }
    public string? UserAgent { get; set; }
    public DateTime Timestamp { get; set; }
}


