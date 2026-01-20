using Mokit.Application.Common;
using Mokit.Application.DTOs.Response;

namespace Mokit.Application.Interfaces;

public interface IMockResponseService
{
    Task<Result<List<MockResponseDto>>> GetByEndpointAsync(Guid endpointId);
    Task<Result<MockResponseDto>> GetByIdAsync(Guid responseId);
    Task<Result<MockResponseDto>> CreateAsync(CreateMockResponseDto dto);
    Task<Result<MockResponseDto>> UpdateAsync(Guid responseId, UpdateMockResponseDto dto);
    Task<Result> DeleteAsync(Guid responseId);
    Task<Result> SetDefaultAsync(Guid responseId);
    Task<Result> ReorderAsync(Guid endpointId, List<Guid> responseIds);
}


