using Microsoft.EntityFrameworkCore;
using Mokit.Application.Common;
using Mokit.Application.Interfaces;
using Mokit.Application.Interfaces.Persistence;
using Mokit.Domain.Entities;
using Mokit.Domain.Enums;
using Mokit.Infrastructure.Data;
using System.Text.Json;
using System.Text.Json.Nodes;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Mokit.Infrastructure.Services;

public class ImportService : IImportService
{
    private readonly IUnitOfWork<MokitDbContext> _unitOfWork;

    public ImportService(IUnitOfWork<MokitDbContext> unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task<Result<ParsedCollectionData>> ParsePostmanCollectionAsync(string content)
    {
        try
        {
            var jsonDoc = JsonDocument.Parse(content);
            var root = jsonDoc.RootElement;

            // Validate Postman collection format
            if (!root.TryGetProperty("info", out var info) ||
                !info.TryGetProperty("name", out var name))
            {
                return Task.FromResult(Result<ParsedCollectionData>.Failure("Invalid Postman collection format"));
            }

            var collectionData = new ParsedCollectionData
            {
                CollectionName = name.GetString() ?? "Unnamed Collection",
                Variables = ParsePostmanVariables(root),
                Endpoints = new List<ParsedEndpoint>()
            };

            // Parse description if available
            if (info.TryGetProperty("description", out var description))
            {
                collectionData.Description = ParseDescription(description);
            }

            // Parse items (requests)
            if (root.TryGetProperty("item", out var items))
            {
                ParsePostmanItems(items, collectionData.Endpoints, "");
            }

            return Task.FromResult(Result<ParsedCollectionData>.Success(collectionData));
        }
        catch (JsonException ex)
        {
            return Task.FromResult(Result<ParsedCollectionData>.Failure($"Invalid JSON format: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<ParsedCollectionData>.Failure($"Failed to parse Postman collection: {ex.Message}"));
        }
    }

    public Task<Result<ParsedCollectionData>> ParseOpenApiSpecAsync(string content, bool isYaml)
    {
        try
        {
            JsonNode? root;

            if (isYaml)
            {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();
                var yamlObject = deserializer.Deserialize<object>(content);
                var jsonString = JsonSerializer.Serialize(yamlObject);
                root = JsonNode.Parse(jsonString);
            }
            else
            {
                root = JsonNode.Parse(content);
            }

            if (root == null)
            {
                return Task.FromResult(Result<ParsedCollectionData>.Failure("Invalid OpenAPI specification"));
            }

            var collectionData = new ParsedCollectionData
            {
                CollectionName = root["info"]?["title"]?.GetValue<string>() ?? "OpenAPI Spec",
                Description = root["info"]?["description"]?.GetValue<string>(),
                Endpoints = new List<ParsedEndpoint>(),
                Variables = new List<ParsedVariable>()
            };

            // Parse paths
            if (root["paths"] is JsonObject paths)
            {
                foreach (var pathItem in paths)
                {
                    var path = pathItem.Key;
                    if (pathItem.Value is JsonObject pathObject)
                    {
                        ParseOpenApiPath(path, pathObject, collectionData.Endpoints);
                    }
                }
            }

            return Task.FromResult(Result<ParsedCollectionData>.Success(collectionData));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<ParsedCollectionData>.Failure($"Failed to parse OpenAPI spec: {ex.Message}"));
        }
    }

    public async Task<Result<ImportResult>> ImportToProjectAsync(string userId, Guid projectId, ParsedCollectionData data, ImportOptions options)
    {
        var result = new ImportResult
        {
            EndpointsCreated = 0,
            EndpointsSkipped = 0,
            ResponsesCreated = 0,
            Warnings = new List<string>(),
            Errors = new List<string>()
        };

        try
        {
            var importResult = await _unitOfWork.ExecuteTransactionAsync(async scope =>
            {
                // Verify project exists and user has access
                var project = await scope.Context.MockProjects
                    .FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId);

                if (project == null)
                {
                    return (false, result, "Project not found or you don't have access to it.");
                }

                // Get existing endpoints for duplicate check
                var existingEndpoints = await scope.Context.MockEndpoints
                    .Where(e => e.ProjectId == projectId)
                    .Select(e => new { e.Route, e.Method })
                    .ToListAsync();

                // Get max order for new endpoints
                var maxOrder = await scope.Context.MockEndpoints
                    .Where(e => e.ProjectId == projectId)
                    .MaxAsync(e => (int?)e.Order) ?? -1;

                foreach (var parsedEndpoint in data.Endpoints)
                {
                    // Check for duplicates
                    if (options.SkipDuplicates)
                    {
                        var isDuplicate = existingEndpoints.Any(e =>
                            e.Route.Equals(parsedEndpoint.Route, StringComparison.OrdinalIgnoreCase) &&
                            e.Method == parsedEndpoint.Method);

                        if (isDuplicate)
                        {
                            result.EndpointsSkipped++;
                            result.Warnings.Add($"Skipped duplicate: {parsedEndpoint.Method} {parsedEndpoint.Route}");
                            continue;
                        }
                    }

                    // Create endpoint
                    var endpoint = new MockEndpoint
                    {
                        ProjectId = projectId,
                        Name = string.IsNullOrEmpty(parsedEndpoint.Name)
                            ? $"{parsedEndpoint.Method} {parsedEndpoint.Route}"
                            : parsedEndpoint.Name,
                        Description = parsedEndpoint.Description,
                        Route = NormalizeRoute(parsedEndpoint.Route),
                        Method = parsedEndpoint.Method,
                        IsActive = true,
                        Order = ++maxOrder,
                        ResponseMode = ResponseSelectionMode.Sequential
                    };

                    scope.Context.MockEndpoints.Add(endpoint);

                    // Create responses
                    if (options.CreateExamples && parsedEndpoint.Responses.Any())
                    {
                        var responseOrder = 0;
                        var firstResponse = true;

                        foreach (var parsedResponse in parsedEndpoint.Responses)
                        {
                            var response = new MockResponse
                            {
                                EndpointId = endpoint.Id,
                                Name = string.IsNullOrEmpty(parsedResponse.Name)
                                    ? $"Response {parsedResponse.StatusCode}"
                                    : parsedResponse.Name,
                                Description = parsedResponse.StatusText,
                                StatusCode = parsedResponse.StatusCode,
                                Body = parsedResponse.Body,
                                ContentType = DetermineContentType(parsedResponse),
                                IsDefault = firstResponse,
                                Order = responseOrder++,
                                IsActive = true
                            };

                            // Add headers if option is enabled
                            if (options.ImportHeaders && parsedResponse.Headers.Any())
                            {
                                var headersDict = parsedResponse.Headers.ToDictionary(h => h.Key, h => h.Value);
                                response.Headers = JsonSerializer.Serialize(headersDict);
                            }

                            scope.Context.MockResponses.Add(response);
                            result.ResponsesCreated++;
                            firstResponse = false;
                        }
                    }
                    else
                    {
                        // Create a default response if no responses were provided or option is disabled
                        var defaultResponse = new MockResponse
                        {
                            EndpointId = endpoint.Id,
                            Name = "Default Response",
                            StatusCode = 200,
                            Body = "{}",
                            ContentType = "application/json",
                            IsDefault = true,
                            Order = 0,
                            IsActive = true
                        };

                        scope.Context.MockResponses.Add(defaultResponse);
                        result.ResponsesCreated++;
                    }

                    result.EndpointsCreated++;
                }

                return (true, result, (string?)null);
            });

            if (!importResult.Item1)
            {
                return Result<ImportResult>.Failure(importResult.Item3 ?? "Import failed");
            }

            return Result<ImportResult>.Success(importResult.Item2);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Import failed: {ex.Message}");
            return Result<ImportResult>.Failure($"Import failed: {ex.Message}");
        }
    }

    private string NormalizeRoute(string route)
    {
        // Ensure route starts with /
        if (!route.StartsWith("/"))
        {
            route = "/" + route;
        }

        // Replace Postman-style path parameters (:id) with curly brace style ({id})
        var parts = route.Split('/');
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].StartsWith(":"))
            {
                parts[i] = "{" + parts[i].Substring(1) + "}";
            }
        }

        return string.Join("/", parts);
    }

    private string DetermineContentType(ParsedResponse response)
    {
        // Check if there's a Content-Type header
        var contentTypeHeader = response.Headers.FirstOrDefault(h =>
            h.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase));

        if (contentTypeHeader != null && !string.IsNullOrEmpty(contentTypeHeader.Value))
        {
            return contentTypeHeader.Value;
        }

        // Try to detect from body content
        if (!string.IsNullOrEmpty(response.Body))
        {
            var trimmed = response.Body.TrimStart();
            if (trimmed.StartsWith("{") || trimmed.StartsWith("["))
            {
                return "application/json";
            }
            if (trimmed.StartsWith("<"))
            {
                return "application/xml";
            }
        }

        return "application/json";
    }

    private List<ParsedVariable> ParsePostmanVariables(JsonElement root)
    {
        var variables = new List<ParsedVariable>();
        if (root.TryGetProperty("variable", out var vars) && vars.ValueKind == JsonValueKind.Array)
        {
            foreach (var variable in vars.EnumerateArray())
            {
                if (variable.TryGetProperty("key", out var key))
                {
                    var parsedVar = new ParsedVariable
                    {
                        Key = key.GetString() ?? "",
                        Value = variable.TryGetProperty("value", out var val) ? val.GetString() ?? "" : "",
                        Type = variable.TryGetProperty("type", out var type) ? type.GetString() : null
                    };
                    variables.Add(parsedVar);
                }
            }
        }
        return variables;
    }

    private string? ParseDescription(JsonElement description)
    {
        if (description.ValueKind == JsonValueKind.String)
        {
            return description.GetString();
        }
        else if (description.ValueKind == JsonValueKind.Object &&
                 description.TryGetProperty("content", out var content))
        {
            return content.GetString();
        }
        return null;
    }

    private void ParsePostmanItems(JsonElement items, List<ParsedEndpoint> endpoints, string parentPath)
    {
        if (items.ValueKind != JsonValueKind.Array) return;

        foreach (var item in items.EnumerateArray())
        {
            var itemName = item.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "";

            if (item.TryGetProperty("request", out var request))
            {
                // Parse individual request
                var endpoint = ParsePostmanRequest(item, request);
                if (endpoint != null)
                {
                    endpoint.Name = itemName;
                    if (!string.IsNullOrEmpty(parentPath))
                    {
                        endpoint.Route = $"{parentPath}/{endpoint.Route}".Replace("//", "/");
                    }

                    // Parse saved responses
                    if (item.TryGetProperty("response", out var responses) && responses.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var response in responses.EnumerateArray())
                        {
                            var parsedResponse = ParsePostmanResponse(response);
                            if (parsedResponse != null)
                            {
                                endpoint.Responses.Add(parsedResponse);
                            }
                        }
                    }

                    endpoints.Add(endpoint);
                }
            }
            else if (item.TryGetProperty("item", out var subItems))
            {
                // This is a folder, parse recursively
                var newPath = string.IsNullOrEmpty(parentPath) ? itemName : $"{parentPath}/{itemName}";
                ParsePostmanItems(subItems, endpoints, newPath);
            }
        }
    }

    private ParsedEndpoint? ParsePostmanRequest(JsonElement item, JsonElement request)
    {
        string methodString = "GET";
        if (request.TryGetProperty("method", out var methodElement))
        {
            methodString = methodElement.GetString() ?? "GET";
        }

        if (!Enum.TryParse<HttpMethodType>(methodString, true, out var method))
        {
            method = HttpMethodType.GET;
        }

        if (!request.TryGetProperty("url", out var urlElement)) return null;

        var route = ParsePostmanUrl(urlElement);
        if (string.IsNullOrEmpty(route)) return null;

        var endpoint = new ParsedEndpoint
        {
            Method = method,
            Route = route,
            Headers = new List<ParsedHeader>(),
            Responses = new List<ParsedResponse>()
        };

        // Parse description
        if (request.TryGetProperty("description", out var desc))
        {
            endpoint.Description = ParseDescription(desc);
        }

        // Parse headers
        if (request.TryGetProperty("header", out var headers))
        {
            ParsePostmanHeaders(headers, endpoint.Headers);
        }

        // Parse body
        if (request.TryGetProperty("body", out var body))
        {
            endpoint.Body = ParsePostmanBody(body);
        }

        return endpoint;
    }

    private ParsedResponse? ParsePostmanResponse(JsonElement response)
    {
        var parsedResponse = new ParsedResponse
        {
            Name = response.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "",
            StatusCode = response.TryGetProperty("code", out var code) ? code.GetInt32() : 200,
            StatusText = response.TryGetProperty("status", out var status) ? status.GetString() ?? "" : "",
            Headers = new List<ParsedHeader>()
        };

        // Parse body
        if (response.TryGetProperty("body", out var body))
        {
            parsedResponse.Body = body.GetString();
        }

        // Parse headers
        if (response.TryGetProperty("header", out var headers) && headers.ValueKind == JsonValueKind.Array)
        {
            foreach (var header in headers.EnumerateArray())
            {
                if (header.TryGetProperty("key", out var key) &&
                    header.TryGetProperty("value", out var value))
                {
                    parsedResponse.Headers.Add(new ParsedHeader
                    {
                        Key = key.GetString() ?? "",
                        Value = value.GetString() ?? ""
                    });
                }
            }
        }

        return parsedResponse;
    }

    private string ParsePostmanUrl(JsonElement urlElement)
    {
        if (urlElement.ValueKind == JsonValueKind.String)
        {
            var url = urlElement.GetString() ?? "";
            return ExtractPathFromUrl(url);
        }
        else if (urlElement.ValueKind == JsonValueKind.Object)
        {
            if (urlElement.TryGetProperty("path", out var pathArray) && pathArray.ValueKind == JsonValueKind.Array)
            {
                var pathParts = new List<string>();
                foreach (var part in pathArray.EnumerateArray())
                {
                    var partStr = part.GetString() ?? "";
                    // Convert Postman variables to route parameters
                    if (partStr.StartsWith(":"))
                    {
                        partStr = "{" + partStr.Substring(1) + "}";
                    }
                    pathParts.Add(partStr);
                }
                return "/" + string.Join("/", pathParts);
            }
            else if (urlElement.TryGetProperty("raw", out var raw))
            {
                return ExtractPathFromUrl(raw.GetString() ?? "");
            }
        }
        return "";
    }

    private string ExtractPathFromUrl(string url)
    {
        // Remove protocol and host, keep path
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return uri.AbsolutePath;
        }

        // Try to handle {{base_url}} style variables
        if (url.Contains("}}"))
        {
            var afterVariable = url.LastIndexOf("}}");
            if (afterVariable >= 0 && afterVariable + 2 < url.Length)
            {
                url = url.Substring(afterVariable + 2);
            }
        }

        // If not a full URL, assume it's already a path
        if (!url.StartsWith("/"))
        {
            url = "/" + url;
        }
        return url;
    }

    private void ParsePostmanHeaders(JsonElement headers, List<ParsedHeader> targetHeaders)
    {
        if (headers.ValueKind != JsonValueKind.Array) return;

        foreach (var header in headers.EnumerateArray())
        {
            // Skip disabled headers
            if (header.TryGetProperty("disabled", out var disabled) && disabled.GetBoolean())
            {
                continue;
            }

            if (header.TryGetProperty("key", out var key) &&
                header.TryGetProperty("value", out var value))
            {
                targetHeaders.Add(new ParsedHeader
                {
                    Key = key.GetString() ?? "",
                    Value = value.GetString() ?? ""
                });
            }
        }
    }

    private ParsedBody? ParsePostmanBody(JsonElement body)
    {
        if (!body.TryGetProperty("mode", out var mode)) return null;

        var parsedBody = new ParsedBody
        {
            Mode = mode.GetString() ?? ""
        };

        switch (parsedBody.Mode)
        {
            case "raw":
                if (body.TryGetProperty("raw", out var raw))
                {
                    parsedBody.Raw = raw.GetString();
                }
                break;
            case "formdata":
                if (body.TryGetProperty("formdata", out var formdata) && formdata.ValueKind == JsonValueKind.Array)
                {
                    parsedBody.FormData = new List<ParsedFormData>();
                    foreach (var item in formdata.EnumerateArray())
                    {
                        if (item.TryGetProperty("key", out var key))
                        {
                            parsedBody.FormData.Add(new ParsedFormData
                            {
                                Key = key.GetString() ?? "",
                                Value = item.TryGetProperty("value", out var val) ? val.GetString() : null,
                                Src = item.TryGetProperty("src", out var src) ? src.GetString() : null,
                                Type = item.TryGetProperty("type", out var type) ? type.GetString() ?? "text" : "text"
                            });
                        }
                    }
                }
                break;
            case "urlencoded":
                if (body.TryGetProperty("urlencoded", out var urlencoded) && urlencoded.ValueKind == JsonValueKind.Array)
                {
                    parsedBody.UrlEncoded = new List<ParsedUrlEncoded>();
                    foreach (var item in urlencoded.EnumerateArray())
                    {
                        if (item.TryGetProperty("key", out var key) &&
                            item.TryGetProperty("value", out var value))
                        {
                            parsedBody.UrlEncoded.Add(new ParsedUrlEncoded
                            {
                                Key = key.GetString() ?? "",
                                Value = value.GetString() ?? ""
                            });
                        }
                    }
                }
                break;
        }

        return parsedBody;
    }

    private void ParseOpenApiPath(string path, JsonObject pathObject, List<ParsedEndpoint> endpoints)
    {
        var httpMethods = new[] { "get", "post", "put", "delete", "patch", "head", "options" };

        foreach (var operation in pathObject)
        {
            if (!httpMethods.Contains(operation.Key.ToLower())) continue;

            if (Enum.TryParse<HttpMethodType>(operation.Key, true, out var method))
            {
                var endpoint = new ParsedEndpoint
                {
                    Method = method,
                    Route = path,
                    Headers = new List<ParsedHeader>(),
                    Responses = new List<ParsedResponse>()
                };

                if (operation.Value is JsonObject operationObject)
                {
                    // Parse operation details
                    endpoint.Name = operationObject["summary"]?.GetValue<string>() ??
                                    operationObject["operationId"]?.GetValue<string>() ??
                                    $"{method} {path}";
                    endpoint.Description = operationObject["description"]?.GetValue<string>();

                    // Parse responses
                    if (operationObject["responses"] is JsonObject responses)
                    {
                        foreach (var response in responses)
                        {
                            var statusCode = int.TryParse(response.Key, out var code) ? code : 200;
                            var parsedResponse = new ParsedResponse
                            {
                                Name = $"Response {response.Key}",
                                StatusCode = statusCode,
                                Headers = new List<ParsedHeader>()
                            };

                            if (response.Value is JsonObject responseObject)
                            {
                                parsedResponse.StatusText = responseObject["description"]?.GetValue<string>() ?? "";

                                // Try to extract example body
                                if (responseObject["content"] is JsonObject content)
                                {
                                    foreach (var contentType in content)
                                    {
                                        if (contentType.Value is JsonObject contentValue)
                                        {
                                            if (contentValue["example"] != null)
                                            {
                                                parsedResponse.Body = contentValue["example"]?.ToJsonString();
                                            }
                                            else if (contentValue["examples"] is JsonObject examples)
                                            {
                                                var firstExample = examples.FirstOrDefault();
                                                if (firstExample.Value is JsonObject exampleObj &&
                                                    exampleObj["value"] != null)
                                                {
                                                    parsedResponse.Body = exampleObj["value"]?.ToJsonString();
                                                }
                                            }
                                        }
                                        break; // Just use the first content type
                                    }
                                }
                            }

                            endpoint.Responses.Add(parsedResponse);
                        }
                    }
                }

                endpoints.Add(endpoint);
            }
        }
    }
}
