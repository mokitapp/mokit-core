using Mokit.Application.Common;
using Mokit.Domain.Enums;

namespace Mokit.Application.Interfaces;

public interface IImportService
{
    Task<Result<ParsedCollectionData>> ParsePostmanCollectionAsync(string content);
    Task<Result<ParsedCollectionData>> ParseOpenApiSpecAsync(string content, bool isYaml);
    Task<Result<ImportResult>> ImportToProjectAsync(string userId, Guid projectId, ParsedCollectionData data, ImportOptions options);
}

public class ParsedCollectionData
{
    public string CollectionName { get; set; } = "";
    public string? Description { get; set; }
    public List<ParsedEndpoint> Endpoints { get; set; } = new();
    public List<ParsedVariable> Variables { get; set; } = new();
}

public class ParsedEndpoint
{
    public string Name { get; set; } = "";
    public HttpMethodType Method { get; set; }
    public string Route { get; set; } = "";
    public string? Description { get; set; }
    public List<ParsedHeader> Headers { get; set; } = new();
    public ParsedBody? Body { get; set; }
    public List<ParsedResponse> Responses { get; set; } = new();
}

public class ParsedHeader
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
}

public class ParsedBody
{
    public string Mode { get; set; } = "";
    public string? Raw { get; set; }
    public List<ParsedFormData>? FormData { get; set; }
    public List<ParsedUrlEncoded>? UrlEncoded { get; set; }
}

public class ParsedFormData
{
    public string Key { get; set; } = "";
    public string? Value { get; set; }
    public string? Src { get; set; }
    public string Type { get; set; } = "";
}

public class ParsedUrlEncoded
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
}

public class ParsedResponse
{
    public string Name { get; set; } = "";
    public int StatusCode { get; set; }
    public string StatusText { get; set; } = "";
    public List<ParsedHeader> Headers { get; set; } = new();
    public string? Body { get; set; }
}

public class ParsedVariable
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
    public string? Type { get; set; }
}

public class ImportOptions
{
    public bool SkipDuplicates { get; set; } = true;
    public bool CreateExamples { get; set; } = true;
    public bool ImportHeaders { get; set; } = true;
}

public class ImportResult
{
    public int EndpointsCreated { get; set; }
    public int EndpointsSkipped { get; set; }
    public int ResponsesCreated { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}
