using Microsoft.EntityFrameworkCore;
using Mokit.Infrastructure.Data;

namespace Mokit.IntegrationTests;

/// <summary>
/// Provides a factory method for creating test DbContext instances.
/// Uses InMemory database for test isolation.
/// </summary>
public static class TestDbContextFactory
{
    /// <summary>
    /// Creates a new DbContext with InMemory database for testing.
    /// Each call creates a unique database instance.
    /// </summary>
    public static MokitDbContext Create()
    {
        var options = new DbContextOptionsBuilder<MokitDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;
        
        var context = new MokitDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
    
    /// <summary>
    /// Creates a DbContext that shares the same database with another context.
    /// Useful for verifying data persistence.
    /// </summary>
    public static MokitDbContext CreateWithSameDatabase(string databaseName)
    {
        var options = new DbContextOptionsBuilder<MokitDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        
        return new MokitDbContext(options);
    }
}
