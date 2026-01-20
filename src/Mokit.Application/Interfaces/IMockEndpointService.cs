using Mokit.Application.Common;
using Mokit.Application.DTOs.Endpoint;

namespace Mokit.Application.Interfaces;

public interface IMockEndpointService
{
    Task<Result<List<MockEndpointDto>>> GetByProjectAsync(Guid projectId);
    Task<Result<MockEndpointDto>> GetByIdAsync(Guid endpointId);
    Task<Result<MockEndpointDto>> CreateAsync(CreateMockEndpointDto dto);
    Task<Result<MockEndpointDto>> CreateWithResponseAsync(
        CreateMockEndpointDto dto,
        int statusCode,
        string contentType,
        string? body,
        string? headers);
    Task<Result<MockEndpointDto>> UpdateAsync(Guid endpointId, UpdateMockEndpointDto dto);
    Task<Result<MockEndpointDto>> UpdateWithResponseAsync(
        Guid endpointId,
        UpdateMockEndpointDto dto,
        int statusCode,
        string contentType,
        string? body,
        string? headers);
    Task<Result> DeleteAsync(Guid endpointId);
    Task<Result> ReorderAsync(Guid projectId, List<Guid> endpointIds);
    Task<Result<MockEndpointDto>> DuplicateAsync(Guid endpointId);
}


