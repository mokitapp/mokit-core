using Microsoft.AspNetCore.Components;
using Mokit.Application.DTOs;
using Mokit.Application.DTOs.Endpoint;
using Mokit.Application.Interfaces;
using Mokit.Domain.Enums;
using Microsoft.JSInterop;
using Mokit.Web.Services;

namespace Mokit.Web.Components.Shared;

public partial class EndpointModal
{
    [Inject] public IMockEndpointService EndpointService { get; set; } = default!;
    [Inject] public IRequestLogService LogService { get; set; } = default!;
    [Inject] public IToastService ToastService { get; set; } = default!;

    [Parameter] public Guid ProjectId { get; set; }
    [Parameter] public MockEndpointDto? Endpoint { get; set; }
    [Parameter] public bool EnableCors { get; set; } = true;
    [Parameter] public EventCallback<MockEndpointDto> OnSaved { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    private string activeTab = "general";
    private CreateMockEndpointDto endpointModel = new();
    private ResponseModel responseModel = new();
    private List<HeaderModel> responseHeaders = new();
    private List<ValidationRuleModel> validationRules = new();
    private List<WebhookModel> webhooks = new();
    private List<RequestLogDetailDto> endpointLogs = new();
    private bool loadingLogs = false;
    private MonacoEditor? monacoEditor;
    private MonacoEditor? errorTemplateEditor;
    private Dictionary<int, MonacoEditor?> webhookEditors = new();
    
    // Validation error response configuration
    private string? validationErrorResponseTemplate;

    protected override void OnInitialized()
    {
        if (Endpoint != null)
        {
            // Edit mode
            endpointModel = new CreateMockEndpointDto
            {
                ProjectId = ProjectId,
                Name = Endpoint.Name,
                Description = Endpoint.Description,
                Route = Endpoint.Route,
                Method = Endpoint.Method
            };
            
            var firstResponse = Endpoint.Responses?.FirstOrDefault();
            responseModel = new ResponseModel
            {
                StatusCode = firstResponse?.StatusCode ?? 200,
                ContentType = firstResponse?.ContentType ?? "application/json",
                Body = firstResponse?.Body ?? "{}",
                DelayMin = Endpoint.DelayMin,
                DelayMax = Endpoint.DelayMax
            };
            
            // Parse headers
            responseHeaders = new List<HeaderModel>();
            if (!string.IsNullOrEmpty(firstResponse?.Headers))
            {
                try
                {
                    var headers = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(firstResponse.Headers);
                    if (headers != null)
                    {
                        foreach (var h in headers)
                        {
                            responseHeaders.Add(new HeaderModel { Key = h.Key, Value = h.Value });
                        }
                    }
                }
                catch { }
            }
            
            // Populate validation rules
            validationRules = Endpoint.ValidationRules?.Select(r => new ValidationRuleModel
            {
                ParameterName = r.ParameterName,
                Location = r.Location,
                DataType = r.DataType,
                IsRequired = r.IsRequired,
                RegexPattern = r.RegexPattern,
                MinValue = r.MinValue,
                MaxValue = r.MaxValue,
                AllowedValues = r.AllowedValues,
                ErrorMessage = r.ErrorMessage,
                StatusCode = r.StatusCode > 0 ? r.StatusCode : 400
            }).ToList() ?? new List<ValidationRuleModel>();
            
            // Populate Webhooks
            webhooks = Endpoint.Webhooks?.Select(w => new WebhookModel
            {
                Name = w.Name,
                Url = w.Url,
                Method = w.Method,
                Body = w.Body,
                Headers = w.Headers,
                DelayMs = w.DelayMs,
                IsEnabled = w.IsEnabled
            }).ToList() ?? new List<WebhookModel>();

            // Load validation error response configuration
            validationErrorResponseTemplate = Endpoint.ValidationErrorResponseTemplate;
        }
        else
        {
            // New mode
            endpointModel = new CreateMockEndpointDto
            {
                ProjectId = ProjectId,
                Method = HttpMethodType.GET
            };
            responseModel = new ResponseModel
            {
                StatusCode = 200,
                ContentType = "application/json",
                Body = "{\n  \"message\": \"Hello from Mokit!\",\n  \"id\": \"{{faker.random.uuid}}\"\n}"
            };
            validationRules = new List<ValidationRuleModel>();
            webhooks = new List<WebhookModel>();
            validationErrorResponseTemplate = null;
        }
    }

    private void SetTabGeneral() => activeTab = "general";
    private void SetTabResponse() => activeTab = "response";
    private void SetTabHeaders() => activeTab = "headers";
    private void SetTabRules() => activeTab = "rules";
    private void SetTabWebhooks() => activeTab = "webhooks";
    private async Task SetTabLogs()
    {
        activeTab = "logs";
        if (Endpoint != null && endpointLogs.Count == 0)
        {
            await LoadEndpointLogs();
        }
    }

    private async Task SaveAsync()
    {
        // Convert headers to JSON
        var headersDict = responseHeaders
            .Where(h => !string.IsNullOrEmpty(h.Key))
            .ToDictionary(h => h.Key, h => h.Value ?? "");
        var headersJson = headersDict.Count > 0 
            ? System.Text.Json.JsonSerializer.Serialize(headersDict) 
            : null;

        // Convert validation rules
        var rulesDto = validationRules.Select(r => new ValidationRuleDto
        {
            ParameterName = r.ParameterName,
            Location = r.Location,
            DataType = r.DataType,
            IsRequired = r.IsRequired,
            RegexPattern = r.RegexPattern,
            MinValue = r.MinValue,
            MaxValue = r.MaxValue,
            AllowedValues = r.AllowedValues,
            ErrorMessage = r.ErrorMessage,
            StatusCode = r.StatusCode
        }).ToList();

        // Convert webhooks
        var webhooksDto = webhooks.Select(w => new WebhookDefinitionDto
        {
            Name = w.Name,
            Url = w.Url,
            Method = w.Method,
            Body = w.Body,
            Headers = w.Headers,
            DelayMs = w.DelayMs,
            IsEnabled = w.IsEnabled
        }).ToList();

        if (Endpoint == null)
        {
            // Create new endpoint
            endpointModel.DelayMin = responseModel.DelayMin;
            endpointModel.DelayMax = responseModel.DelayMax;
            
            // Ensure ProjectId is set
            endpointModel.ProjectId = ProjectId;
            endpointModel.ValidationRules = rulesDto;
            endpointModel.Webhooks = webhooksDto;

            var result = await EndpointService.CreateWithResponseAsync(
                endpointModel,
                responseModel.StatusCode,
                responseModel.ContentType,
                responseModel.Body,
                headersJson);
                
            if (result.IsSuccess)
            {
                await OnSaved.InvokeAsync(result.Data!);
            }
        }
        else
        {
            // Update existing endpoint
            var updateDto = new UpdateMockEndpointDto
            {
                Name = endpointModel.Name,
                Description = endpointModel.Description,
                Route = endpointModel.Route,
                Method = endpointModel.Method,
                IsActive = true,
                Order = Endpoint.Order,
                DelayMin = responseModel.DelayMin,
                DelayMax = responseModel.DelayMax,
                ValidationRules = rulesDto,
                Webhooks = webhooksDto,
                ValidationErrorResponseTemplate = validationErrorResponseTemplate
            };
            
            var result = await EndpointService.UpdateWithResponseAsync(
                Endpoint.Id,
                updateDto,
                responseModel.StatusCode,
                responseModel.ContentType,
                responseModel.Body,
                headersJson);
                
            if (result.IsSuccess)
            {
                await OnSaved.InvokeAsync(result.Data!);
            }
            else
            {
                ToastService.ShowError(result.Error ?? "Failed to save endpoint");
            }
        }
    }

    private void AddHeader()
    {
        responseHeaders.Add(new HeaderModel());
    }

    private void RemoveHeader(HeaderModel header)
    {
        responseHeaders.Remove(header);
    }

    private void AddValidationRule()
    {
        validationRules.Add(new ValidationRuleModel { Location = "Query", DataType = "String" });
    }

    private void RemoveValidationRule(ValidationRuleModel rule)
    {
        validationRules.Remove(rule);
    }

    private void AddWebhook()
    {
        webhooks.Add(new WebhookModel 
        { 
            Name = "New Webhook",
            Method = HttpMethodType.POST,
            IsEnabled = true 
        });
    }

    private void RemoveWebhook(WebhookModel hook)
    {
        webhooks.Remove(hook);
    }

    private void ApplyErrorTemplate(ChangeEventArgs e)
    {
        var template = e.Value?.ToString();
        if (string.IsNullOrEmpty(template)) return;

        validationErrorResponseTemplate = template switch
        {
            "laravel" => @"{
  ""message"": ""{{validation.firstMessage}}"",
  ""errors"": {{validation.errors}}
}",
            "spring" => @"{
  ""timestamp"": ""{{timestamp}}"",
  ""status"": 400,
  ""error"": ""Bad Request"",
  ""message"": ""{{validation.firstMessage}}"",
  ""errors"": {{validation.messages}}
}",
            "minimal" => @"{
  ""error"": ""{{validation.firstMessage}}""
}",
            "detailed" => @"{
  ""success"": false,
  ""code"": ""VALIDATION_ERROR"",
  ""errorCount"": {{validation.errorCount}},
  ""message"": ""{{validation.firstMessage}}"",
  ""fields"": {{validation.fields}},
  ""errors"": {{validation.errors}},
  ""timestamp"": ""{{timestamp}}""
}",
            _ => validationErrorResponseTemplate
        };
        StateHasChanged();
    }

    private async Task InsertSampleContent()
    {
        var sample = GetSampleContent(responseModel.ContentType);
        responseModel.Body = sample;
        if (monacoEditor != null)
        {
            await monacoEditor.SetValue(sample);
        }
    }

    private string GetSampleContent(string contentType)
    {
        return contentType switch
        {
            "application/json" => @"{
  ""success"": true,
  ""data"": {
    ""id"": ""{{faker.random.uuid}}"",
    ""name"": ""{{faker.name.fullName}}"",
    ""email"": ""{{faker.internet.email}}"",
    ""avatar"": ""{{faker.image.avatar}}"",
    ""createdAt"": ""{{faker.date.recent}}""
  },
  ""meta"": {
    ""requestId"": ""{{request.id}}"",
    ""timestamp"": ""{{now}}""
  }
}",
            "application/xml" or "text/xml" => @"<?xml version=""1.0"" encoding=""UTF-8""?>
<response>
  <success>true</success>
  <data>
    <id>{{faker.random.uuid}}</id>
    <name>{{faker.name.fullName}}</name>
    <email>{{faker.internet.email}}</email>
    <createdAt>{{faker.date.recent}}</createdAt>
  </data>
  <meta>
    <requestId>{{request.id}}</requestId>
    <timestamp>{{now}}</timestamp>
  </meta>
</response>",
            "text/html" => @"<!DOCTYPE html>
<html>
<head>
  <title>Mokit Response</title>
</head>
<body>
  <h1>Hello, {{faker.name.fullName}}!</h1>
  <p>Email: {{faker.internet.email}}</p>
  <p>Created: {{now}}</p>
</body>
</html>",
            "text/plain" => @"Mokit Response
================
ID: {{faker.random.uuid}}
Name: {{faker.name.fullName}}
Email: {{faker.internet.email}}
Date: {{now}}",
            "text/javascript" => @"// Mokit JavaScript Response
const data = {
  id: '{{faker.random.uuid}}',
  name: '{{faker.name.fullName}}',
  email: '{{faker.internet.email}}',
  timestamp: '{{now}}'
};

console.log(data);",
            "text/css" => @"/* Mokit CSS Response */
.user-card {
  background: #ffffff;
  border-radius: 8px;
  padding: 16px;
}

.user-name {
  font-size: 18px;
  color: #333;
}",
            _ => "Mokit Response"
        };
    }

    private async Task LoadEndpointLogs()
    {
        if (Endpoint == null) return;
        
        loadingLogs = true;
        StateHasChanged();
        
        var result = await LogService.GetEndpointLogsAsync(Endpoint.Id, 1, 20);
        if (result.IsSuccess && result.Data != null)
        {
            endpointLogs = result.Data;
        }
        
        loadingLogs = false;
    }
    
    private string GetLogStatusBadgeClass(int statusCode)
    {
        return statusCode switch
        {
            >= 200 and < 300 => "badge-success",
            >= 300 and < 400 => "badge-info",
            >= 400 and < 500 => "badge-warning",
            >= 500 => "badge-danger",
            _ => "badge-secondary"
        };
    }

    private async Task OnContentTypeChanged()
    {
        if (monacoEditor != null)
        {
            var language = GetLanguageFromContentType(responseModel.ContentType);
            await monacoEditor.SetLanguage(language);
        }
    }

    private string GetLanguageFromContentType(string contentType)
    {
        return contentType switch
        {
            "application/json" => "json",
            "application/xml" => "xml",
            "text/xml" => "xml",
            "text/html" => "html",
            "text/plain" => "plaintext",
            "text/javascript" => "javascript",
            "application/javascript" => "javascript",
            "text/css" => "css",
            _ => "plaintext"
        };
    }

    private async Task FormatDocument()
    {
        if (monacoEditor != null)
        {
            await monacoEditor.FormatDocument();
        }
    }

    // Reuse shared models
    public class ResponseModel
    {
        public int StatusCode { get; set; } = 200;
        public string ContentType { get; set; } = "application/json";
        public string? Body { get; set; }
        public int? DelayMin { get; set; }
        public int? DelayMax { get; set; }
    }

    public class HeaderModel
    {
        public string Key { get; set; } = "";
        public string Value { get; set; } = "";
    }

    public class ValidationRuleModel
    {
        public string ParameterName { get; set; } = "";
        public string Location { get; set; } = "Query";
        public string DataType { get; set; } = "String";
        public bool IsRequired { get; set; }
        public string? RegexPattern { get; set; }
        public string? MinValue { get; set; }
        public string? MaxValue { get; set; }
        public string? AllowedValues { get; set; }
        public string? ErrorMessage { get; set; }
        public int StatusCode { get; set; } = 400;
    }

    public class WebhookModel
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public HttpMethodType Method { get; set; } = HttpMethodType.POST;
        public string? Body { get; set; }
        public string? Headers { get; set; }
        public int DelayMs { get; set; }
        public bool IsEnabled { get; set; } = true;
    }
}
