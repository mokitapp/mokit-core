using System.Text.Json;
using Mokit.Domain.Entities;
using Mokit.Domain.Enums;
using Mokit.MockEngine.Routing;
using Mokit.MockEngine.Templates;

namespace Mokit.MockEngine.Processing;

public class RequestProcessor
{
    private readonly RouteMatcher _routeMatcher;
    private readonly TemplateEngine _templateEngine;

    public RequestProcessor()
    {
        _routeMatcher = new RouteMatcher();
        _templateEngine = new TemplateEngine();
    }

    public MockProcessingResult Process(MockRequest request, IEnumerable<MockEndpoint> endpoints)
    {
        // Find matching endpoint
        var matchResult = FindMatchingEndpoint(request, endpoints);
        
        if (!matchResult.IsMatch)
        {
            return MockProcessingResult.NotFound(request.Path);
        }

        var endpoint = matchResult.Endpoint!;
        var routeParams = matchResult.RouteParams;

        // Select response based on endpoint configuration
        var response = SelectResponse(endpoint, request);
        
        if (response == null)
        {
            return MockProcessingResult.NotFound($"No response configured for endpoint: {endpoint.Name}");
        }

        // Build request context for template rendering
        var context = BuildContext(request, routeParams);

        // Process response body with template engine
        var processedBody = _templateEngine.Render(response.Body ?? "{}", context);
        
        // Process headers
        var headers = ProcessHeaders(response.Headers, context);

        return new MockProcessingResult
        {
            IsSuccess = true,
            StatusCode = response.StatusCode,
            Body = processedBody,
            ContentType = response.ContentType,
            Headers = headers,
            MatchedEndpoint = endpoint,
            MatchedResponse = response,
            RouteParams = routeParams,
            Delay = CalculateDelay(endpoint, response)
        };
    }

    private EndpointMatchResult FindMatchingEndpoint(MockRequest request, IEnumerable<MockEndpoint> endpoints)
    {
        foreach (var endpoint in endpoints.Where(e => e.IsActive).OrderBy(e => e.Order))
        {
            // Check HTTP method
            if (!MethodMatches(endpoint.Method, request.Method))
            {
                continue;
            }

            // Check route
            var routeMatch = _routeMatcher.Match(
                endpoint.Route, 
                request.Path, 
                endpoint.IsWildcard, 
                endpoint.RegexPattern);

            if (routeMatch.IsMatch)
            {
                return new EndpointMatchResult
                {
                    IsMatch = true,
                    Endpoint = endpoint,
                    RouteParams = routeMatch.Parameters
                };
            }
        }

        return new EndpointMatchResult { IsMatch = false };
    }

    private static bool MethodMatches(HttpMethodType endpointMethod, string requestMethod)
    {
        return string.Equals(endpointMethod.ToString(), requestMethod, StringComparison.OrdinalIgnoreCase);
    }

    private MockResponse? SelectResponse(MockEndpoint endpoint, MockRequest request)
    {
        var activeResponses = endpoint.Responses.Where(r => r.IsActive).ToList();
        
        if (!activeResponses.Any())
        {
            return null;
        }

        // Check conditional responses first
        foreach (var response in activeResponses.Where(r => !string.IsNullOrEmpty(r.ConditionExpression)))
        {
            if (EvaluateCondition(response.ConditionExpression!, request))
            {
                return response;
            }
        }

        // Select based on response mode
        return endpoint.ResponseMode switch
        {
            ResponseSelectionMode.Sequential => GetSequentialResponse(endpoint, activeResponses),
            ResponseSelectionMode.Random => GetRandomResponse(activeResponses),
            _ => activeResponses.FirstOrDefault(r => r.IsDefault) ?? activeResponses.First()
        };
    }

    private MockResponse GetSequentialResponse(MockEndpoint endpoint, List<MockResponse> responses)
    {
        var orderedResponses = responses.OrderBy(r => r.Order).ToList();
        var index = endpoint.CurrentResponseIndex % orderedResponses.Count;
        endpoint.CurrentResponseIndex++;
        return orderedResponses[index];
    }

    private static MockResponse GetRandomResponse(List<MockResponse> responses)
    {
        return responses[Random.Shared.Next(responses.Count)];
    }

    private bool EvaluateCondition(string expression, MockRequest request)
    {
        // Simple condition evaluation
        // Format: "body.field == value" or "header.name == value" or "query.param == value"
        try
        {
            var parts = expression.Split(new[] { "==", "!=", ">", "<", ">=", "<=" }, StringSplitOptions.TrimEntries);
            if (parts.Length != 2) return false;

            var leftValue = GetConditionValue(parts[0], request);
            var rightValue = parts[1].Trim('"', '\'');

            if (expression.Contains("=="))
                return string.Equals(leftValue, rightValue, StringComparison.OrdinalIgnoreCase);
            if (expression.Contains("!="))
                return !string.Equals(leftValue, rightValue, StringComparison.OrdinalIgnoreCase);

            if (decimal.TryParse(leftValue, out var leftNum) && decimal.TryParse(rightValue, out var rightNum))
            {
                if (expression.Contains(">=")) return leftNum >= rightNum;
                if (expression.Contains("<=")) return leftNum <= rightNum;
                if (expression.Contains(">")) return leftNum > rightNum;
                if (expression.Contains("<")) return leftNum < rightNum;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private string? GetConditionValue(string path, MockRequest request)
    {
        var segments = path.Split('.');
        if (segments.Length < 2) return null;

        var source = segments[0].ToLower();
        var key = segments[1];

        return source switch
        {
            "body" => GetBodyValue(request.Body, string.Join(".", segments.Skip(1))),
            "header" or "headers" => request.Headers.TryGetValue(key, out var h) ? h : null,
            "query" => request.QueryParams.TryGetValue(key, out var q) ? q : null,
            _ => null
        };
    }

    private string? GetBodyValue(string? body, string path)
    {
        if (string.IsNullOrEmpty(body)) return null;

        try
        {
            var json = JsonDocument.Parse(body);
            var segments = path.Split('.');
            JsonElement current = json.RootElement;

            foreach (var segment in segments)
            {
                if (current.ValueKind == JsonValueKind.Object && current.TryGetProperty(segment, out var next))
                {
                    current = next;
                }
                else
                {
                    return null;
                }
            }

            return current.ValueKind switch
            {
                JsonValueKind.String => current.GetString(),
                JsonValueKind.Number => current.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => current.GetRawText()
            };
        }
        catch
        {
            return null;
        }
    }

    private MockRequestContext BuildContext(MockRequest request, Dictionary<string, string> routeParams)
    {
        object? bodyObject = null;
        if (!string.IsNullOrEmpty(request.Body))
        {
            try
            {
                bodyObject = JsonSerializer.Deserialize<Dictionary<string, object>>(request.Body);
            }
            catch
            {
                bodyObject = request.Body;
            }
        }

        return new MockRequestContext
        {
            Path = request.Path,
            Method = request.Method,
            QueryParams = request.QueryParams,
            Headers = request.Headers,
            RouteParams = routeParams,
            Body = bodyObject,
            RawBody = request.Body
        };
    }

    private Dictionary<string, string> ProcessHeaders(string? headersJson, MockRequestContext context)
    {
        if (string.IsNullOrEmpty(headersJson))
        {
            return new Dictionary<string, string>();
        }

        try
        {
            var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(headersJson) 
                ?? new Dictionary<string, string>();

            // Process template variables in header values
            foreach (var key in headers.Keys.ToList())
            {
                headers[key] = _templateEngine.Render(headers[key], context);
            }

            return headers;
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }

    private static int CalculateDelay(MockEndpoint endpoint, MockResponse response)
    {
        if (endpoint.DelayMin.HasValue && endpoint.DelayMax.HasValue)
        {
            return Random.Shared.Next(endpoint.DelayMin.Value, endpoint.DelayMax.Value + 1);
        }

        if (endpoint.DelayMin.HasValue)
        {
            return endpoint.DelayMin.Value;
        }

        return 0;
    }
}

public class MockRequest
{
    public string Path { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public Dictionary<string, string> QueryParams { get; set; } = new();
    public Dictionary<string, string> Headers { get; set; } = new();
    public string? Body { get; set; }
}

public class EndpointMatchResult
{
    public bool IsMatch { get; set; }
    public MockEndpoint? Endpoint { get; set; }
    public Dictionary<string, string> RouteParams { get; set; } = new();
}

public class MockProcessingResult
{
    public bool IsSuccess { get; set; }
    public int StatusCode { get; set; } = 200;
    public string Body { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/json";
    public Dictionary<string, string> Headers { get; set; } = new();
    public MockEndpoint? MatchedEndpoint { get; set; }
    public MockResponse? MatchedResponse { get; set; }
    public Dictionary<string, string> RouteParams { get; set; } = new();
    public int Delay { get; set; }
    public string? ErrorMessage { get; set; }

    public static MockProcessingResult NotFound(string message) => new()
    {
        IsSuccess = false,
        StatusCode = 404,
        Body = JsonSerializer.Serialize(new { error = "Not Found", message }),
        ErrorMessage = message
    };
}


