using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mokit.Application.DTOs.Endpoint;
using Mokit.Application.Interfaces.Persistence;
using Mokit.Domain.Entities;
using Mokit.Domain.Enums;
using Mokit.Infrastructure.Data;
using Mokit.Infrastructure.Persistence.UnitOfWork;
using Mokit.Infrastructure.Services;
using Moq;
using Xunit;

namespace Mokit.UnitTests.Services;

public class MockEndpointServiceTests
{
    private MokitDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MokitDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new MokitDbContext(options);
    }

    private IUnitOfWork<MokitDbContext> CreateUnitOfWork(MokitDbContext context)
    {
        var loggerFactory = new LoggerFactory();
        
        // Create a mock DbContextFactory that returns our context
        var mockFactory = new Mock<IDbContextFactory<MokitDbContext>>();
        mockFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => CreateContext());
        
        return new UnitOfWork(
            mockFactory.Object,
            loggerFactory,
            loggerFactory.CreateLogger<UnitOfWork>());
    }

    [Fact]
    public async Task UpdateWithResponseAsync_ShouldUpdateDelaySettings()
    {
        // Arrange - Create context with test data
        var context = CreateContext();
        
        var project = new MockProject 
        { 
            Id = Guid.NewGuid(), 
            Name = "Test Project", 
            Slug = "test-project",
            UserId = "user1" 
        };
        context.MockProjects.Add(project);

        var endpoint = new MockEndpoint
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Name = "Test Endpoint",
            Route = "/test",
            Method = HttpMethodType.GET,
            DelayMin = 0,
            DelayMax = 0
        };
        context.MockEndpoints.Add(endpoint);
        
        var response = new MockResponse
        {
            Id = Guid.NewGuid(),
            EndpointId = endpoint.Id,
            StatusCode = 200,
            IsDefault = true
        };
        context.MockResponses.Add(response);
        
        await context.SaveChangesAsync();

        // Create a factory that always returns a context with our test data
        var loggerFactory = new LoggerFactory();
        var mockFactory = new Mock<IDbContextFactory<MokitDbContext>>();
        mockFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => 
            {
                // Return a new context pointing to the same in-memory database
                var options = new DbContextOptionsBuilder<MokitDbContext>()
                    .UseInMemoryDatabase(databaseName: context.Database.GetDbConnection().Database)
                    .Options;
                return new MokitDbContext(options);
            });

        var unitOfWork = new UnitOfWork(
            mockFactory.Object,
            loggerFactory,
            loggerFactory.CreateLogger<UnitOfWork>());

        var service = new MockEndpointService(unitOfWork);

        // Act
        var updateDto = new UpdateMockEndpointDto
        {
            Name = "Updated Name",
            Route = "/test",
            Method = HttpMethodType.GET,
            DelayMin = 100,
            DelayMax = 500
        };

        var result = await service.UpdateWithResponseAsync(
            endpoint.Id, 
            updateDto, 
            200, 
            "application/json", 
            "{}", 
            null);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(100, result.Data?.DelayMin);
        Assert.Equal(500, result.Data?.DelayMax);

        // Verify matches DB using fresh context
        var dbEndpoint = await context.MockEndpoints.FindAsync(endpoint.Id);
        Assert.Equal(100, dbEndpoint?.DelayMin);
        Assert.Equal(500, dbEndpoint?.DelayMax);
    }
}
