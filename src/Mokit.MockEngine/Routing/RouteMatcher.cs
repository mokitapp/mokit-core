using System.Text.RegularExpressions;

namespace Mokit.MockEngine.Routing;

public class RouteMatcher
{
    public RouteMatchResult Match(string pattern, string requestPath, bool isWildcard = false, string? regexPattern = null)
    {
        // Normalize paths
        pattern = NormalizePath(pattern);
        requestPath = NormalizePath(requestPath);

        // Try regex pattern first if provided
        if (!string.IsNullOrEmpty(regexPattern))
        {
            return MatchWithRegex(regexPattern, requestPath);
        }

        // Wildcard matching
        if (isWildcard || pattern.Contains("**") || pattern.Contains("*"))
        {
            return MatchWithWildcard(pattern, requestPath);
        }

        // Parameter matching (e.g., /api/users/{id})
        return MatchWithParameters(pattern, requestPath);
    }

    private static string NormalizePath(string path)
    {
        path = path.Trim('/');
        // Remove query string
        var queryIndex = path.IndexOf('?');
        if (queryIndex >= 0)
        {
            path = path[..queryIndex];
        }
        return path;
    }

    private static RouteMatchResult MatchWithRegex(string regexPattern, string requestPath)
    {
        try
        {
            var regex = new Regex($"^{regexPattern}$", RegexOptions.IgnoreCase);
            var match = regex.Match(requestPath);

            if (!match.Success)
            {
                return RouteMatchResult.NoMatch();
            }

            var parameters = new Dictionary<string, string>();
            foreach (var groupName in regex.GetGroupNames().Where(n => !int.TryParse(n, out _)))
            {
                parameters[groupName] = match.Groups[groupName].Value;
            }

            return RouteMatchResult.Matched(parameters);
        }
        catch
        {
            return RouteMatchResult.NoMatch();
        }
    }

    private static RouteMatchResult MatchWithWildcard(string pattern, string requestPath)
    {
        // Convert wildcard pattern to regex
        // ** matches any number of path segments
        // * matches any characters within a single path segment
        var regexPattern = pattern
            .Replace(".", "\\.")
            .Replace("**", "§§") // Temporary placeholder
            .Replace("*", "[^/]*")
            .Replace("§§", ".*");

        // Also support {param} syntax in wildcard patterns
        regexPattern = Regex.Replace(regexPattern, @"\{(\w+)\}", "(?<$1>[^/]+)");

        return MatchWithRegex(regexPattern, requestPath);
    }

    private static RouteMatchResult MatchWithParameters(string pattern, string requestPath)
    {
        var patternSegments = pattern.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var pathSegments = requestPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (patternSegments.Length != pathSegments.Length)
        {
            return RouteMatchResult.NoMatch();
        }

        var parameters = new Dictionary<string, string>();

        for (int i = 0; i < patternSegments.Length; i++)
        {
            var patternSegment = patternSegments[i];
            var pathSegment = pathSegments[i];

            // Check if it's a parameter (e.g., {id} or :id)
            if (patternSegment.StartsWith('{') && patternSegment.EndsWith('}'))
            {
                // ASP.NET style: {id} or {id:int}
                var paramName = patternSegment.Trim('{', '}');
                
                // Check for constraints like {id:int}
                var constraintIndex = paramName.IndexOf(':');
                if (constraintIndex >= 0)
                {
                    var constraint = paramName[(constraintIndex + 1)..];
                    paramName = paramName[..constraintIndex];

                    if (!ValidateConstraint(pathSegment, constraint))
                    {
                        return RouteMatchResult.NoMatch();
                    }
                }

                parameters[paramName] = pathSegment;
            }
            else if (patternSegment.StartsWith(':'))
            {
                // Express.js/Postman style: :id
                var paramName = patternSegment[1..]; // Remove leading ':'
                parameters[paramName] = pathSegment;
            }
            else if (!string.Equals(patternSegment, pathSegment, StringComparison.OrdinalIgnoreCase))
            {
                return RouteMatchResult.NoMatch();
            }
        }

        return RouteMatchResult.Matched(parameters);
    }

    private static bool ValidateConstraint(string value, string constraint)
    {
        return constraint.ToLower() switch
        {
            "int" => int.TryParse(value, out _),
            "long" => long.TryParse(value, out _),
            "guid" => Guid.TryParse(value, out _),
            "bool" => bool.TryParse(value, out _),
            "decimal" => decimal.TryParse(value, out _),
            "double" => double.TryParse(value, out _),
            "alpha" => value.All(char.IsLetter),
            "regex" => true, // Regex handled separately
            _ => true
        };
    }
}

public class RouteMatchResult
{
    public bool IsMatch { get; private set; }
    public Dictionary<string, string> Parameters { get; private set; } = new();

    public static RouteMatchResult NoMatch() => new() { IsMatch = false };
    public static RouteMatchResult Matched(Dictionary<string, string> parameters) => 
        new() { IsMatch = true, Parameters = parameters };
}


