using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;
using Mokit.Application.Common;
using Mokit.Application.DTOs.Project;
using Mokit.Application.Interfaces;
using Mokit.Web.Hubs;
using Mokit.Web.Services;

namespace Mokit.Web.Components.Pages;

public partial class Logs : IAsyncDisposable
{
    [Inject] public NavigationManager Navigation { get; set; } = default!;
    [Inject] public IMockProjectService ProjectService { get; set; } = default!;
    [Inject] public IRequestLogService LogService { get; set; } = default!;
    [Inject] public IUserService UserService { get; set; } = default!;
    [Inject] public AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] public IToastService ToastService { get; set; } = default!;

    private HubConnection? hubConnection;
    private List<RequestLogDetailDto> logs = new();
    private List<MockProjectDto> projects = new();
    private Guid? selectedProjectId;
    private string? userId;
    private bool isAdmin = false;
    private bool loading = true;
    private int currentPage = 1;
    private bool hasMoreLogs = false;
    private int totalLogCount = 0;
    
    // Detail modal
    private bool showDetailModal = false;
    private RequestLogDetailDto? selectedLog;
    private string detailTab = "request";
    private bool canDeleteLog = false;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        userId = authState.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        if (!string.IsNullOrEmpty(userId))
        {
            isAdmin = await UserService.IsAdminAsync(userId);
            
            var result = await ProjectService.GetAllAsync(userId);
            if (result.IsSuccess)
            {
                projects = result.Data ?? new List<MockProjectDto>();
            }
            
            await LoadLogs();
        }
        
        loading = false;

        // Setup SignalR connection for real-time updates
        hubConnection = new HubConnectionBuilder()
            .WithUrl(Navigation.ToAbsoluteUri("/Mokit"))
            .Build();

        hubConnection.On<RequestLogDto>("RequestReceived", async (logDto) =>
        {
            if (!selectedProjectId.HasValue || logDto.ProjectId == selectedProjectId.Value)
            {
                // Reload logs to get the full details
                await LoadLogs(resetPage: true);
                await InvokeAsync(StateHasChanged);
            }
        });

        try
        {
            await hubConnection.StartAsync();
        }
        catch
        {
            // SignalR connection failed
        }
    }

    private async Task LoadLogs(bool resetPage = false)
    {
        if (resetPage) currentPage = 1;
        
        Result<List<RequestLogDetailDto>> result;
        
        if (isAdmin)
        {
            result = await LogService.GetAllLogsAsync(currentPage, 50, selectedProjectId);
        }
        else if (!string.IsNullOrEmpty(userId))
        {
            result = await LogService.GetUserLogsAsync(userId, currentPage, 50, selectedProjectId);
        }
        else
        {
            return;
        }
        
        if (result.IsSuccess && result.Data != null)
        {
            if (resetPage || currentPage == 1)
            {
                logs = result.Data;
            }
            else
            {
                logs.AddRange(result.Data);
            }
            hasMoreLogs = result.Data.Count == 50;
        }
        else if (!result.IsSuccess)
        {
            // Optional: Show error on load failure
            // ToastService.ShowError("Failed to load logs"); 
            // Often silent failure on load is better than spamming, but depends on preference.
        }
        
        totalLogCount = await LogService.GetLogCountAsync(userId, isAdmin);
    }

    private async Task LoadMoreLogs()
    {
        currentPage++;
        await LoadLogs();
    }

    private async Task RefreshLogs()
    {
        loading = true;
        await LoadLogs(resetPage: true);
        loading = false;
    }

    private async Task OnProjectChanged(ChangeEventArgs e)
    {
        if (Guid.TryParse(e.Value?.ToString(), out var projectId))
        {
            selectedProjectId = projectId;
        }
        else
        {
            selectedProjectId = null;
        }
        
        loading = true;
        await LoadLogs(resetPage: true);
        loading = false;
    }

    private void ShowLogDetail(RequestLogDetailDto log)
    {
        selectedLog = log;
        detailTab = "request";
        canDeleteLog = isAdmin || projects.Any(p => p.Id == log.ProjectId);
        showDetailModal = true;
    }

    private void CloseDetailModal()
    {
        showDetailModal = false;
        selectedLog = null;
    }

    // Confirmation state
    private bool showConfirmation = false;
    private string confirmationTitle = "";
    private string confirmationMessage = "";
    private LogAction pendingAction = LogAction.None;

    private enum LogAction { None, DeleteSingle, DeleteProject, DeleteAll }

    private void PromptDeleteSelectedLog()
    {
        if (selectedLog == null) return;
        confirmationTitle = "Delete Log Entry";
        confirmationMessage = "Are you sure you want to delete this log entry? This action cannot be undone.";
        pendingAction = LogAction.DeleteSingle;
        showConfirmation = true;
    }

    private void PromptDeleteProjectLogs()
    {
        if (!selectedProjectId.HasValue) return;
        confirmationTitle = "Clear Project Logs";
        confirmationMessage = "Are you sure you want to delete ALL logs for this project? This action cannot be undone.";
        pendingAction = LogAction.DeleteProject;
        showConfirmation = true;
    }

    private void PromptDeleteAllLogs()
    {
        confirmationTitle = "Clear All Logs";
        confirmationMessage = "Are you sure you want to delete ALL logs in the system? This action cannot be undone.";
        pendingAction = LogAction.DeleteAll;
        showConfirmation = true;
    }

    private void CancelConfirmation()
    {
        showConfirmation = false;
        pendingAction = LogAction.None;
    }

    private async Task ExecuteConfirmedAction()
    {
        showConfirmation = false;

        switch (pendingAction)
        {
            case LogAction.DeleteSingle:
                await DeleteSelectedLog();
                break;
            case LogAction.DeleteProject:
                await DeleteProjectLogs();
                break;
            case LogAction.DeleteAll:
                await DeleteAllLogs();
                break;
        }

        pendingAction = LogAction.None;
    }

    private async Task DeleteSelectedLog()
    {
        if (selectedLog == null || string.IsNullOrEmpty(userId)) return;
        
        var result = await LogService.DeleteLogAsync(selectedLog.Id, userId, isAdmin);
        if (result.IsSuccess)
        {
            ToastService.ShowSuccess("Log entry deleted successfully");
            logs.Remove(selectedLog);
            totalLogCount--;
            CloseDetailModal();
        }
        else
        {
            ToastService.ShowError(result.Error ?? "Failed to delete log");
        }
    }

    private async Task DeleteProjectLogs()
    {
        if (!selectedProjectId.HasValue || string.IsNullOrEmpty(userId)) return;
        
        var result = await LogService.DeleteProjectLogsAsync(selectedProjectId.Value, userId, isAdmin);
        if (result.IsSuccess)
        {
            ToastService.ShowSuccess("Project logs cleared");
            await LoadLogs(resetPage: true);
        }
        else
        {
             ToastService.ShowError(result.Error ?? "Failed to clear project logs");
        }
    }

    private async Task DeleteAllLogs()
    {
        if (!isAdmin) return;
        
        var result = await LogService.DeleteAllLogsAsync();
        if (result.IsSuccess)
        {
            ToastService.ShowSuccess("All logs cleared");
            logs.Clear();
            totalLogCount = 0;
        }
        else
        {
             ToastService.ShowError(result.Error ?? "Failed to clear all logs");
        }
    }

    private string GetStatusBadgeClass(int statusCode)
    {
        return statusCode switch
        {
            >= 200 and < 300 => "badge badge-success",
            >= 300 and < 400 => "badge badge-info",
            >= 400 and < 500 => "badge badge-warning",
            >= 500 => "badge badge-danger",
            _ => "badge badge-secondary"
        };
    }
    
    private void SetDetailTabRequest() => detailTab = "request";
    private void SetDetailTabResponse() => detailTab = "response";
    private void SetDetailTabHeaders() => detailTab = "headers";

    private string FormatJson(string? json)
    {
        if (string.IsNullOrEmpty(json)) return "";
        
        try
        {
            var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return json;
        }
    }

    private string FormatHeaders(string? headersJson)
    {
        if (string.IsNullOrEmpty(headersJson)) return "";
        
        try
        {
            var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(headersJson);
            if (headers != null)
            {
                return string.Join("\n", headers.Select(h => $"{h.Key}: {h.Value}"));
            }
        }
        catch
        {
            return headersJson;
        }
        
        return headersJson;
    }

    private string FormatQueryString(string? queryString)
    {
        if (string.IsNullOrEmpty(queryString)) return "";
        
        var qs = queryString.TrimStart('?');
        var pairs = qs.Split('&');
        return string.Join("\n", pairs.Select(p => {
            var parts = p.Split('=', 2);
            return parts.Length == 2 ? $"{parts[0]} = {Uri.UnescapeDataString(parts[1])}" : p;
        }));
    }

    public async ValueTask DisposeAsync()
    {
        if (hubConnection is not null)
        {
            await hubConnection.DisposeAsync();
        }
    }
}
