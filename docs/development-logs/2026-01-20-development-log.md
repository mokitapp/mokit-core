# Development Log - 2026-01-20

## Summary
Critical fix for NpgsqlRetryingExecutionStrategy transaction handling and route parameter matching improvements.

---

## 🚨 Critical Bug Fix: NpgsqlRetryingExecutionStrategy

### Problem
When using `NpgsqlRetryingExecutionStrategy` with PostgreSQL, all database operations that involved transactions were failing with:

```
System.InvalidOperationException: The configured execution strategy 'NpgsqlRetryingExecutionStrategy' 
does not support user-initiated transactions. Use the execution strategy returned by 
'DbContext.Database.CreateExecutionStrategy()' to execute all the operations in the transaction 
as a retriable unit.
```

### Root Cause
The `CreateTransactionalScopeAsync()` method was wrapping only `BeginTransactionAsync()` inside the execution strategy, but the actual database operations and `CommitAsync()` were happening **outside** the strategy block. This violates `NpgsqlRetryingExecutionStrategy`'s requirement that the entire transaction lifecycle must be wrapped in a single `ExecuteAsync()` call.

### Solution
Created a new `ExecuteTransactionAsync<TResult>()` method that wraps the entire transaction (begin, execute, commit/rollback) in a single execution strategy block:

```csharp
// NEW: Correct pattern
public async Task<TResult> ExecuteTransactionAsync<TResult>(
    Func<IUnitOfWorkScope<MokitDbContext>, Task<TResult>> operation,
    CancellationToken cancellationToken = default)
{
    var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
    var strategy = context.Database.CreateExecutionStrategy();

    return await strategy.ExecuteAsync(async () =>
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await using var scope = new UnitOfWorkScope(context, null, scopeLogger);
        
        try
        {
            var result = await operation(scope);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    });
}
```

---

## 🔧 Changes Made

### Unit of Work Pattern Updates

| File | Change |
|------|--------|
| `IUnitOfWork.cs` | Added `ExecuteTransactionAsync<TResult>()` and `ExecuteTransactionAsync()` methods |
| `UnitOfWork.cs` | Implemented new execution strategy-safe transaction methods |
| `CreateTransactionalScopeAsync()` | Marked as `[Obsolete]` with guidance to use new pattern |

### Service Layer Migration (30 Methods Updated)

All services migrated from `CreateTransactionalScopeAsync` to `ExecuteTransactionAsync`:

| Service | Methods Updated |
|---------|----------------|
| `MockProjectService` | `CreateAsync`, `UpdateAsync`, `DeleteAsync` |
| `MockEndpointService` | `CreateWithResponseAsync`, `UpdateAsync`, `UpdateWithResponseAsync`, `DeleteAsync`, `ReorderAsync`, `DuplicateAsync` |
| `MockDataProvider` | `LogRequestAsync` |
| `RequestLogService` | `DeleteLogAsync`, `DeleteProjectLogsAsync`, `DeleteAllLogsAsync` |
| `MockResponseService` | `CreateAsync`, `UpdateAsync`, `DeleteAsync`, `SetDefaultAsync`, `ReorderAsync` |
| `TeamService` | `CreateAsync`, `UpdateAsync`, `DeleteAsync`, `AddMemberAsync`, `RemoveMemberAsync`, `UpdateMemberRoleAsync` |
| `UserService` | `UpdateUserAsync`, `DeactivateUserAsync`, `ReactivateUserAsync`, `AddUserToTeamAsync`, `RemoveUserFromTeamAsync` |
| `ImportService` | `ImportToProjectAsync` |

---

## 🛣️ Route Matching Enhancement

### Problem
Route parameters defined with Express.js/Postman style (`:id`) were not matching incoming requests:

```
Route Definition: /api/users/:id
Request: /api/users/1
Result: ❌ "Endpoint not found"
```

### Solution
Updated `RouteMatcher.cs` to support both parameter styles:

```csharp
// ASP.NET style: {id}
if (patternSegment.StartsWith('{') && patternSegment.EndsWith('}'))
{
    var paramName = patternSegment.Trim('{', '}');
    parameters[paramName] = pathSegment;
}
// Express.js/Postman style: :id
else if (patternSegment.StartsWith(':'))
{
    var paramName = patternSegment[1..]; // Remove leading ':'
    parameters[paramName] = pathSegment;
}
```

---

## Files Modified

### Core Infrastructure
- `src/Mokit.Application/Interfaces/Persistence/IUnitOfWork.cs` - Added ExecuteTransactionAsync methods
- `src/Mokit.Infrastructure/Persistence/UnitOfWork/UnitOfWork.cs` - Implemented new pattern

### Services (All Transactional Methods)
- `src/Mokit.Infrastructure/Services/MockProjectService.cs`
- `src/Mokit.Infrastructure/Services/MockEndpointService.cs`
- `src/Mokit.Infrastructure/Services/MockDataProvider.cs`
- `src/Mokit.Infrastructure/Services/RequestLogService.cs`
- `src/Mokit.Infrastructure/Services/MockResponseService.cs`
- `src/Mokit.Infrastructure/Services/TeamService.cs`
- `src/Mokit.Infrastructure/Services/UserService.cs`
- `src/Mokit.Infrastructure/Services/ImportService.cs`

### Mock Engine
- `src/Mokit.MockEngine/Routing/RouteMatcher.cs` - Added `:param` style support

---

## 🧪 Test Results

Build Status: ✅ **Build succeeded. 0 Error(s)**

All existing tests continue to pass.

---

## 📝 Migration Guide

### For Developers: Updating Custom Code

**Before (Old Pattern - Now Deprecated):**
```csharp
await using var scope = await _unitOfWork.CreateTransactionalScopeAsync();
// ... database operations ...
await scope.CommitAsync();
```

**After (New Pattern - Required for NpgsqlRetryingExecutionStrategy):**
```csharp
var result = await _unitOfWork.ExecuteTransactionAsync(async scope =>
{
    // ... database operations ...
    return someResult;
});
```

### Key Differences:
1. Transaction is managed automatically (no manual `CommitAsync()` needed)
2. Return values come through the lambda
3. For failure cases, return a tuple like `(false, "Error message")`
4. Rollback happens automatically on exceptions

---

## 🔜 Future Considerations

1. **Remove `CreateTransactionalScopeAsync`** - Once all consumers are migrated, remove the obsolete method entirely
2. **Consider "Normalize on Save"** - Convert `:id` to `{id}` format when saving endpoints for cleaner matching logic
3. **Pre-compiled Route Patterns** - For high-traffic scenarios, cache compiled regex patterns
