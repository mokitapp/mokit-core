using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Mokit.Domain.Entities;
using Mokit.Domain.Enums;

namespace Mokit.MockEngine.Validation;

/// <summary>
/// Enterprise-grade request validation engine.
/// Supports validation for query params, headers, path params, and JSON body properties.
/// </summary>
public class RequestValidator
{
    /// <summary>
    /// Validates an incoming request against the endpoint's validation rules.
    /// </summary>
    public ValidationResult Validate(
        HttpContext context,
        MockEndpoint endpoint,
        Dictionary<string, string> routeParams,
        string requestBody)
    {
        var result = new ValidationResult();

        if (endpoint.ValidationRules == null || endpoint.ValidationRules.Count == 0)
        {
            return result;
        }

        // Parse JSON body once if needed
        JsonDocument? jsonDoc = null;
        if (!string.IsNullOrEmpty(requestBody) && 
            context.Request.ContentType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true)
        {
            try
            {
                jsonDoc = JsonDocument.Parse(requestBody);
            }
            catch (JsonException ex)
            {
                result.AddError("body", "Invalid JSON format", ex.Message);
                return result; // Can't validate body rules if JSON is malformed
            }
        }

        foreach (var rule in endpoint.ValidationRules)
        {
            if (!rule.IsActive) continue;

            ValidateRule(context, rule, routeParams, jsonDoc, result);
        }

        jsonDoc?.Dispose();
        return result;
    }

    private void ValidateRule(
        HttpContext context,
        ValidationRule rule,
        Dictionary<string, string> routeParams,
        JsonDocument? jsonDoc,
        ValidationResult result)
    {
        // Get the value based on location
        var (value, found) = GetParameterValue(context, rule, routeParams, jsonDoc);

        // Check required
        if (rule.IsRequired && !found)
        {
            result.AddError(
                rule.ParameterName,
                rule.ErrorMessage ?? $"Required {rule.Location.ToString().ToLower()} parameter '{rule.ParameterName}' is missing",
                $"Location: {rule.Location}",
                rule.StatusCode);
            return;
        }

        // If not required and not found, skip further validation
        if (!found || string.IsNullOrEmpty(value))
        {
            return;
        }

        // Validate data type
        if (!ValidateDataType(value, rule, result))
        {
            return;
        }

        // Validate min/max (for numeric types)
        ValidateMinMax(value, rule, result);

        // Validate allowed values
        ValidateAllowedValues(value, rule, result);

        // Validate regex pattern
        ValidateRegex(value, rule, result);
    }

    private (string? value, bool found) GetParameterValue(
        HttpContext context,
        ValidationRule rule,
        Dictionary<string, string> routeParams,
        JsonDocument? jsonDoc)
    {
        switch (rule.Location)
        {
            case ParameterLocation.Query:
                if (context.Request.Query.TryGetValue(rule.ParameterName, out var queryValue))
                {
                    return (queryValue.ToString(), true);
                }
                return (null, false);

            case ParameterLocation.Header:
                if (context.Request.Headers.TryGetValue(rule.ParameterName, out var headerValue))
                {
                    return (headerValue.ToString(), true);
                }
                return (null, false);

            case ParameterLocation.Path:
                if (routeParams.TryGetValue(rule.ParameterName, out var pathValue))
                {
                    return (pathValue, true);
                }
                return (null, false);

            case ParameterLocation.Body:
                if (jsonDoc == null)
                {
                    return (null, false);
                }
                return GetJsonValue(jsonDoc.RootElement, rule.ParameterName);

            default:
                return (null, false);
        }
    }

    /// <summary>
    /// Gets a value from JSON using dot notation (e.g., "user.address.city")
    /// Also supports array indexing (e.g., "users[0].name")
    /// </summary>
    private (string? value, bool found) GetJsonValue(JsonElement root, string path)
    {
        var segments = ParseJsonPath(path);
        var current = root;

        foreach (var segment in segments)
        {
            if (segment.IsArrayIndex)
            {
                // Array index access
                if (current.ValueKind != JsonValueKind.Array)
                {
                    return (null, false);
                }

                var arrayLength = current.GetArrayLength();
                if (segment.ArrayIndex < 0 || segment.ArrayIndex >= arrayLength)
                {
                    return (null, false);
                }

                current = current[segment.ArrayIndex];
            }
            else
            {
                // Property access
                if (current.ValueKind != JsonValueKind.Object)
                {
                    return (null, false);
                }

                if (!current.TryGetProperty(segment.PropertyName, out var nextElement))
                {
                    return (null, false);
                }

                current = nextElement;
            }
        }

        // Convert final value to string
        return current.ValueKind switch
        {
            JsonValueKind.String => (current.GetString(), true),
            JsonValueKind.Number => (current.GetRawText(), true),
            JsonValueKind.True => ("true", true),
            JsonValueKind.False => ("false", true),
            JsonValueKind.Null => (null, true),
            JsonValueKind.Array => (current.GetRawText(), true),
            JsonValueKind.Object => (current.GetRawText(), true),
            _ => (null, false)
        };
    }

    private List<JsonPathSegment> ParseJsonPath(string path)
    {
        var segments = new List<JsonPathSegment>();
        var parts = path.Split('.');

        foreach (var part in parts)
        {
            // Check for array index: propertyName[0]
            var bracketIndex = part.IndexOf('[');
            if (bracketIndex > 0)
            {
                var propertyName = part.Substring(0, bracketIndex);
                segments.Add(new JsonPathSegment { PropertyName = propertyName });

                // Parse array indices
                var remaining = part.Substring(bracketIndex);
                var indices = System.Text.RegularExpressions.Regex.Matches(remaining, @"\[(\d+)\]");
                foreach (System.Text.RegularExpressions.Match match in indices)
                {
                    if (int.TryParse(match.Groups[1].Value, out var index))
                    {
                        segments.Add(new JsonPathSegment { IsArrayIndex = true, ArrayIndex = index });
                    }
                }
            }
            else if (!string.IsNullOrEmpty(part))
            {
                segments.Add(new JsonPathSegment { PropertyName = part });
            }
        }

        return segments;
    }

    private bool ValidateDataType(string value, ValidationRule rule, ValidationResult result)
    {
        if (string.IsNullOrEmpty(rule.DataType)) return true;

        var dataType = rule.DataType.ToLowerInvariant();
        bool isValid = true;
        string expectedFormat = "";

        switch (dataType)
        {
            case "number":
            case "integer":
            case "int":
                isValid = double.TryParse(value, out _);
                expectedFormat = "a valid number";
                break;

            case "boolean":
            case "bool":
                isValid = bool.TryParse(value, out _) || value == "1" || value == "0";
                expectedFormat = "true, false, 1, or 0";
                break;

            case "uuid":
            case "guid":
                isValid = Guid.TryParse(value, out _);
                expectedFormat = "a valid UUID/GUID (e.g., 550e8400-e29b-41d4-a716-446655440000)";
                break;

            case "email":
                isValid = IsValidEmail(value);
                expectedFormat = "a valid email address";
                break;

            case "date":
                isValid = DateTime.TryParse(value, out _);
                expectedFormat = "a valid date";
                break;

            case "url":
                isValid = Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
                          (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
                expectedFormat = "a valid URL";
                break;

            case "array":
                isValid = value.TrimStart().StartsWith('[');
                expectedFormat = "a JSON array";
                break;

            case "object":
                isValid = value.TrimStart().StartsWith('{');
                expectedFormat = "a JSON object";
                break;

            case "string":
            default:
                // String is always valid
                return true;
        }

        if (!isValid)
        {
            result.AddError(
                rule.ParameterName,
                rule.ErrorMessage ?? $"Parameter '{rule.ParameterName}' must be {expectedFormat}",
                $"Received: {TruncateValue(value)}, Expected type: {rule.DataType}",
                rule.StatusCode);
        }

        return isValid;
    }

    private void ValidateMinMax(string value, ValidationRule rule, ValidationResult result)
    {
        // MinMax only applies to numeric types
        if (string.IsNullOrEmpty(rule.MinValue) && string.IsNullOrEmpty(rule.MaxValue))
        {
            return;
        }

        // Try to parse as number
        if (!double.TryParse(value, out var numericValue))
        {
            // For strings, check length
            if (!string.IsNullOrEmpty(rule.MinValue) && int.TryParse(rule.MinValue, out var minLen))
            {
                if (value.Length < minLen)
                {
                    result.AddError(
                        rule.ParameterName,
                        rule.ErrorMessage ?? $"Parameter '{rule.ParameterName}' must be at least {minLen} characters",
                        $"Current length: {value.Length}",
                        rule.StatusCode);
                }
            }

            if (!string.IsNullOrEmpty(rule.MaxValue) && int.TryParse(rule.MaxValue, out var maxLen))
            {
                if (value.Length > maxLen)
                {
                    result.AddError(
                        rule.ParameterName,
                        rule.ErrorMessage ?? $"Parameter '{rule.ParameterName}' must be at most {maxLen} characters",
                        $"Current length: {value.Length}",
                        rule.StatusCode);
                }
            }

            return;
        }

        // Numeric validation
        if (!string.IsNullOrEmpty(rule.MinValue) && double.TryParse(rule.MinValue, out var minVal))
        {
            if (numericValue < minVal)
            {
                result.AddError(
                    rule.ParameterName,
                    rule.ErrorMessage ?? $"Parameter '{rule.ParameterName}' must be at least {minVal}",
                    $"Current value: {numericValue}",
                    rule.StatusCode);
            }
        }

        if (!string.IsNullOrEmpty(rule.MaxValue) && double.TryParse(rule.MaxValue, out var maxVal))
        {
            if (numericValue > maxVal)
            {
                result.AddError(
                    rule.ParameterName,
                    rule.ErrorMessage ?? $"Parameter '{rule.ParameterName}' must be at most {maxVal}",
                    $"Current value: {numericValue}",
                    rule.StatusCode);
            }
        }
    }

    private void ValidateAllowedValues(string value, ValidationRule rule, ValidationResult result)
    {
        if (string.IsNullOrEmpty(rule.AllowedValues))
        {
            return;
        }

        // Parse allowed values (comma-separated or JSON array)
        var allowedList = ParseAllowedValues(rule.AllowedValues);
        
        if (allowedList.Count == 0)
        {
            return;
        }

        // Case-insensitive comparison
        var isAllowed = allowedList.Any(av => 
            string.Equals(av.Trim(), value.Trim(), StringComparison.OrdinalIgnoreCase));

        if (!isAllowed)
        {
            result.AddError(
                rule.ParameterName,
                rule.ErrorMessage ?? $"Parameter '{rule.ParameterName}' must be one of: {string.Join(", ", allowedList)}",
                $"Received: {TruncateValue(value)}",
                rule.StatusCode);
        }
    }

    private List<string> ParseAllowedValues(string allowedValues)
    {
        var trimmed = allowedValues.Trim();
        
        // Try to parse as JSON array first
        if (trimmed.StartsWith('['))
        {
            try
            {
                using var doc = JsonDocument.Parse(trimmed);
                var list = new List<string>();
                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    list.Add(element.ToString());
                }
                return list;
            }
            catch
            {
                // Fall back to comma-separated
            }
        }

        // Parse as comma-separated
        return trimmed.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(v => v.Trim())
            .Where(v => !string.IsNullOrEmpty(v))
            .ToList();
    }

    private void ValidateRegex(string value, ValidationRule rule, ValidationResult result)
    {
        if (string.IsNullOrEmpty(rule.RegexPattern))
        {
            return;
        }

        try
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(value, rule.RegexPattern))
            {
            result.AddError(
                    rule.ParameterName,
                    rule.ErrorMessage ?? $"Parameter '{rule.ParameterName}' does not match the required format",
                    $"Pattern: {rule.RegexPattern}",
                    rule.StatusCode);
            }
        }
        catch (ArgumentException)
        {
            // Invalid regex pattern - log this but don't fail the request
        }
    }

    private bool IsValidEmail(string email)
    {
        if (string.IsNullOrEmpty(email)) return false;
        
        var atIndex = email.IndexOf('@');
        if (atIndex <= 0) return false;
        
        var dotIndex = email.LastIndexOf('.');
        if (dotIndex <= atIndex + 1) return false;
        if (dotIndex >= email.Length - 1) return false;

        return true;
    }

    private string TruncateValue(string value, int maxLength = 50)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Length <= maxLength) return value;
        return value.Substring(0, maxLength) + "...";
    }

    private class JsonPathSegment
    {
        public string PropertyName { get; set; } = "";
        public bool IsArrayIndex { get; set; }
        public int ArrayIndex { get; set; }
    }
}

/// <summary>
/// Represents the result of request validation.
/// </summary>
public class ValidationResult
{
    public List<ValidationError> Errors { get; } = new();
    
    public bool IsValid => Errors.Count == 0;
    
    /// <summary>
    /// Returns the status code from the first failed validation rule (default 400).
    /// </summary>
    public int FirstStatusCode { get; private set; } = 400;

    public void AddError(string field, string message, string? detail = null, int statusCode = 400)
    {
        // Track the first error's status code
        if (Errors.Count == 0)
        {
            FirstStatusCode = statusCode > 0 ? statusCode : 400;
        }
        
        Errors.Add(new ValidationError
        {
            Field = field,
            Message = message,
            Detail = detail
        });
    }

    /// <summary>
    /// Generates the default validation error response.
    /// </summary>
    public object ToResponse()
    {
        return new
        {
            error = "Validation Failed",
            message = $"{Errors.Count} validation error(s) found",
            validationErrors = Errors.Select(e => new
            {
                field = e.Field,
                message = e.Message,
                detail = e.Detail
            }),
            timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Renders a custom error response template with validation placeholders.
    /// Supported placeholders:
    /// - {{validation.errors}} - Full errors as JSON array
    /// - {{validation.errorCount}} - Number of errors
    /// - {{validation.messages}} - Array of error messages only
    /// - {{validation.firstMessage}} - First error message
    /// - {{validation.fields}} - Array of field names with errors
    /// - {{validation.firstField}} - First field with error
    /// - {{now}} or {{timestamp}} - Current timestamp
    /// </summary>
    public string RenderTemplate(string template)
    {
        var result = template;

        // {{validation.errors}} - Full error objects as JSON array
        var errorsJson = JsonSerializer.Serialize(Errors.Select(e => new
        {
            field = e.Field,
            message = e.Message,
            detail = e.Detail
        }), new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        result = result.Replace("{{validation.errors}}", errorsJson);

        // {{validation.errorCount}} - Number of errors
        result = result.Replace("{{validation.errorCount}}", Errors.Count.ToString());

        // {{validation.messages}} - Array of messages only
        var messagesJson = JsonSerializer.Serialize(Errors.Select(e => e.Message).ToArray());
        result = result.Replace("{{validation.messages}}", messagesJson);

        // {{validation.firstMessage}} - First error message
        result = result.Replace("{{validation.firstMessage}}", Errors.FirstOrDefault()?.Message ?? "Validation failed");

        // {{validation.fields}} - Array of field names
        var fieldsJson = JsonSerializer.Serialize(Errors.Select(e => e.Field).Distinct().ToArray());
        result = result.Replace("{{validation.fields}}", fieldsJson);

        // {{validation.firstField}} - First field name
        result = result.Replace("{{validation.firstField}}", Errors.FirstOrDefault()?.Field ?? "unknown");

        // {{now}} or {{timestamp}} - Current timestamp
        var timestamp = DateTime.UtcNow.ToString("o");
        result = result.Replace("{{now}}", timestamp);
        result = result.Replace("{{timestamp}}", timestamp);

        return result;
    }
}

public class ValidationError
{
    public string Field { get; set; } = "";
    public string Message { get; set; } = "";
    public string? Detail { get; set; }
}

