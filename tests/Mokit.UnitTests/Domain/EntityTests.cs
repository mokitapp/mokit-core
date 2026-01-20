using FluentAssertions;
using Mokit.Domain.Entities;
using Mokit.Domain.Enums;
using Xunit;

namespace Mokit.UnitTests.Domain;

public class MockEndpointTests
{
    [Fact]
    public void MockEndpoint_DefaultValues_AreCorrect()
    {
        // Act
        var endpoint = new MockEndpoint();

        // Assert
        endpoint.Id.Should().NotBe(Guid.Empty); // Base entity generates new Guid
        endpoint.Name.Should().BeEmpty();
        endpoint.Route.Should().BeEmpty();
        endpoint.Method.Should().Be(HttpMethodType.GET);
        endpoint.IsActive.Should().BeTrue();
        endpoint.Order.Should().Be(0);
        endpoint.IsWildcard.Should().BeFalse();
        endpoint.RegexPattern.Should().BeNull();
        endpoint.ResponseMode.Should().Be(ResponseSelectionMode.Sequential);
        endpoint.CurrentResponseIndex.Should().Be(0);
        endpoint.DelayMin.Should().BeNull();
        endpoint.DelayMax.Should().BeNull();
        endpoint.ValidationErrorResponseTemplate.Should().BeNull();
    }

    [Fact]
    public void MockEndpoint_CanSetAllProperties()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var endpointId = Guid.NewGuid();

        // Act
        var endpoint = new MockEndpoint
        {
            Id = endpointId,
            ProjectId = projectId,
            Name = "Get Users",
            Description = "Returns list of users",
            Route = "/api/users",
            Method = HttpMethodType.POST,
            IsActive = false,
            Order = 5,
            IsWildcard = true,
            RegexPattern = "^/api/.*$",
            ResponseMode = ResponseSelectionMode.Random,
            CurrentResponseIndex = 2,
            DelayMin = 100,
            DelayMax = 500,
            ValidationErrorResponseTemplate = "{\"error\": \"{{validation.firstMessage}}\"}"
        };

        // Assert
        endpoint.Id.Should().Be(endpointId);
        endpoint.ProjectId.Should().Be(projectId);
        endpoint.Name.Should().Be("Get Users");
        endpoint.Description.Should().Be("Returns list of users");
        endpoint.Route.Should().Be("/api/users");
        endpoint.Method.Should().Be(HttpMethodType.POST);
        endpoint.IsActive.Should().BeFalse();
        endpoint.Order.Should().Be(5);
        endpoint.IsWildcard.Should().BeTrue();
        endpoint.RegexPattern.Should().Be("^/api/.*$");
        endpoint.ResponseMode.Should().Be(ResponseSelectionMode.Random);
        endpoint.CurrentResponseIndex.Should().Be(2);
        endpoint.DelayMin.Should().Be(100);
        endpoint.DelayMax.Should().Be(500);
        endpoint.ValidationErrorResponseTemplate.Should().Contain("validation.firstMessage");
    }

    [Fact]
    public void MockEndpoint_Collections_AreInitialized()
    {
        // Act
        var endpoint = new MockEndpoint();

        // Assert
        endpoint.Responses.Should().NotBeNull();
        endpoint.Responses.Should().BeEmpty();
        endpoint.ValidationRules.Should().NotBeNull();
        endpoint.ValidationRules.Should().BeEmpty();
        endpoint.Webhooks.Should().NotBeNull();
        endpoint.Webhooks.Should().BeEmpty();
    }

    [Theory]
    [InlineData(HttpMethodType.GET)]
    [InlineData(HttpMethodType.POST)]
    [InlineData(HttpMethodType.PUT)]
    [InlineData(HttpMethodType.DELETE)]
    [InlineData(HttpMethodType.PATCH)]
    [InlineData(HttpMethodType.HEAD)]
    [InlineData(HttpMethodType.OPTIONS)]
    public void MockEndpoint_SupportsAllHttpMethods(HttpMethodType method)
    {
        // Act
        var endpoint = new MockEndpoint { Method = method };

        // Assert
        endpoint.Method.Should().Be(method);
    }
}

public class MockProjectTests
{
    [Fact]
    public void MockProject_DefaultValues_AreCorrect()
    {
        // Act
        var project = new MockProject();

        // Assert
        project.Id.Should().NotBe(Guid.Empty); // Base entity generates new Guid
        project.Name.Should().BeEmpty();
        project.Slug.Should().BeEmpty();
        project.Description.Should().BeNull();
        project.IsActive.Should().BeTrue();
        project.Port.Should().Be(0);
        project.TeamId.Should().BeNull();
        project.UserId.Should().BeNull();
        project.DefaultDelay.Should().Be(0);
        project.EnableCors.Should().BeTrue();
        project.EnableLogging.Should().BeTrue();
        project.EnableLatencySimulation.Should().BeFalse();
        project.GlobalLatencyMin.Should().BeNull();
        project.GlobalLatencyMax.Should().BeNull();
        project.EnableJwtValidation.Should().BeFalse();
        project.JwtSecret.Should().BeNull();
        project.JwtIssuer.Should().BeNull();
        project.JwtAudience.Should().BeNull();
    }

    [Fact]
    public void MockProject_CanSetAllProperties()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var teamId = Guid.NewGuid();

        // Act
        var project = new MockProject
        {
            Id = projectId,
            Name = "My API Mock",
            Slug = "my-api-mock",
            Description = "Mock for testing",
            IsActive = false,
            Port = 5050,
            TeamId = teamId,
            UserId = "user123",
            DefaultDelay = 200,
            EnableCors = false,
            EnableLogging = false,
            EnableLatencySimulation = true,
            GlobalLatencyMin = 50,
            GlobalLatencyMax = 300,
            EnableJwtValidation = true,
            JwtSecret = "supersecret",
            JwtIssuer = "myissuer",
            JwtAudience = "myaudience"
        };

        // Assert
        project.Id.Should().Be(projectId);
        project.Name.Should().Be("My API Mock");
        project.Slug.Should().Be("my-api-mock");
        project.Description.Should().Be("Mock for testing");
        project.IsActive.Should().BeFalse();
        project.Port.Should().Be(5050);
        project.TeamId.Should().Be(teamId);
        project.UserId.Should().Be("user123");
        project.DefaultDelay.Should().Be(200);
        project.EnableCors.Should().BeFalse();
        project.EnableLogging.Should().BeFalse();
        project.EnableLatencySimulation.Should().BeTrue();
        project.GlobalLatencyMin.Should().Be(50);
        project.GlobalLatencyMax.Should().Be(300);
        project.EnableJwtValidation.Should().BeTrue();
        project.JwtSecret.Should().Be("supersecret");
        project.JwtIssuer.Should().Be("myissuer");
        project.JwtAudience.Should().Be("myaudience");
    }

    [Fact]
    public void MockProject_Collections_AreInitialized()
    {
        // Act
        var project = new MockProject();

        // Assert
        project.Endpoints.Should().NotBeNull();
        project.Endpoints.Should().BeEmpty();
        project.RequestLogs.Should().NotBeNull();
        project.RequestLogs.Should().BeEmpty();
        project.DynamicVariables.Should().NotBeNull();
        project.DynamicVariables.Should().BeEmpty();
    }

    [Fact]
    public void MockProject_CanHavePersonalOwnership()
    {
        // Act
        var project = new MockProject
        {
            UserId = "user-123",
            TeamId = null
        };

        // Assert
        project.UserId.Should().Be("user-123");
        project.TeamId.Should().BeNull();
    }

    [Fact]
    public void MockProject_CanHaveTeamOwnership()
    {
        // Arrange
        var teamId = Guid.NewGuid();

        // Act
        var project = new MockProject
        {
            TeamId = teamId,
            UserId = null
        };

        // Assert
        project.TeamId.Should().Be(teamId);
        project.UserId.Should().BeNull();
    }
}

public class WebhookDefinitionTests
{
    [Fact]
    public void WebhookDefinition_DefaultValues_AreCorrect()
    {
        // Act
        var webhook = new WebhookDefinition();

        // Assert
        webhook.Name.Should().BeEmpty();
        webhook.Url.Should().BeEmpty();
        webhook.Method.Should().Be(HttpMethodType.POST);
        webhook.Body.Should().BeNull();
        webhook.Headers.Should().BeNull();
        webhook.DelayMs.Should().Be(0);
        webhook.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void WebhookDefinition_CanSetAllProperties()
    {
        // Arrange
        var endpointId = Guid.NewGuid();

        // Act
        var webhook = new WebhookDefinition
        {
            EndpointId = endpointId,
            Name = "Notify Slack",
            Url = "https://hooks.slack.com/webhook",
            Method = HttpMethodType.POST,
            Body = "{\"text\": \"New request!\"}",
            Headers = "{\"Authorization\": \"Bearer token\"}",
            DelayMs = 1000,
            IsEnabled = false
        };

        // Assert
        webhook.EndpointId.Should().Be(endpointId);
        webhook.Name.Should().Be("Notify Slack");
        webhook.Url.Should().Be("https://hooks.slack.com/webhook");
        webhook.Method.Should().Be(HttpMethodType.POST);
        webhook.Body.Should().Contain("New request!");
        webhook.Headers.Should().Contain("Authorization");
        webhook.DelayMs.Should().Be(1000);
        webhook.IsEnabled.Should().BeFalse();
    }
}

public class ValidationRuleTests
{
    [Fact]
    public void ValidationRule_DefaultValues_AreCorrect()
    {
        // Act
        var rule = new ValidationRule();

        // Assert
        rule.ParameterName.Should().BeEmpty();
        rule.Location.Should().Be(ParameterLocation.Query);
        rule.DataType.Should().BeNull();
        rule.IsRequired.Should().BeFalse();
        rule.RegexPattern.Should().BeNull();
        rule.MinValue.Should().BeNull();
        rule.MaxValue.Should().BeNull();
        rule.AllowedValues.Should().BeNull();
        rule.ErrorMessage.Should().BeNull();
        rule.StatusCode.Should().Be(400);
        rule.IsActive.Should().BeTrue();
    }

    [Fact]
    public void ValidationRule_CanSetAllProperties()
    {
        // Act
        var rule = new ValidationRule
        {
            ParameterName = "email",
            Location = ParameterLocation.Body,
            DataType = "Email",
            IsRequired = true,
            RegexPattern = @"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$",
            MinValue = "5",
            MaxValue = "100",
            AllowedValues = null,
            ErrorMessage = "Invalid email format",
            StatusCode = 422,
            IsActive = false
        };

        // Assert
        rule.ParameterName.Should().Be("email");
        rule.Location.Should().Be(ParameterLocation.Body);
        rule.DataType.Should().Be("Email");
        rule.IsRequired.Should().BeTrue();
        rule.RegexPattern.Should().NotBeNull();
        rule.MinValue.Should().Be("5");
        rule.MaxValue.Should().Be("100");
        rule.ErrorMessage.Should().Be("Invalid email format");
        rule.StatusCode.Should().Be(422);
        rule.IsActive.Should().BeFalse();
    }

    [Theory]
    [InlineData(ParameterLocation.Query)]
    [InlineData(ParameterLocation.Header)]
    [InlineData(ParameterLocation.Path)]
    [InlineData(ParameterLocation.Body)]
    public void ValidationRule_SupportsAllLocations(ParameterLocation location)
    {
        // Act
        var rule = new ValidationRule { Location = location };

        // Assert
        rule.Location.Should().Be(location);
    }
}
