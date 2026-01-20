using Microsoft.EntityFrameworkCore;
using Mokit.Application.Common;
using Mokit.Application.DTOs.Endpoint;
using Mokit.Application.Interfaces;
using Mokit.Application.Interfaces.Persistence;
using Mokit.Domain.Entities;
using Mokit.Domain.Enums;
using Mokit.Infrastructure.Data;

namespace Mokit.Infrastructure.Services;

public class MockEndpointService : IMockEndpointService
{
    private readonly IUnitOfWork<MokitDbContext> _unitOfWork;

    public MockEndpointService(IUnitOfWork<MokitDbContext> unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<List<MockEndpointDto>>> GetByProjectAsync(Guid projectId)
    {
        await using var scope = await _unitOfWork.CreateScopeAsync();

        var endpoints = await scope.Context.MockEndpoints
            .Include(e => e.Responses)
            .Include(e => e.ValidationRules)
            .Include(e => e.Webhooks)
            .Where(e => e.ProjectId == projectId)
            .OrderBy(e => e.Order)
            .Select(e => MapToDto(e))
            .ToListAsync();

        return Result<List<MockEndpointDto>>.Success(endpoints);
    }

    public async Task<Result<MockEndpointDto>> GetByIdAsync(Guid endpointId)
    {
        await using var scope = await _unitOfWork.CreateScopeAsync();

        var endpoint = await scope.Context.MockEndpoints
            .Include(e => e.Responses)
            .Include(e => e.ValidationRules)
            .Include(e => e.Webhooks)
            .FirstOrDefaultAsync(e => e.Id == endpointId);

        if (endpoint == null)
        {
            return Result<MockEndpointDto>.Failure("Endpoint not found");
        }

        return Result<MockEndpointDto>.Success(MapToDto(endpoint));
    }

    public async Task<Result<MockEndpointDto>> CreateAsync(CreateMockEndpointDto dto)
    {
        return await CreateWithResponseAsync(dto, 200, "application/json", "{}", null);
    }

    public async Task<Result<MockEndpointDto>> CreateWithResponseAsync(
        CreateMockEndpointDto dto,
        int statusCode,
        string contentType,
        string? body,
        string? headers)
    {
        var result = await _unitOfWork.ExecuteTransactionAsync(async scope =>
        {
            var project = await scope.Context.MockProjects.FindAsync(dto.ProjectId);
            if (project == null)
            {
                return (false, Guid.Empty, "Project not found");
            }

            var maxOrder = await scope.Context.MockEndpoints
                .Where(e => e.ProjectId == dto.ProjectId)
                .MaxAsync(e => (int?)e.Order) ?? -1;

            var endpoint = new MockEndpoint
            {
                ProjectId = dto.ProjectId,
                Name = dto.Name,
                Description = dto.Description,
                Route = dto.Route,
                Method = dto.Method,
                IsWildcard = dto.IsWildcard,
                RegexPattern = dto.RegexPattern,
                ResponseMode = dto.ResponseMode,
                DelayMin = dto.DelayMin,
                DelayMax = dto.DelayMax,
                Order = maxOrder + 1
            };

            scope.Context.MockEndpoints.Add(endpoint);

            // Add Validation Rules
            if (dto.ValidationRules != null)
            {
                foreach (var ruleDto in dto.ValidationRules)
                {
                    var rule = new ValidationRule
                    {
                        EndpointId = endpoint.Id,
                        ParameterName = ruleDto.ParameterName,
                        Location = Enum.Parse<ParameterLocation>(ruleDto.Location),
                        IsRequired = ruleDto.IsRequired,
                        DataType = ruleDto.DataType,
                        RegexPattern = ruleDto.RegexPattern,
                        MinValue = ruleDto.MinValue,
                        MaxValue = ruleDto.MaxValue,
                        AllowedValues = ruleDto.AllowedValues,
                        ErrorMessage = ruleDto.ErrorMessage,
                        StatusCode = ruleDto.StatusCode > 0 ? ruleDto.StatusCode : 400,
                        IsActive = true
                    };
                    scope.Context.ValidationRules.Add(rule);
                }
            }

            // Add Webhooks
            if (dto.Webhooks != null)
            {
                foreach (var hookDto in dto.Webhooks)
                {
                    var hook = new WebhookDefinition
                    {
                        EndpointId = endpoint.Id,
                        Name = hookDto.Name,
                        Url = hookDto.Url,
                        Method = hookDto.Method,
                        Body = hookDto.Body,
                        Headers = hookDto.Headers,
                        DelayMs = hookDto.DelayMs,
                        IsEnabled = hookDto.IsEnabled
                    };
                    scope.Context.WebhookDefinitions.Add(hook);
                }
            }

            // Create response with provided values
            var defaultResponse = new MockResponse
            {
                EndpointId = endpoint.Id,
                Name = "Default Response",
                StatusCode = statusCode,
                Body = body ?? "{}",
                ContentType = contentType,
                Headers = headers,
                IsDefault = true,
                Order = 0
            };

            scope.Context.MockResponses.Add(defaultResponse);
            return (true, endpoint.Id, (string?)null);
        });

        if (!result.Item1)
        {
            return Result<MockEndpointDto>.Failure(result.Item3 ?? "Create failed");
        }

        return await GetByIdAsync(result.Item2);
    }

    public async Task<Result<MockEndpointDto>> UpdateAsync(Guid endpointId, UpdateMockEndpointDto dto)
    {
        var result = await _unitOfWork.ExecuteTransactionAsync(async scope =>
        {
            var endpoint = await scope.Context.MockEndpoints
                .Include(e => e.ValidationRules)
                .Include(e => e.Webhooks)
                .FirstOrDefaultAsync(e => e.Id == endpointId);

            if (endpoint == null)
            {
                return (false, "Endpoint not found");
            }

            endpoint.Name = dto.Name;
            endpoint.Description = dto.Description;
            endpoint.Route = dto.Route;
            endpoint.Method = dto.Method;
            endpoint.IsActive = dto.IsActive;
            endpoint.Order = dto.Order;
            endpoint.IsWildcard = dto.IsWildcard;
            endpoint.RegexPattern = dto.RegexPattern;
            endpoint.ResponseMode = dto.ResponseMode;
            endpoint.DelayMin = dto.DelayMin;
            endpoint.DelayMax = dto.DelayMax;
            endpoint.ValidationErrorResponseTemplate = dto.ValidationErrorResponseTemplate;
            endpoint.UpdatedAt = DateTime.UtcNow;

            // Update Validation Rules
            if (dto.ValidationRules != null)
            {
                scope.Context.ValidationRules.RemoveRange(endpoint.ValidationRules);

                foreach (var ruleDto in dto.ValidationRules)
                {
                    var rule = new ValidationRule
                    {
                        EndpointId = endpoint.Id,
                        ParameterName = ruleDto.ParameterName,
                        Location = Enum.Parse<ParameterLocation>(ruleDto.Location),
                        IsRequired = ruleDto.IsRequired,
                        DataType = ruleDto.DataType,
                        RegexPattern = ruleDto.RegexPattern,
                        MinValue = ruleDto.MinValue,
                        MaxValue = ruleDto.MaxValue,
                        AllowedValues = ruleDto.AllowedValues,
                        ErrorMessage = ruleDto.ErrorMessage,
                        StatusCode = ruleDto.StatusCode > 0 ? ruleDto.StatusCode : 400,
                        IsActive = true
                    };
                    scope.Context.ValidationRules.Add(rule);
                }
            }

            // Update Webhooks
            if (dto.Webhooks != null)
            {
                scope.Context.WebhookDefinitions.RemoveRange(endpoint.Webhooks);

                foreach (var hookDto in dto.Webhooks)
                {
                    var hook = new WebhookDefinition
                    {
                        EndpointId = endpoint.Id,
                        Name = hookDto.Name,
                        Url = hookDto.Url,
                        Method = hookDto.Method,
                        Body = hookDto.Body,
                        Headers = hookDto.Headers,
                        DelayMs = hookDto.DelayMs,
                        IsEnabled = hookDto.IsEnabled
                    };
                    scope.Context.WebhookDefinitions.Add(hook);
                }
            }

            return (true, (string?)null);
        });

        if (!result.Item1)
        {
            return Result<MockEndpointDto>.Failure(result.Item2 ?? "Update failed");
        }

        return await GetByIdAsync(endpointId);
    }

    public async Task<Result<MockEndpointDto>> UpdateWithResponseAsync(
        Guid endpointId,
        UpdateMockEndpointDto dto,
        int statusCode,
        string contentType,
        string? body,
        string? headers)
    {
        var result = await _unitOfWork.ExecuteTransactionAsync(async scope =>
        {
            var endpoint = await scope.Context.MockEndpoints
                .Include(e => e.Responses)
                .Include(e => e.ValidationRules)
                .Include(e => e.Webhooks)
                .FirstOrDefaultAsync(e => e.Id == endpointId);

            if (endpoint == null)
            {
                return (false, "Endpoint not found");
            }

            endpoint.Name = dto.Name;
            endpoint.Description = dto.Description;
            endpoint.Route = dto.Route;
            endpoint.Method = dto.Method;
            endpoint.IsActive = dto.IsActive;
            endpoint.Order = dto.Order;
            endpoint.IsWildcard = dto.IsWildcard;
            endpoint.RegexPattern = dto.RegexPattern;
            endpoint.ResponseMode = dto.ResponseMode;
            endpoint.DelayMin = dto.DelayMin;
            endpoint.DelayMax = dto.DelayMax;
            endpoint.ValidationErrorResponseTemplate = dto.ValidationErrorResponseTemplate;
            endpoint.UpdatedAt = DateTime.UtcNow;

            // Update Validation Rules
            if (dto.ValidationRules != null)
            {
                scope.Context.ValidationRules.RemoveRange(endpoint.ValidationRules);

                foreach (var ruleDto in dto.ValidationRules)
                {
                    var rule = new ValidationRule
                    {
                        EndpointId = endpoint.Id,
                        ParameterName = ruleDto.ParameterName,
                        Location = Enum.Parse<ParameterLocation>(ruleDto.Location),
                        IsRequired = ruleDto.IsRequired,
                        DataType = ruleDto.DataType,
                        RegexPattern = ruleDto.RegexPattern,
                        MinValue = ruleDto.MinValue,
                        MaxValue = ruleDto.MaxValue,
                        AllowedValues = ruleDto.AllowedValues,
                        ErrorMessage = ruleDto.ErrorMessage,
                        StatusCode = ruleDto.StatusCode > 0 ? ruleDto.StatusCode : 400,
                        IsActive = true
                    };
                    scope.Context.ValidationRules.Add(rule);
                }
            }

            // Update Webhooks
            if (dto.Webhooks != null)
            {
                scope.Context.WebhookDefinitions.RemoveRange(endpoint.Webhooks);

                foreach (var hookDto in dto.Webhooks)
                {
                    var hook = new WebhookDefinition
                    {
                        EndpointId = endpoint.Id,
                        Name = hookDto.Name,
                        Url = hookDto.Url,
                        Method = hookDto.Method,
                        Body = hookDto.Body,
                        Headers = hookDto.Headers,
                        DelayMs = hookDto.DelayMs,
                        IsEnabled = hookDto.IsEnabled
                    };
                    scope.Context.WebhookDefinitions.Add(hook);
                }
            }

            // Update or create default response
            var defaultResponse = endpoint.Responses.FirstOrDefault(r => r.IsDefault);
            if (defaultResponse != null)
            {
                defaultResponse.StatusCode = statusCode;
                defaultResponse.ContentType = contentType;
                defaultResponse.Body = body ?? "{}";
                defaultResponse.Headers = headers;
                defaultResponse.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                var newResponse = new MockResponse
                {
                    EndpointId = endpoint.Id,
                    Name = "Default Response",
                    StatusCode = statusCode,
                    Body = body ?? "{}",
                    ContentType = contentType,
                    Headers = headers,
                    IsDefault = true,
                    Order = 0
                };
                scope.Context.MockResponses.Add(newResponse);
            }

            return (true, (string?)null);
        });

        if (!result.Item1)
        {
            return Result<MockEndpointDto>.Failure(result.Item2 ?? "Update failed");
        }

        return await GetByIdAsync(endpointId);
    }

    public async Task<Result> DeleteAsync(Guid endpointId)
    {
        var result = await _unitOfWork.ExecuteTransactionAsync(async scope =>
        {
            var endpoint = await scope.Context.MockEndpoints.FindAsync(endpointId);
            if (endpoint == null)
            {
                return (false, "Endpoint not found");
            }

            scope.Context.MockEndpoints.Remove(endpoint);
            return (true, (string?)null);
        });

        if (!result.Item1)
        {
            return Result.Failure(result.Item2 ?? "Delete failed");
        }

        return Result.Success();
    }

    public async Task<Result> ReorderAsync(Guid projectId, List<Guid> endpointIds)
    {
        await _unitOfWork.ExecuteTransactionAsync(async scope =>
        {
            var endpoints = await scope.Context.MockEndpoints
                .Where(e => e.ProjectId == projectId)
                .ToListAsync();

            for (int i = 0; i < endpointIds.Count; i++)
            {
                var endpoint = endpoints.FirstOrDefault(e => e.Id == endpointIds[i]);
                if (endpoint != null)
                {
                    endpoint.Order = i;
                }
            }
        });

        return Result.Success();
    }

    public async Task<Result<MockEndpointDto>> DuplicateAsync(Guid endpointId)
    {
        var result = await _unitOfWork.ExecuteTransactionAsync(async scope =>
        {
            var original = await scope.Context.MockEndpoints
                .Include(e => e.Responses)
                .Include(e => e.ValidationRules)
                .Include(e => e.Webhooks)
                .FirstOrDefaultAsync(e => e.Id == endpointId);

            if (original == null)
            {
                return (false, Guid.Empty, "Endpoint not found");
            }

            var maxOrder = await scope.Context.MockEndpoints
                .Where(e => e.ProjectId == original.ProjectId)
                .MaxAsync(e => (int?)e.Order) ?? -1;

            var duplicate = new MockEndpoint
            {
                ProjectId = original.ProjectId,
                Name = $"{original.Name} (Copy)",
                Description = original.Description,
                Route = original.Route,
                Method = original.Method,
                IsWildcard = original.IsWildcard,
                RegexPattern = original.RegexPattern,
                ResponseMode = original.ResponseMode,
                DelayMin = original.DelayMin,
                DelayMax = original.DelayMax,
                Order = maxOrder + 1
            };

            scope.Context.MockEndpoints.Add(duplicate);

            foreach (var response in original.Responses)
            {
                var duplicateResponse = new MockResponse
                {
                    EndpointId = duplicate.Id,
                    Name = response.Name,
                    Description = response.Description,
                    StatusCode = response.StatusCode,
                    Body = response.Body,
                    ContentType = response.ContentType,
                    Headers = response.Headers,
                    IsDefault = response.IsDefault,
                    Order = response.Order,
                    Condition = response.Condition,
                    ConditionExpression = response.ConditionExpression
                };
                scope.Context.MockResponses.Add(duplicateResponse);
            }

            foreach (var rule in original.ValidationRules)
            {
                var duplicateRule = new ValidationRule
                {
                    EndpointId = duplicate.Id,
                    ParameterName = rule.ParameterName,
                    Location = rule.Location,
                    IsRequired = rule.IsRequired,
                    DataType = rule.DataType,
                    RegexPattern = rule.RegexPattern,
                    MinValue = rule.MinValue,
                    MaxValue = rule.MaxValue,
                    AllowedValues = rule.AllowedValues,
                    DefaultValue = rule.DefaultValue,
                    ErrorMessage = rule.ErrorMessage
                };
                scope.Context.ValidationRules.Add(duplicateRule);
            }

            foreach (var hook in original.Webhooks)
            {
                var duplicateHook = new WebhookDefinition
                {
                    EndpointId = duplicate.Id,
                    Name = hook.Name,
                    Url = hook.Url,
                    Method = hook.Method,
                    Body = hook.Body,
                    Headers = hook.Headers,
                    DelayMs = hook.DelayMs,
                    IsEnabled = hook.IsEnabled
                };
                scope.Context.WebhookDefinitions.Add(duplicateHook);
            }

            return (true, duplicate.Id, (string?)null);
        });

        if (!result.Item1)
        {
            return Result<MockEndpointDto>.Failure(result.Item3 ?? "Duplicate failed");
        }

        return await GetByIdAsync(result.Item2);
    }

    private static MockEndpointDto MapToDto(MockEndpoint e)
    {
        return new MockEndpointDto
        {
            Id = e.Id,
            ProjectId = e.ProjectId,
            Name = e.Name,
            Description = e.Description,
            Route = e.Route,
            Method = e.Method,
            IsActive = e.IsActive,
            Order = e.Order,
            IsWildcard = e.IsWildcard,
            RegexPattern = e.RegexPattern,
            ResponseMode = e.ResponseMode,
            DelayMin = e.DelayMin,
            DelayMax = e.DelayMax,
            ResponseCount = e.Responses?.Count ?? 0,
            CreatedAt = e.CreatedAt,
            ValidationErrorResponseTemplate = e.ValidationErrorResponseTemplate,
            Responses = e.Responses?.Select(r => new MockResponseDto
            {
                Id = r.Id,
                Name = r.Name,
                StatusCode = r.StatusCode,
                Body = r.Body,
                ContentType = r.ContentType,
                Headers = r.Headers,
                IsDefault = r.IsDefault
            }).ToList() ?? new List<MockResponseDto>(),
            ValidationRules = e.ValidationRules?.Select(r => new ValidationRuleDto
            {
                Id = r.Id,
                ParameterName = r.ParameterName,
                Location = r.Location.ToString(),
                DataType = r.DataType ?? "String",
                IsRequired = r.IsRequired,
                RegexPattern = r.RegexPattern,
                MinValue = r.MinValue,
                MaxValue = r.MaxValue,
                AllowedValues = r.AllowedValues,
                ErrorMessage = r.ErrorMessage,
                StatusCode = r.StatusCode

            }).ToList() ?? new List<ValidationRuleDto>(),
            Webhooks = e.Webhooks?.Select(w => new WebhookDefinitionDto
            {
                Id = w.Id,
                Name = w.Name,
                Url = w.Url,
                Method = w.Method,
                Body = w.Body,
                Headers = w.Headers,
                DelayMs = w.DelayMs,
                IsEnabled = w.IsEnabled
            }).ToList() ?? new List<WebhookDefinitionDto>()
        };
    }
}
