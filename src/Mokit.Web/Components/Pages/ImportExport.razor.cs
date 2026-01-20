using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Mokit.Application.DTOs.Project;
using Mokit.Application.Interfaces;

namespace Mokit.Web.Components.Pages;

public partial class ImportExport
{
    [Inject] public IImportService ImportService { get; set; } = default!;
    [Inject] public IMockProjectService ProjectService { get; set; } = default!;
    [Inject] public NavigationManager Navigation { get; set; } = default!;
    [Inject] public AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] public Mokit.Web.Services.IToastService ToastService { get; set; } = default!;

    private string importFormat = "postman";
    private string? selectedProjectId;
    private IBrowserFile? selectedFile;
    private string? fileContent;
    private bool isProcessing;
    private bool isDragging;
    // Error/Success messages replaced by ToastService
    private ParsedCollectionData? parsedData;
    private ImportOptions importOptions = new() { SkipDuplicates = true, CreateExamples = true, ImportHeaders = true };
    private List<MockProjectDto> userProjects = new();
    private HashSet<ParsedEndpoint> expandedEndpoints = new();
    private string? userId;
    private bool formatAutoDetected;
    private bool formatDropdownOpen;
    private bool projectDropdownOpen;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        userId = authState.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        await LoadUserProjects();
    }

    private async Task LoadUserProjects()
    {
        if (string.IsNullOrEmpty(userId)) return;
        
        var result = await ProjectService.GetAllAsync(userId);
        if (result.IsSuccess && result.Data != null)
        {
            userProjects = result.Data;
        }
    }

    private async Task OnFileSelected(InputFileChangeEventArgs e)
    {
        parsedData = null;
        formatAutoDetected = false;
        selectedFile = e.File;

        try
        {
            using var stream = selectedFile.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
            using var reader = new StreamReader(stream);
            fileContent = await reader.ReadToEndAsync();
            
            // Auto-detect format
            DetectImportFormat();
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Failed to read file: {ex.Message}");
            selectedFile = null;
        }
    }
    
    private void DetectImportFormat()
    {
        if (selectedFile == null || string.IsNullOrEmpty(fileContent)) return;
        
        var fileName = selectedFile.Name.ToLowerInvariant();
        var content = fileContent.TrimStart();
        
        // Check file extension first
        var isYamlFile = fileName.EndsWith(".yaml") || fileName.EndsWith(".yml");
        var isJsonFile = fileName.EndsWith(".json");
        
        // Try to detect by content
        if (isYamlFile)
        {
            // YAML file - check if it's OpenAPI
            if (content.Contains("openapi:") || content.Contains("swagger:"))
            {
                importFormat = "openapi-yaml";
                formatAutoDetected = true;
            }
        }
        else if (isJsonFile || content.StartsWith("{"))
        {
            // JSON content - determine if Postman or OpenAPI
            if (content.Contains("\"openapi\"") || content.Contains("\"swagger\""))
            {
                importFormat = "openapi-json";
                formatAutoDetected = true;
            }
            else if (content.Contains("\"info\"") && content.Contains("\"item\""))
            {
                // Postman collection has "info" and "item" at root level
                importFormat = "postman";
                formatAutoDetected = true;
            }
            else if (content.Contains("\"paths\""))
            {
                // OpenAPI has "paths"
                importFormat = "openapi-json";
                formatAutoDetected = true;
            }
            else if (content.Contains("\"_postman_id\"") || content.Contains("\"schema\":") && content.Contains("getpostman.com"))
            {
                importFormat = "postman";
                formatAutoDetected = true;
            }
        }
    }

    private void ClearFile()
    {
        selectedFile = null;
        fileContent = null;
        parsedData = null;
        formatAutoDetected = false;
    }

    private void HandleDragEnter() => isDragging = true;
    private void HandleDragLeave() => isDragging = false;
    private void HandleDrop() => isDragging = false;

    private async Task ParseCollection()
    {
        if (string.IsNullOrEmpty(fileContent))
        {
            ToastService.ShowError("Please select a file first.");
            return;
        }

        isProcessing = true;
        expandedEndpoints.Clear();

        try
        {
            Application.Common.Result<ParsedCollectionData> result;

            if (importFormat == "postman")
            {
                result = await ImportService.ParsePostmanCollectionAsync(fileContent);
            }
            else
            {
                var isYaml = importFormat == "openapi-yaml";
                result = await ImportService.ParseOpenApiSpecAsync(fileContent, isYaml);
            }

            if (result.IsSuccess && result.Data != null)
            {
                parsedData = result.Data;
                ToastService.ShowSuccess($"Successfully parsed {parsedData.Endpoints.Count} endpoints.");
            }
            else
            {
                 ToastService.ShowError(result.Error ?? "Failed to parse collection.");
            }
        }
        catch (Exception ex)
        {
             ToastService.ShowError($"Error parsing collection: {ex.Message}");
        }
        finally
        {
            isProcessing = false;
        }
    }

    private async Task ImportCollection()
    {
        if (parsedData == null || string.IsNullOrEmpty(selectedProjectId))
        {
             ToastService.ShowError("Please select a target project.");
            return;
        }

        isProcessing = true;

        try
        {
            var projectId = Guid.Parse(selectedProjectId);
            var result = await ImportService.ImportToProjectAsync(userId ?? "", projectId, parsedData, importOptions);

            if (result.IsSuccess && result.Data != null)
            {
                var importResult = result.Data;
                var message = $"Import completed! Created {importResult.EndpointsCreated} endpoints and {importResult.ResponsesCreated} responses.";

                if (importResult.Warnings.Any())
                {
                    message += $" Warnings: {string.Join(", ", importResult.Warnings)}";
                    ToastService.ShowWarning(message);
                }
                else
                {
                    ToastService.ShowSuccess(message);
                }

                Navigation.NavigateTo($"/projects/{selectedProjectId}");
            }
            else
            {
                ToastService.ShowError(result.Error ?? "Failed to import collection.");
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Error importing collection: {ex.Message}");
        }
        finally
        {
            isProcessing = false;
        }
    }

    private void ToggleEndpoint(ParsedEndpoint endpoint)
    {
        if (expandedEndpoints.Contains(endpoint))
            expandedEndpoints.Remove(endpoint);
        else
            expandedEndpoints.Add(endpoint);
    }

    private void ToggleAllEndpoints()
    {
        if (parsedData == null) return;
        
        if (expandedEndpoints.Count == parsedData.Endpoints.Count)
            expandedEndpoints.Clear();
        else
            expandedEndpoints = new HashSet<ParsedEndpoint>(parsedData.Endpoints);
    }

    private string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F1} MB";
    }

    private string GetStatusClass(int statusCode) => statusCode switch
    {
        >= 200 and < 300 => "2xx",
        >= 300 and < 400 => "3xx",
        >= 400 and < 500 => "4xx",
        >= 500 => "5xx",
        _ => "2xx"
    };
    
    private string GetFormatDisplayName(string format) => format switch
    {
        "postman" => "Postman Collection (v2.1)",
        "openapi-json" => "OpenAPI Specification (JSON)",
        "openapi-yaml" => "OpenAPI Specification (YAML)",
        _ => "Select format..."
    };
    
    private string GetProjectName(string? projectId)
    {
        if (string.IsNullOrEmpty(projectId)) return "Select a project...";
        var project = userProjects.FirstOrDefault(p => p.Id.ToString() == projectId);
        return project?.Name ?? "Select a project...";
    }
    
    private void ToggleFormatDropdown()
    {
        formatDropdownOpen = !formatDropdownOpen;
        projectDropdownOpen = false;
    }
    
    private void ToggleProjectDropdown()
    {
        projectDropdownOpen = !projectDropdownOpen;
        formatDropdownOpen = false;
    }
    
    private void SelectFormat(string format)
    {
        importFormat = format;
        formatDropdownOpen = false;
        formatAutoDetected = false;
    }
    
    private void SelectProject(string projectId)
    {
        selectedProjectId = projectId;
        projectDropdownOpen = false;
    }
    
    private void CloseDropdowns()
    {
        formatDropdownOpen = false;
        projectDropdownOpen = false;
    }
    
}
