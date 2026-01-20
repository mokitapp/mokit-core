using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Mokit.Domain.Entities;
using Mokit.Domain.Enums;
using Mokit.Infrastructure.Data;
using Xunit;

namespace Mokit.IntegrationTests.Database;

/// <summary>
/// Integration tests for database operations using InMemory database.
/// Tests the full EF Core pipeline: entities, configurations, and DbContext.
/// </summary>
public class DatabaseIntegrationTests
{
    #region MockProject Tests

    [Fact]
    public async Task CreateProject_ShouldPersistAllProperties()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var project = new MockProject
        {
            Name = "Test API",
            Slug = "test-api",
            Description = "Test project for integration testing",
            IsActive = true,
            Port = 5050,
            UserId = "test-user-123",
            EnableCors = true,
            EnableLogging = true,
            DefaultDelay = 100
        };

        // Act
        context.MockProjects.Add(project);
        await context.SaveChangesAsync();

        // Assert
        var saved = await context.MockProjects.FindAsync(project.Id);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Test API");
        saved.Slug.Should().Be("test-api");
        saved.Port.Should().Be(5050);
        saved.UserId.Should().Be("test-user-123");
        saved.EnableCors.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateProject_ShouldPersistChanges()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var project = new MockProject
        {
            Name = "Original Name",
            Slug = "original",
            UserId = "user1"
        };
        context.MockProjects.Add(project);
        await context.SaveChangesAsync();

        // Act
        project.Name = "Updated Name";
        project.EnableCors = false;
        await context.SaveChangesAsync();

        // Assert - verify in same context (InMemory doesn't need fresh context)
        var updated = await context.MockProjects.FindAsync(project.Id);
        updated!.Name.Should().Be("Updated Name");
        updated.EnableCors.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteProject_ShouldRemoveFromDatabase()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var project = new MockProject
        {
            Name = "To Delete",
            Slug = "to-delete",
            UserId = "user1"
        };
        context.MockProjects.Add(project);
        await context.SaveChangesAsync();
        var projectId = project.Id;

        // Act
        context.MockProjects.Remove(project);
        await context.SaveChangesAsync();

        // Assert
        var deleted = await context.MockProjects.FindAsync(projectId);
        deleted.Should().BeNull();
    }

    #endregion

    #region MockEndpoint Tests

    [Fact]
    public async Task CreateEndpoint_WithProject_ShouldEstablishRelationship()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var project = new MockProject
        {
            Name = "API Project",
            Slug = "api-project",
            UserId = "user1"
        };
        context.MockProjects.Add(project);
        await context.SaveChangesAsync();

        var endpoint = new MockEndpoint
        {
            ProjectId = project.Id,
            Name = "Get Users",
            Route = "/api/users",
            Method = HttpMethodType.GET,
            IsActive = true
        };

        // Act
        context.MockEndpoints.Add(endpoint);
        await context.SaveChangesAsync();

        // Assert
        var savedEndpoint = await context.MockEndpoints
            .Include(e => e.Project)
            .FirstOrDefaultAsync(e => e.Id == endpoint.Id);
        
        savedEndpoint.Should().NotBeNull();
        savedEndpoint!.Project.Should().NotBeNull();
        savedEndpoint.Project.Id.Should().Be(project.Id);
        savedEndpoint.Project.Name.Should().Be("API Project");
    }

    [Fact]
    public async Task Endpoint_WithDelaySettings_ShouldPersist()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var project = new MockProject { Name = "Test", Slug = "test", UserId = "u1" };
        context.MockProjects.Add(project);
        await context.SaveChangesAsync();

        var endpoint = new MockEndpoint
        {
            ProjectId = project.Id,
            Name = "Slow Endpoint",
            Route = "/slow",
            Method = HttpMethodType.GET,
            DelayMin = 500,
            DelayMax = 2000
        };

        // Act
        context.MockEndpoints.Add(endpoint);
        await context.SaveChangesAsync();

        // Assert
        var saved = await context.MockEndpoints.FindAsync(endpoint.Id);
        saved!.DelayMin.Should().Be(500);
        saved.DelayMax.Should().Be(2000);
    }

    [Theory]
    [InlineData(HttpMethodType.GET)]
    [InlineData(HttpMethodType.POST)]
    [InlineData(HttpMethodType.PUT)]
    [InlineData(HttpMethodType.DELETE)]
    [InlineData(HttpMethodType.PATCH)]
    public async Task Endpoint_AllHttpMethods_ShouldPersist(HttpMethodType method)
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var project = new MockProject { Name = "Test", Slug = "test", UserId = "u1" };
        context.MockProjects.Add(project);
        await context.SaveChangesAsync();

        var endpoint = new MockEndpoint
        {
            ProjectId = project.Id,
            Name = $"Test {method}",
            Route = $"/{method.ToString().ToLower()}",
            Method = method
        };

        // Act
        context.MockEndpoints.Add(endpoint);
        await context.SaveChangesAsync();

        // Assert
        var saved = await context.MockEndpoints.FindAsync(endpoint.Id);
        saved!.Method.Should().Be(method);
    }

    #endregion

    #region MockResponse Tests

    [Fact]
    public async Task CreateResponse_WithEndpoint_ShouldPersist()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var project = new MockProject { Name = "Test", Slug = "test", UserId = "u1" };
        context.MockProjects.Add(project);
        
        var endpoint = new MockEndpoint
        {
            ProjectId = project.Id,
            Name = "Get User",
            Route = "/user",
            Method = HttpMethodType.GET
        };
        context.MockEndpoints.Add(endpoint);
        await context.SaveChangesAsync();

        var response = new MockResponse
        {
            EndpointId = endpoint.Id,
            StatusCode = 200,
            ContentType = "application/json",
            Body = """{"id": 1, "name": "John"}""",
            IsDefault = true
        };

        // Act
        context.MockResponses.Add(response);
        await context.SaveChangesAsync();

        // Assert
        var saved = await context.MockResponses
            .Include(r => r.Endpoint)
            .FirstOrDefaultAsync(r => r.Id == response.Id);
        
        saved.Should().NotBeNull();
        saved!.StatusCode.Should().Be(200);
        saved.ContentType.Should().Be("application/json");
        saved.Body.Should().Contain("John");
        saved.Endpoint.Name.Should().Be("Get User");
    }

    [Fact]
    public async Task Endpoint_WithMultipleResponses_ShouldLoadAll()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var project = new MockProject { Name = "Test", Slug = "test", UserId = "u1" };
        context.MockProjects.Add(project);
        
        var endpoint = new MockEndpoint
        {
            ProjectId = project.Id,
            Name = "Multi Response",
            Route = "/multi",
            Method = HttpMethodType.GET
        };
        context.MockEndpoints.Add(endpoint);
        await context.SaveChangesAsync();

        context.MockResponses.AddRange(
            new MockResponse { EndpointId = endpoint.Id, StatusCode = 200, IsDefault = true },
            new MockResponse { EndpointId = endpoint.Id, StatusCode = 404, IsDefault = false },
            new MockResponse { EndpointId = endpoint.Id, StatusCode = 500, IsDefault = false }
        );
        await context.SaveChangesAsync();

        // Act
        var loaded = await context.MockEndpoints
            .Include(e => e.Responses)
            .FirstOrDefaultAsync(e => e.Id == endpoint.Id);

        // Assert
        loaded!.Responses.Should().HaveCount(3);
        loaded.Responses.Select(r => r.StatusCode).Should().Contain(new[] { 200, 404, 500 });
    }

    #endregion

    #region Webhook Tests

    [Fact]
    public async Task CreateWebhook_WithEndpoint_ShouldPersist()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var project = new MockProject { Name = "Test", Slug = "test", UserId = "u1" };
        context.MockProjects.Add(project);
        
        var endpoint = new MockEndpoint
        {
            ProjectId = project.Id,
            Name = "With Webhook",
            Route = "/hook",
            Method = HttpMethodType.POST
        };
        context.MockEndpoints.Add(endpoint);
        await context.SaveChangesAsync();

        var webhook = new WebhookDefinition
        {
            EndpointId = endpoint.Id,
            Name = "Slack Notification",
            Url = "https://hooks.slack.com/services/xxx",
            Method = HttpMethodType.POST,
            Body = """{"text": "New request received"}""",
            Headers = """{"Content-Type": "application/json"}""",
            DelayMs = 1000,
            IsEnabled = true
        };

        // Act
        context.Set<WebhookDefinition>().Add(webhook);
        await context.SaveChangesAsync();

        // Assert
        var saved = await context.Set<WebhookDefinition>()
            .Include(w => w.Endpoint)
            .FirstOrDefaultAsync(w => w.Id == webhook.Id);
        
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Slack Notification");
        saved.Url.Should().Contain("slack.com");
        saved.DelayMs.Should().Be(1000);
        saved.Endpoint.Name.Should().Be("With Webhook");
    }

    [Fact]
    public async Task Endpoint_WithMultipleWebhooks_ShouldLoadAll()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var project = new MockProject { Name = "Test", Slug = "test", UserId = "u1" };
        context.MockProjects.Add(project);
        
        var endpoint = new MockEndpoint
        {
            ProjectId = project.Id,
            Name = "Multi Webhook",
            Route = "/webhooks",
            Method = HttpMethodType.POST
        };
        context.MockEndpoints.Add(endpoint);
        await context.SaveChangesAsync();

        context.Set<WebhookDefinition>().AddRange(
            new WebhookDefinition { EndpointId = endpoint.Id, Name = "Hook 1", Url = "https://a.com", Method = HttpMethodType.POST },
            new WebhookDefinition { EndpointId = endpoint.Id, Name = "Hook 2", Url = "https://b.com", Method = HttpMethodType.POST }
        );
        await context.SaveChangesAsync();

        // Act
        var loaded = await context.MockEndpoints
            .Include(e => e.Webhooks)
            .FirstOrDefaultAsync(e => e.Id == endpoint.Id);

        // Assert
        loaded!.Webhooks.Should().HaveCount(2);
        loaded.Webhooks.Select(w => w.Name).Should().Contain(new[] { "Hook 1", "Hook 2" });
    }

    #endregion

    #region ValidationRule Tests

    [Fact]
    public async Task CreateValidationRule_ShouldPersist()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var project = new MockProject { Name = "Test", Slug = "test", UserId = "u1" };
        context.MockProjects.Add(project);
        
        var endpoint = new MockEndpoint
        {
            ProjectId = project.Id,
            Name = "Validated",
            Route = "/validate",
            Method = HttpMethodType.POST
        };
        context.MockEndpoints.Add(endpoint);
        await context.SaveChangesAsync();

        var rule = new ValidationRule
        {
            EndpointId = endpoint.Id,
            ParameterName = "email",
            Location = ParameterLocation.Body,
            DataType = "Email",
            IsRequired = true,
            RegexPattern = @"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$",
            ErrorMessage = "Invalid email format",
            StatusCode = 422
        };

        // Act
        context.Set<ValidationRule>().Add(rule);
        await context.SaveChangesAsync();

        // Assert
        var saved = await context.Set<ValidationRule>().FindAsync(rule.Id);
        saved.Should().NotBeNull();
        saved!.ParameterName.Should().Be("email");
        saved.Location.Should().Be(ParameterLocation.Body);
        saved.IsRequired.Should().BeTrue();
        saved.StatusCode.Should().Be(422);
    }

    #endregion

    #region Cascade Delete Tests

    [Fact]
    public async Task DeleteProject_ShouldCascadeDeleteEndpoints()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var project = new MockProject { Name = "Cascade Test", Slug = "cascade", UserId = "u1" };
        context.MockProjects.Add(project);
        
        var endpoint1 = new MockEndpoint { ProjectId = project.Id, Name = "E1", Route = "/e1", Method = HttpMethodType.GET };
        var endpoint2 = new MockEndpoint { ProjectId = project.Id, Name = "E2", Route = "/e2", Method = HttpMethodType.POST };
        context.MockEndpoints.AddRange(endpoint1, endpoint2);
        await context.SaveChangesAsync();

        var endpointIds = new[] { endpoint1.Id, endpoint2.Id };

        // Act
        context.MockProjects.Remove(project);
        await context.SaveChangesAsync();

        // Assert
        var remainingEndpoints = await context.MockEndpoints
            .Where(e => endpointIds.Contains(e.Id))
            .ToListAsync();
        
        remainingEndpoints.Should().BeEmpty();
    }

    #endregion
}
