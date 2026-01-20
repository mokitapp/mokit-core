using System.Text.Json;
using System.Text.RegularExpressions;
using Mokit.Domain.Entities;
using Mokit.Domain.Enums;

namespace Mokit.MockEngine.Processing;

public class ValidationProcessor
{
    public ValidationResult Validate(MockRequest request, MockEndpoint endpoint)
    {
        var errors = new List<ValidationError>();

        foreach (var rule in endpoint.ValidationRules.Where(r => r.IsActive))
        {
            var value = GetParameterValue(request, rule);
            var validationError = ValidateParameter(value, rule);
            
            if (validationError != null)
            {
                errors.Add(validationError);
            }
        }

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }

    private string? GetParameterValue(MockRequest request, ValidationRule rule)
    {
        return rule.Location switch
        {
            ParameterLocation.Query => request.QueryParams.TryGetValue(rule.ParameterName, out var q) ? q : null,
            ParameterLocation.Header => request.Headers.TryGetValue(rule.ParameterName, out var h) ? h : null,
            ParameterLocation.Path => null, // Path params are already validated by route matching
            ParameterLocation.Body => GetBodyParameterValue(request.Body, rule.ParameterName),
            ParameterLocation.Cookie => request.Headers.TryGetValue("Cookie", out var c) 
                ? GetCookieValue(c, rule.ParameterName) : null,
            _ => null
        };
    }

    private string? GetBodyParameterValue(string? body, string parameterPath)
    {
        if (string.IsNullOrEmpty(body)) return null;

        try
        {
            var json = JsonDocument.Parse(body);
            var segments = parameterPath.Split('.');
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
                JsonValueKind.Null => null,
                _ => current.GetRawText()
            };
        }
        catch
        {
            return null;
        }
    }

    private string? GetCookieValue(string cookieHeader, string cookieName)
    {
        var cookies = cookieHeader.Split(';')
            .Select(c => c.Trim().Split('=', 2))
            .Where(c => c.Length == 2)
            .ToDictionary(c => c[0], c => c[1], StringComparer.OrdinalIgnoreCase);

        return cookies.TryGetValue(cookieName, out var value) ? value : null;
    }

    private ValidationError? ValidateParameter(string? value, ValidationRule rule)
    {
        // Required check
        if (rule.IsRequired && string.IsNullOrEmpty(value))
        {
            return new ValidationError
            {
                ParameterName = rule.ParameterName,
                Location = rule.Location.ToString(),
                ErrorCode = "REQUIRED",
                Message = rule.ErrorMessage ?? $"Parameter '{rule.ParameterName}' is required"
            };
        }

        // Skip other validations if value is null/empty and not required
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        // Data type validation
        if (!string.IsNullOrEmpty(rule.DataType))
        {
            var typeError = ValidateDataType(value, rule.DataType, rule.ParameterName);
            if (typeError != null)
            {
                typeError.Message = rule.ErrorMessage ?? typeError.Message;
                return typeError;
            }
        }

        // Regex pattern validation
        if (!string.IsNullOrEmpty(rule.RegexPattern))
        {
            try
            {
                if (!Regex.IsMatch(value, rule.RegexPattern))
                {
                    return new ValidationError
                    {
                        ParameterName = rule.ParameterName,
                        Location = rule.Location.ToString(),
                        ErrorCode = "PATTERN_MISMATCH",
                        Message = rule.ErrorMessage ?? $"Parameter '{rule.ParameterName}' does not match the required pattern"
                    };
                }
            }
            catch
            {
                // Invalid regex pattern, skip validation
            }
        }

        // Min/Max value validation
        if (!string.IsNullOrEmpty(rule.MinValue) || !string.IsNullOrEmpty(rule.MaxValue))
        {
            var rangeError = ValidateRange(value, rule.MinValue, rule.MaxValue, rule.ParameterName, rule.DataType);
            if (rangeError != null)
            {
                rangeError.Message = rule.ErrorMessage ?? rangeError.Message;
                return rangeError;
            }
        }

        return null;
    }

    private ValidationError? ValidateDataType(string value, string dataType, string parameterName)
    {
        var isValid = dataType.ToLower() switch
        {
            "int" or "integer" => int.TryParse(value, out _),
            "long" => long.TryParse(value, out _),
            "decimal" or "number" => decimal.TryParse(value, out _),
            "double" or "float" => double.TryParse(value, out _),
            "bool" or "boolean" => bool.TryParse(value, out _),
            "guid" or "uuid" => Guid.TryParse(value, out _),
            "email" => IsValidEmail(value),
            "url" => Uri.TryCreate(value, UriKind.Absolute, out _),
            "date" => DateTime.TryParse(value, out _),
            "datetime" => DateTime.TryParse(value, out _),
            "string" => true,
            _ => true
        };

        if (!isValid)
        {
            return new ValidationError
            {
                ParameterName = parameterName,
                Location = "body",
                ErrorCode = "INVALID_TYPE",
                Message = $"Parameter '{parameterName}' must be of type '{dataType}'"
            };
        }

        return null;
    }

    private ValidationError? ValidateRange(string value, string? minValue, string? maxValue, 
        string parameterName, string? dataType)
    {
        // For numeric types
        if (decimal.TryParse(value, out var numericValue))
        {
            if (!string.IsNullOrEmpty(minValue) && decimal.TryParse(minValue, out var min))
            {
                if (numericValue < min)
                {
                    return new ValidationError
                    {
                        ParameterName = parameterName,
                        Location = "body",
                        ErrorCode = "BELOW_MINIMUM",
                        Message = $"Parameter '{parameterName}' must be at least {min}"
                    };
                }
            }

            if (!string.IsNullOrEmpty(maxValue) && decimal.TryParse(maxValue, out var max))
            {
                if (numericValue > max)
                {
                    return new ValidationError
                    {
                        ParameterName = parameterName,
                        Location = "body",
                        ErrorCode = "ABOVE_MAXIMUM",
                        Message = $"Parameter '{parameterName}' must be at most {max}"
                    };
                }
            }
        }
        // For string length
        else if (dataType?.ToLower() == "string")
        {
            if (!string.IsNullOrEmpty(minValue) && int.TryParse(minValue, out var minLen))
            {
                if (value.Length < minLen)
                {
                    return new ValidationError
                    {
                        ParameterName = parameterName,
                        Location = "body",
                        ErrorCode = "TOO_SHORT",
                        Message = $"Parameter '{parameterName}' must be at least {minLen} characters"
                    };
                }
            }

            if (!string.IsNullOrEmpty(maxValue) && int.TryParse(maxValue, out var maxLen))
            {
                if (value.Length > maxLen)
                {
                    return new ValidationError
                    {
                        ParameterName = parameterName,
                        Location = "body",
                        ErrorCode = "TOO_LONG",
                        Message = $"Parameter '{parameterName}' must be at most {maxLen} characters"
                    };
                }
            }
        }

        return null;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<ValidationError> Errors { get; set; } = new();
}

public class ValidationError
{
    public string ParameterName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}


