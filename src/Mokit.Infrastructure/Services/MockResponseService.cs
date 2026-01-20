using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Mokit.Application.Common;
using Mokit.Application.DTOs.Response;
using Mokit.Application.Interfaces;
using Mokit.Application.Interfaces.Persistence;
using Mokit.Domain.Entities;
using Mokit.Infrastructure.Data;

namespace Mokit.Infrastructure.Services;

public class MockResponseService : IMockResponseService
{
    private readonly IUnitOfWork<MokitDbContext> _unitOfWork;

    public MockResponseService(IUnitOfWork<MokitDbContext> unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<List<MockResponseDto>>> GetByEndpointAsync(Guid endpointId)
    {
        await using var scope = await _unitOfWork.CreateScopeAsync();

        var responses = await scope.Context.MockResponses
            .Where(r => r.EndpointId == endpointId)
            .OrderBy(r => r.Order)
            .Select(r => MapToDto(r))
            .ToListAsync();

        return Result<List<MockResponseDto>>.Success(responses);
    }

    public async Task<Result<MockResponseDto>> GetByIdAsync(Guid responseId)
    {
        await using var scope = await _unitOfWork.CreateScopeAsync();

        var response = await scope.Context.MockResponses.FindAsync(responseId);
        if (response == null)
        {
            return Result<MockResponseDto>.Failure("Response not found");
        }

        return Result<MockResponseDto>.Success(MapToDto(response));
    }

    public async Task<Result<MockResponseDto>> CreateAsync(CreateMockResponseDto dto)
    {
        var responseId = await _unitOfWork.ExecuteTransactionAsync(async scope =>
        {
            var endpoint = await scope.Context.MockEndpoints.FindAsync(dto.EndpointId);
            if (endpoint == null)
            {
                return (Guid.Empty, "Endpoint not found");
            }

            var maxOrder = await scope.Context.MockResponses
                .Where(r => r.EndpointId == dto.EndpointId)
                .MaxAsync(r => (int?)r.Order) ?? -1;

            // If this is set as default, unset other defaults
            if (dto.IsDefault)
            {
                var existingDefaults = await scope.Context.MockResponses
                    .Where(r => r.EndpointId == dto.EndpointId && r.IsDefault)
                    .ToListAsync();

                foreach (var existing in existingDefaults)
                {
                    existing.IsDefault = false;
                }
            }

            var response = new MockResponse
            {
                EndpointId = dto.EndpointId,
                Name = dto.Name,
                Description = dto.Description,
                StatusCode = dto.StatusCode,
                Body = dto.Body,
                ContentType = dto.ContentType,
                IsDefault = dto.IsDefault,
                Headers = dto.Headers != null ? JsonSerializer.Serialize(dto.Headers) : null,
                Condition = dto.Condition,
                ConditionExpression = dto.ConditionExpression,
                Order = maxOrder + 1
            };

            scope.Context.MockResponses.Add(response);
            return (response.Id, (string?)null);
        });

        if (responseId.Item1 == Guid.Empty)
        {
            return Result<MockResponseDto>.Failure(responseId.Item2 ?? "Create failed");
        }

        return await GetByIdAsync(responseId.Item1);
    }

    public async Task<Result<MockResponseDto>> UpdateAsync(Guid responseId, UpdateMockResponseDto dto)
    {
        var result = await _unitOfWork.ExecuteTransactionAsync(async scope =>
        {
            var response = await scope.Context.MockResponses.FindAsync(responseId);
            if (response == null)
            {
                return (false, "Response not found");
            }

            response.Name = dto.Name;
            response.Description = dto.Description;
            response.StatusCode = dto.StatusCode;
            response.Body = dto.Body;
            response.ContentType = dto.ContentType;
            response.Order = dto.Order;
            response.IsActive = dto.IsActive;
            response.Headers = dto.Headers != null ? JsonSerializer.Serialize(dto.Headers) : null;
            response.Condition = dto.Condition;
            response.ConditionExpression = dto.ConditionExpression;
            response.IsFileResponse = dto.IsFileResponse;
            response.FilePath = dto.FilePath;
            response.FileName = dto.FileName;
            response.UpdatedAt = DateTime.UtcNow;

            // Handle default flag
            if (dto.IsDefault && !response.IsDefault)
            {
                var existingDefaults = await scope.Context.MockResponses
                    .Where(r => r.EndpointId == response.EndpointId && r.IsDefault && r.Id != responseId)
                    .ToListAsync();

                foreach (var existing in existingDefaults)
                {
                    existing.IsDefault = false;
                }
            }
            response.IsDefault = dto.IsDefault;

            return (true, (string?)null);
        });

        if (!result.Item1)
        {
            return Result<MockResponseDto>.Failure(result.Item2 ?? "Update failed");
        }

        return await GetByIdAsync(responseId);
    }

    public async Task<Result> DeleteAsync(Guid responseId)
    {
        var result = await _unitOfWork.ExecuteTransactionAsync(async scope =>
        {
            var response = await scope.Context.MockResponses.FindAsync(responseId);
            if (response == null)
            {
                return (false, "Response not found");
            }

            scope.Context.MockResponses.Remove(response);
            return (true, (string?)null);
        });

        if (!result.Item1)
        {
            return Result.Failure(result.Item2 ?? "Delete failed");
        }

        return Result.Success();
    }

    public async Task<Result> SetDefaultAsync(Guid responseId)
    {
        var result = await _unitOfWork.ExecuteTransactionAsync(async scope =>
        {
            var response = await scope.Context.MockResponses.FindAsync(responseId);
            if (response == null)
            {
                return (false, "Response not found");
            }

            var allResponses = await scope.Context.MockResponses
                .Where(r => r.EndpointId == response.EndpointId)
                .ToListAsync();

            foreach (var r in allResponses)
            {
                r.IsDefault = r.Id == responseId;
            }

            return (true, (string?)null);
        });

        if (!result.Item1)
        {
            return Result.Failure(result.Item2 ?? "Set default failed");
        }

        return Result.Success();
    }

    public async Task<Result> ReorderAsync(Guid endpointId, List<Guid> responseIds)
    {
        await _unitOfWork.ExecuteTransactionAsync(async scope =>
        {
            var responses = await scope.Context.MockResponses
                .Where(r => r.EndpointId == endpointId)
                .ToListAsync();

            for (int i = 0; i < responseIds.Count; i++)
            {
                var response = responses.FirstOrDefault(r => r.Id == responseIds[i]);
                if (response != null)
                {
                    response.Order = i;
                }
            }
        });

        return Result.Success();
    }

    private static MockResponseDto MapToDto(MockResponse r)
    {
        Dictionary<string, string>? headers = null;
        if (!string.IsNullOrEmpty(r.Headers))
        {
            try
            {
                headers = JsonSerializer.Deserialize<Dictionary<string, string>>(r.Headers);
            }
            catch { }
        }

        return new MockResponseDto
        {
            Id = r.Id,
            EndpointId = r.EndpointId,
            Name = r.Name,
            Description = r.Description,
            StatusCode = r.StatusCode,
            Body = r.Body,
            ContentType = r.ContentType,
            IsDefault = r.IsDefault,
            Order = r.Order,
            IsActive = r.IsActive,
            Headers = headers,
            Condition = r.Condition,
            ConditionExpression = r.ConditionExpression,
            IsFileResponse = r.IsFileResponse,
            FilePath = r.FilePath,
            FileName = r.FileName,
            CreatedAt = r.CreatedAt
        };
    }
}
