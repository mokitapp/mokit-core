using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Mokit.MockEngine.Security;

public class JwtSimulator
{
    private const string DefaultSecret = "Mokit-Default-Secret-Key-For-Testing-Only-32bytes!";
    private const string DefaultIssuer = "Mokit";
    private const string DefaultAudience = "MokitClient";

    /// <summary>
    /// Generates a JWT token with the specified claims
    /// </summary>
    public string GenerateToken(JwtGenerationOptions options)
    {
        var secret = options.Secret ?? DefaultSecret;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, options.Subject ?? Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        // Add custom claims
        if (options.Claims != null)
        {
            foreach (var claim in options.Claims)
            {
                claims.Add(new Claim(claim.Key, claim.Value));
            }
        }

        // Add roles
        if (options.Roles != null)
        {
            foreach (var role in options.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }

        var token = new JwtSecurityToken(
            issuer: options.Issuer ?? DefaultIssuer,
            audience: options.Audience ?? DefaultAudience,
            claims: claims,
            expires: DateTime.UtcNow.Add(options.ExpiresIn ?? TimeSpan.FromHours(1)),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Validates a JWT token and returns the validation result
    /// </summary>
    public JwtValidationResult ValidateToken(string token, JwtValidationOptions options)
    {
        try
        {
            var secret = options.Secret ?? DefaultSecret;
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = options.ValidateIssuer,
                ValidIssuer = options.Issuer ?? DefaultIssuer,
                ValidateAudience = options.ValidateAudience,
                ValidAudience = options.Audience ?? DefaultAudience,
                ValidateLifetime = options.ValidateLifetime,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            var jwtToken = (JwtSecurityToken)validatedToken;

            return new JwtValidationResult
            {
                IsValid = true,
                Claims = principal.Claims.ToDictionary(c => c.Type, c => c.Value),
                Subject = jwtToken.Subject,
                Issuer = jwtToken.Issuer,
                Audience = jwtToken.Audiences.FirstOrDefault(),
                ExpiresAt = jwtToken.ValidTo
            };
        }
        catch (SecurityTokenExpiredException)
        {
            return new JwtValidationResult
            {
                IsValid = false,
                ErrorCode = "TOKEN_EXPIRED",
                ErrorMessage = "Token has expired"
            };
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            return new JwtValidationResult
            {
                IsValid = false,
                ErrorCode = "INVALID_SIGNATURE",
                ErrorMessage = "Token signature is invalid"
            };
        }
        catch (SecurityTokenException ex)
        {
            return new JwtValidationResult
            {
                IsValid = false,
                ErrorCode = "INVALID_TOKEN",
                ErrorMessage = ex.Message
            };
        }
        catch (Exception ex)
        {
            return new JwtValidationResult
            {
                IsValid = false,
                ErrorCode = "VALIDATION_ERROR",
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Decodes a JWT token without validation (for inspection only)
    /// </summary>
    public JwtDecodedToken? DecodeToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            return new JwtDecodedToken
            {
                Header = jwtToken.Header.ToDictionary(h => h.Key, h => h.Value?.ToString() ?? ""),
                Payload = jwtToken.Payload.ToDictionary(p => p.Key, p => p.Value?.ToString() ?? ""),
                Subject = jwtToken.Subject,
                Issuer = jwtToken.Issuer,
                Audience = jwtToken.Audiences.FirstOrDefault(),
                IssuedAt = jwtToken.IssuedAt,
                ExpiresAt = jwtToken.ValidTo,
                NotBefore = jwtToken.ValidFrom
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Checks if a request has a valid Authorization header with Bearer token
    /// </summary>
    public (bool hasToken, string? token) ExtractBearerToken(string? authorizationHeader)
    {
        if (string.IsNullOrEmpty(authorizationHeader))
        {
            return (false, null);
        }

        if (!authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return (false, null);
        }

        var token = authorizationHeader.Substring("Bearer ".Length).Trim();
        return (!string.IsNullOrEmpty(token), token);
    }
}

public class JwtGenerationOptions
{
    public string? Subject { get; set; }
    public string? Secret { get; set; }
    public string? Issuer { get; set; }
    public string? Audience { get; set; }
    public TimeSpan? ExpiresIn { get; set; }
    public Dictionary<string, string>? Claims { get; set; }
    public List<string>? Roles { get; set; }
}

public class JwtValidationOptions
{
    public string? Secret { get; set; }
    public string? Issuer { get; set; }
    public string? Audience { get; set; }
    public bool ValidateIssuer { get; set; } = true;
    public bool ValidateAudience { get; set; } = true;
    public bool ValidateLifetime { get; set; } = true;
}

public class JwtValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, string>? Claims { get; set; }
    public string? Subject { get; set; }
    public string? Issuer { get; set; }
    public string? Audience { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class JwtDecodedToken
{
    public Dictionary<string, string> Header { get; set; } = new();
    public Dictionary<string, string> Payload { get; set; } = new();
    public string? Subject { get; set; }
    public string? Issuer { get; set; }
    public string? Audience { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime NotBefore { get; set; }
}


