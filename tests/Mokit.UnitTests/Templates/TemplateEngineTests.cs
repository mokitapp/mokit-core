using FluentAssertions;
using Mokit.MockEngine.Templates;
using Xunit;

namespace Mokit.UnitTests.Templates;

public class TemplateEngineTests
{
    private readonly TemplateEngine _engine;
    private readonly MockRequestContext _defaultContext;

    public TemplateEngineTests()
    {
        _engine = new TemplateEngine();
        _defaultContext = new MockRequestContext
        {
            Path = "/api/users",
            Method = "GET",
            QueryParams = new Dictionary<string, string>
            {
                { "id", "123" },
                { "name", "John" }
            },
            Headers = new Dictionary<string, string>
            {
                { "Authorization", "Bearer token123" },
                { "Content-Type", "application/json" }
            },
            RouteParams = new Dictionary<string, string>
            {
                { "userId", "456" }
            }
        };
    }

    #region Basic Rendering Tests

    [Fact]
    public void Render_WithNullTemplate_ReturnsNull()
    {
        // Act
        var result = _engine.Render(null!, _defaultContext);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Render_WithEmptyTemplate_ReturnsEmpty()
    {
        // Act
        var result = _engine.Render("", _defaultContext);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Render_WithPlainText_ReturnsUnchanged()
    {
        // Arrange
        var template = "Hello, World!";

        // Act
        var result = _engine.Render(template, _defaultContext);

        // Assert
        result.Should().Be("Hello, World!");
    }

    [Fact]
    public void Render_WithPlainJson_ReturnsUnchanged()
    {
        // Arrange
        var template = """{"name": "John", "age": 30}""";

        // Act
        var result = _engine.Render(template, _defaultContext);

        // Assert
        result.Should().Be("""{"name": "John", "age": 30}""");
    }

    #endregion

    #region Request Context Tests

    [Fact]
    public void Render_WithRequestPath_ReturnsPath()
    {
        // Arrange
        var template = "Path: {{ request.path }}";

        // Act
        var result = _engine.Render(template, _defaultContext);

        // Assert
        result.Should().Be("Path: /api/users");
    }

    [Fact]
    public void Render_WithRequestMethod_ReturnsMethod()
    {
        // Arrange
        var template = "Method: {{ request.method }}";

        // Act
        var result = _engine.Render(template, _defaultContext);

        // Assert
        result.Should().Be("Method: GET");
    }

    [Fact]
    public void Render_WithQueryParam_ReturnsValue()
    {
        // Arrange
        var template = "ID: {{ request.query.id }}";

        // Act
        var result = _engine.Render(template, _defaultContext);

        // Assert
        result.Should().Be("ID: 123");
    }

    [Fact]
    public void Render_WithQuerystringAlias_ReturnsValue()
    {
        // Arrange
        var template = "ID: {{ request.querystring.id }}";

        // Act
        var result = _engine.Render(template, _defaultContext);

        // Assert
        result.Should().Be("ID: 123");
    }

    [Fact]
    public void Render_WithRouteParam_ReturnsValue()
    {
        // Arrange
        var template = "UserID: {{ request.route.userId }}";

        // Act
        var result = _engine.Render(template, _defaultContext);

        // Assert
        result.Should().Be("UserID: 456");
    }

    [Fact]
    public void Render_WithParamsAlias_ReturnsValue()
    {
        // Arrange
        var template = "UserID: {{ request.params.userId }}";

        // Act
        var result = _engine.Render(template, _defaultContext);

        // Assert
        result.Should().Be("UserID: 456");
    }

    [Fact]
    public void Render_WithHeader_ReturnsValue()
    {
        // Arrange
        var template = "Auth: {{ request.headers.Authorization }}";

        // Act
        var result = _engine.Render(template, _defaultContext);

        // Assert
        result.Should().Be("Auth: Bearer token123");
    }

    #endregion

    #region Built-in Variables Tests

    [Fact]
    public void Render_WithNow_ReturnsIsoDate()
    {
        // Arrange
        var template = "{{ now }}";

        // Act
        var result = _engine.Render(template, _defaultContext);

        // Assert
        result.Should().MatchRegex(@"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}");
    }

    [Fact]
    public void Render_WithToday_ReturnsDateOnly()
    {
        // Arrange
        var template = "{{ today }}";
        var expected = DateTime.UtcNow.ToString("yyyy-MM-dd");

        // Act
        var result = _engine.Render(template, _defaultContext);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Render_WithGuid_ReturnsValidGuid()
    {
        // Arrange
        var template = "{{ guid }}";

        // Act
        var result = _engine.Render(template, _defaultContext);

        // Assert
        Guid.TryParse(result, out _).Should().BeTrue();
    }

    [Fact]
    public void Render_WithUuid_ReturnsValidGuid()
    {
        // Arrange
        var template = "{{ uuid }}";

        // Act
        var result = _engine.Render(template, _defaultContext);

        // Assert
        Guid.TryParse(result, out _).Should().BeTrue();
    }

    #endregion

    #region Faker Tests

    [Fact]
    public void Render_WithFakerName_ReturnsNonEmptyString()
    {
        // Arrange
        var template = "{{ faker.name.full_name }}";

        // Act
        var result = _engine.Render(template, _defaultContext);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain(" "); // Full name has space
    }

    [Fact]
    public void Render_WithFakerEmail_ReturnsEmailFormat()
    {
        // Arrange 
        var template = "{{ faker.internet.email }}";

        // Act
        var result = _engine.Render(template, _defaultContext);

        // Assert
        result.Should().Contain("@");
        result.Should().Contain(".");
    }

    [Fact]
    public void Render_WithFakerProductName_ReturnsNonEmpty()
    {
        // Arrange
        var template = "{{ faker.commerce.product_name }}";

        // Act
        var result = _engine.Render(template, _defaultContext);

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Render_WithFakerCity_ReturnsNonEmpty()
    {
        // Arrange
        var template = "{{ faker.address.city }}";

        // Act
        var result = _engine.Render(template, _defaultContext);

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Render_WithFakerRandomNumber_ReturnsNumber()
    {
        // Arrange
        var template = "{{ faker.random.number }}";

        // Act
        var result = _engine.Render(template, _defaultContext);

        // Assert
        int.TryParse(result, out var number).Should().BeTrue();
        number.Should().BeInRange(0, 10000);
    }

    [Fact]
    public void Render_WithFakerRandomBoolean_ReturnsBooleanString()
    {
        // Arrange
        var template = "{{ faker.random.boolean }}";

        // Act
        var result = _engine.Render(template, _defaultContext);

        // Assert
        result.Should().BeOneOf("true", "false");
    }

    #endregion

    #region Scriban Control Flow Tests

    [Fact]
    public void Render_WithForLoop_RendersMultipleItems()
    {
        // Arrange
        var template = "{{ for i in 1..3 }}{{ i }}{{ end }}";

        // Act
        var result = _engine.Render(template, _defaultContext);

        // Assert
        result.Should().Be("123");
    }

    [Fact]
    public void Render_WithForLoopAndIndex_RendersCorrectly()
    {
        // Arrange
        var template = "{{ for i in 1..3 }}[{{ for.index }}]{{ end }}";

        // Act
        var result = _engine.Render(template, _defaultContext);

        // Assert
        result.Should().Be("[0][1][2]");
    }

    [Fact]
    public void Render_WithIfCondition_RendersCorrectBranch()
    {
        // Arrange
        var templateTrue = "{{ if true }}yes{{ else }}no{{ end }}";
        var templateFalse = "{{ if false }}yes{{ else }}no{{ end }}";

        // Act
        var resultTrue = _engine.Render(templateTrue, _defaultContext);
        var resultFalse = _engine.Render(templateFalse, _defaultContext);

        // Assert
        resultTrue.Should().Be("yes");
        resultFalse.Should().Be("no");
    }

    [Fact]
    public void Render_WithQueryParamInCondition_EvaluatesCorrectly()
    {
        // Arrange
        var template = """{{ if request.query.id }}has_id{{ else }}no_id{{ end }}""";

        // Act
        var result = _engine.Render(template, _defaultContext);

        // Assert
        result.Should().Be("has_id");
    }

    #endregion

    #region Complex Template Tests

    [Fact]
    public void Render_WithJsonArrayLoop_ProducesValidStructure()
    {
        // Arrange
        var template = """
[
{{ for i in 1..2 }}
  { "index": {{ for.index }} }{{ if !for.last }},{{ end }}
{{ end }}
]
""";

        // Act
        var result = _engine.Render(template, _defaultContext);

        // Assert
        result.Should().Contain("\"index\": 0");
        result.Should().Contain("\"index\": 1");
    }

    [Fact]
    public void Render_WithMixedContent_RendersAll()
    {
        // Arrange
        var template = """
{
  "path": "{{ request.path }}",
  "method": "{{ request.method }}",
  "queryId": "{{ request.query.id }}"
}
""";

        // Act
        var result = _engine.Render(template, _defaultContext);

        // Assert
        result.Should().Contain("\"path\": \"/api/users\"");
        result.Should().Contain("\"method\": \"GET\"");
        result.Should().Contain("\"queryId\": \"123\"");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void Render_WithInvalidSyntax_ReturnsOriginalTemplate()
    {
        // Arrange - unclosed block
        var template = "{{ if true }}yes";

        // Act
        var result = _engine.Render(template, _defaultContext);

        // Assert - should return original on parse error
        result.Should().Be(template);
    }

    [Fact]
    public void Render_WithMissingVariable_HandlesGracefully()
    {
        // Arrange
        var template = "{{ request.query.nonexistent }}";

        // Act
        var result = _engine.Render(template, _defaultContext);

        // Assert - Scriban returns empty for missing values
        result.Should().BeEmpty();
    }

    #endregion
}
