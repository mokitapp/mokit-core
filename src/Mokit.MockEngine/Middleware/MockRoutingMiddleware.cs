using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mokit.Application.Interfaces;
using Mokit.Application.DTOs.Webhook;
using Mokit.Domain.Entities;
using Mokit.MockEngine.Processing;
using Mokit.MockEngine.Routing;
using Mokit.MockEngine.Templates;
using Mokit.Domain.Enums;

namespace Mokit.MockEngine.Middleware;

public class MockRoutingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<MockRoutingMiddleware> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TemplateEngine _templateEngine;
    private readonly RouteMatcher _routeMatcher;
    private readonly IWebhookJobQueue _webhookQueue;

    // Reserved paths that should not be handled by mock routing
    private static readonly string[] ReservedPaths = new[]
    {
        "/_blazor",
        "/_framework",
        "/api",
        "/account",
        "/projects",
        "/teams",
        "/servers",
        "/logs",
        "/variables",
        "/settings",
        "/import-export",
        "/admin",
        "/setup",
        "/favicon",
        "/css",
        "/js",
        "/lib",
        "/Mokit"
    };

    public MockRoutingMiddleware(
        RequestDelegate next,
        ILogger<MockRoutingMiddleware> logger,
        IServiceScopeFactory scopeFactory,
        IWebhookJobQueue webhookQueue)
    {
        _next = next;
        _logger = logger;
        _scopeFactory = scopeFactory;
        _webhookQueue = webhookQueue;
        _templateEngine = new TemplateEngine();
        _routeMatcher = new RouteMatcher();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";

        // Skip root path
        if (path == "/")
        {
            await _next(context);
            return;
        }

        // Skip reserved paths
        if (IsReservedPath(path))
        {
            await _next(context);
            return;
        }

        // Skip static files
        if (path.Contains('.') && !path.EndsWith(".json") && !path.EndsWith(".xml"))
        {
            await _next(context);
            return;
        }

        // Try to match mock endpoint
        var mockResult = await TryHandleMockRequest(context, path);
        
        if (!mockResult)
        {
            // No mock found, continue to next middleware
            await _next(context);
        }
    }

    private bool IsReservedPath(string path)
    {
        return ReservedPaths.Any(reserved => 
            path.Equals(reserved, StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith(reserved + "/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith(reserved + "?", StringComparison.OrdinalIgnoreCase));
    }

    private async Task<bool> TryHandleMockRequest(HttpContext context, string path)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Parse path to get project/team slugs
        var pathSegments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (pathSegments.Length < 1)
        {
            return false;
        }

        using var scope = _scopeFactory.CreateScope();
        var dataProvider = scope.ServiceProvider.GetRequiredService<IMockDataProvider>();
        var notifier = scope.ServiceProvider.GetService<IRequestLogNotifier>();

        // Try to find project
        MockProject? project = null;
        string endpointPath;

        // Check if first segment is a team slug
        var firstSegment = pathSegments[0];
        // We need a way to check if it's a team project. 
        // Previously we checked if a team existed with that slug.
        // Now we will try both possibilities or rely on the provider.
        
        // Strategy: First try as team/project
        if (pathSegments.Length >= 2)
        {
            var projectSlug = pathSegments[1];
            project = await dataProvider.GetProjectByTeamSlashProjectSlugAsync(firstSegment, projectSlug);
            
            if (project != null)
            {
                 endpointPath = pathSegments.Length > 2 
                    ? "/" + string.Join("/", pathSegments.Skip(2)) 
                    : "/";
            }
            else
            {
                // Try as personal project
                project = await dataProvider.GetProjectBySlugAsync(firstSegment);
                endpointPath = pathSegments.Length > 1 
                    ? "/" + string.Join("/", pathSegments.Skip(1)) 
                    : "/";
            }
        }
        else
        {
            // Only one segment, must be personal project root or simple path
             project = await dataProvider.GetProjectBySlugAsync(firstSegment);
             endpointPath = pathSegments.Length > 1 
                ? "/" + string.Join("/", pathSegments.Skip(1)) 
                : "/";
        }

        if (project == null)
        {
            return false;
        }

        // Handle OPTIONS request for CORS
        if (context.Request.Method == "OPTIONS" && project.EnableCors)
        {
            context.Response.StatusCode = 204;
            context.Response.Headers["Access-Control-Allow-Origin"] = "*";
            context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, PATCH, DELETE, OPTIONS, HEAD";
            context.Response.Headers["Access-Control-Allow-Headers"] = "*";
            context.Response.Headers["Access-Control-Max-Age"] = "86400";
            return true;
        }

        // Find matching endpoint
        var httpMethod = context.Request.Method;
        var matchingEndpoint = FindMatchingEndpoint(project.Endpoints, httpMethod, endpointPath, out var routeParams);

        if (matchingEndpoint == null)
        {
            _logger.LogDebug("No endpoint found for {Method} {Path} in project {ProjectSlug}", 
                httpMethod, endpointPath, project.Slug);
            
            // Return 404 for mock path that doesn't match any endpoint
            await WriteNotFoundResponse(context, project, endpointPath);
            stopwatch.Stop();
            
            // Log the request
            if (project.EnableLogging)
            {
                await LogRequestAsync(dataProvider, notifier, project, null, context, endpointPath, 404, stopwatch.ElapsedMilliseconds, null, null);
            }
            
            return true;
        }

        _logger.LogInformation("Mock request matched: {Method} {Path} -> {EndpointName}", 
            httpMethod, endpointPath, matchingEndpoint.Name);

        // Apply delay if configured
        await ApplyDelayAsync(matchingEndpoint);

        // Read body for validation (and later use)
        context.Request.EnableBuffering();
        string requestBody = "";
        if (context.Request.ContentLength > 0)
        {
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
        }

        // Validate request using professional validator
        var validator = new Validation.RequestValidator();
        var validationResult = validator.Validate(context, matchingEndpoint, routeParams, requestBody);
        
        if (!validationResult.IsValid)
        {
            // Use the status code from the first failed rule
            var statusCode = validationResult.FirstStatusCode;
            
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            
            string errorJson;
            
            // Use custom template if configured, otherwise use default response
            if (!string.IsNullOrEmpty(matchingEndpoint.ValidationErrorResponseTemplate))
            {
                errorJson = validationResult.RenderTemplate(matchingEndpoint.ValidationErrorResponseTemplate);
            }
            else
            {
                errorJson = JsonSerializer.Serialize(validationResult.ToResponse(), new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            
            await context.Response.WriteAsync(errorJson);
            
            stopwatch.Stop();
            
            // Log failed request
            if (project.EnableLogging)
            {
                await LogRequestAsync(dataProvider, notifier, project, matchingEndpoint, context, endpointPath, 
                    statusCode, stopwatch.ElapsedMilliseconds, errorJson, null);
            }
            
            return true;
        }

        // Get the response
        var response = GetResponse(matchingEndpoint);
        
        // Process the response body with template engine
        var requestContext = await BuildRequestContext(context, endpointPath, routeParams);
        var processedBody = _templateEngine.Render(response.Body ?? "{}", requestContext);

        // Write response
        context.Response.StatusCode = response.StatusCode;
        context.Response.ContentType = response.ContentType ?? "application/json";

        // Add custom headers
        if (!string.IsNullOrEmpty(response.Headers))
        {
            try
            {
                var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(response.Headers);
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        context.Response.Headers[header.Key] = header.Value;
                    }
                }
            }
            catch (JsonException)
            {
                // Invalid headers JSON, ignore
            }
        }

        // Add CORS headers if enabled
        if (project.EnableCors)
        {
            context.Response.Headers["Access-Control-Allow-Origin"] = "*";
            context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, PATCH, DELETE, OPTIONS, HEAD";
            context.Response.Headers["Access-Control-Allow-Headers"] = "*";
        }

        await context.Response.WriteAsync(processedBody);
        
        stopwatch.Stop();

        // Log the request if enabled
        if (project.EnableLogging)
            await LogRequestAsync(dataProvider, notifier, project, matchingEndpoint, context, endpointPath, 
                response.StatusCode, stopwatch.ElapsedMilliseconds, processedBody, response.Headers);


        // Dispatch Webhooks
        var webhookContext = new WebhookExecutionContext
        {
            Path = context.Request.Path,
            Method = context.Request.Method,
            QueryParams = context.Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString()),
            Headers = context.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
            RouteParams = routeParams,
            RawBody = requestBody,
            Body = string.IsNullOrEmpty(requestBody) ? null : JsonSerializer.Deserialize<object>(requestBody) // Optional?
        };
        DispatchWebhooks(matchingEndpoint, webhookContext);

        return true;
    }

    private void DispatchWebhooks(MockEndpoint endpoint, WebhookExecutionContext contextData)
    {
        if (endpoint.Webhooks?.Any(w => w.IsEnabled) == true)
        {
             foreach (var hook in endpoint.Webhooks.Where(w => w.IsEnabled))
            {
               _ = _webhookQueue.EnqueueAsync(new WebhookJob
                {
                    Definition = hook,
                    Context = contextData,
                    OriginalRequestId = Guid.NewGuid()
                }).AsTask();
            }
        }
    }

    private MockEndpoint? FindMatchingEndpoint(
        ICollection<MockEndpoint> endpoints, 
        string method, 
        string path,
        out Dictionary<string, string> routeParams)
    {
        routeParams = new Dictionary<string, string>();

        // Sort endpoints by specificity (non-wildcard first, then by route length)
        var sortedEndpoints = endpoints
            .Where(e => e.IsActive)
            .OrderByDescending(e => !e.IsWildcard)
            .ThenByDescending(e => e.Route.Count(c => c == '/'))
            .ToList();

        foreach (var endpoint in sortedEndpoints)
        {
            // Check HTTP method
            if (!endpoint.Method.ToString().Equals(method, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Try to match route
            var matchResult = _routeMatcher.Match(endpoint.Route, path, endpoint.IsWildcard, endpoint.RegexPattern);
            if (matchResult.IsMatch)
            {
                routeParams = matchResult.Parameters;
                return endpoint;
            }
        }

        return null;
    }

    private MockResponse GetResponse(MockEndpoint endpoint)
    {
        // Get default response or first available
        var response = endpoint.Responses.FirstOrDefault(r => r.IsDefault) 
                      ?? endpoint.Responses.FirstOrDefault();

        if (response == null)
        {
            // Create default response if none exists
            return new MockResponse
            {
                StatusCode = 200,
                ContentType = "application/json",
                Body = "{\"message\": \"No response configured\"}"
            };
        }

        return response;
    }

    private async Task ApplyDelayAsync(MockEndpoint endpoint)
    {
        if (endpoint.DelayMin > 0 || endpoint.DelayMax > 0)
        {
            var min = endpoint.DelayMin ?? 0;
            var max = endpoint.DelayMax ?? min;
            var delay = min == max ? min : Random.Shared.Next(min, max + 1);
            
            if (delay > 0)
            {
                await Task.Delay(delay);
            }
        }
    }

    private async Task<MockRequestContext> BuildRequestContext(
        HttpContext context, 
        string path,
        Dictionary<string, string> routeParams)
    {
        var queryParams = context.Request.Query
            .ToDictionary(q => q.Key, q => q.Value.ToString());

        var headers = context.Request.Headers
            .ToDictionary(h => h.Key, h => h.Value.ToString());

        string? body = null;
        if (context.Request.ContentLength > 0 && context.Request.Body.CanRead)
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
        }

        return new MockRequestContext
        {
            Path = path,
            Method = context.Request.Method,
            QueryParams = queryParams,
            Headers = headers,
            RouteParams = routeParams,
            Body = body
        };
    }

    private async Task WriteNotFoundResponse(HttpContext context, MockProject project, string path)
    {
        context.Response.StatusCode = 404;
        context.Response.ContentType = "application/json";

        if (project.EnableCors)
        {
            context.Response.Headers["Access-Control-Allow-Origin"] = "*";
        }
        
        var response = new
        {
            error = "Endpoint not found",
            project = project.Slug,
            path = path,
            availableEndpoints = project.Endpoints
                .Where(e => e.IsActive)
                .Select(e => new { method = e.Method.ToString(), route = e.Route })
                .ToList(),
            timestamp = DateTime.UtcNow
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }

    private async Task LogRequestAsync(
        IMockDataProvider dataProvider,
        IRequestLogNotifier? notifier,
        MockProject project,
        MockEndpoint? endpoint,
        HttpContext context,
        string path,
        int statusCode,
        long durationMs,
        string? responseBody,
        string? responseHeaders)
    {
        try
        {
            string? requestBody = null;
            if (context.Request.ContentLength > 0 && context.Request.Body.CanSeek)
            {
                context.Request.Body.Position = 0;
                using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
                requestBody = await reader.ReadToEndAsync();
            }

            var clientIp = context.Connection.RemoteIpAddress?.ToString();
            var userAgent = context.Request.Headers.UserAgent.ToString();
            var timestamp = DateTime.UtcNow;

            var log = new RequestLog
            {
                ProjectId = project.Id,
                EndpointId = endpoint?.Id,
                Method = context.Request.Method,
                Path = path,
                QueryString = context.Request.QueryString.Value,
                RequestHeaders = JsonSerializer.Serialize(
                    context.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString())),
                RequestBody = requestBody,
                ResponseStatusCode = statusCode,
                ResponseHeaders = responseHeaders ?? JsonSerializer.Serialize(
                    context.Response.Headers.ToDictionary(h => h.Key, h => h.Value.ToString())),
                ResponseBody = responseBody,
                DurationMs = durationMs,
                ClientIp = clientIp,
                UserAgent = userAgent,
                IsMatched = endpoint != null,
                MatchedRoute = endpoint?.Route,
                CreatedAt = timestamp
            };

            await dataProvider.LogRequestAsync(log);

            // Send real-time notification via SignalR
            if (notifier != null)
            {
                await notifier.NotifyRequestReceivedAsync(new RequestLogNotification
                {
                    ProjectId = project.Id,
                    EndpointId = endpoint?.Id,
                    Method = context.Request.Method,
                    Path = path,
                    QueryString = context.Request.QueryString.Value,
                    StatusCode = statusCode,
                    DurationMs = durationMs,
                    IsMatched = endpoint != null,
                    ClientIp = clientIp,
                    Timestamp = timestamp
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log request");
        }
    }
}

// Extension method to add middleware
public static class MockRoutingMiddlewareExtensions
{
    public static IApplicationBuilder UseMockRouting(this IApplicationBuilder app) 
    {
        return app.UseMiddleware<MockRoutingMiddleware>();
    }
}

