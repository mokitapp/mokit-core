using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Mokit.Application.Interfaces;
using Mokit.Domain.Entities;
using Mokit.Domain.Enums;
using Mokit.Web.Components.Shared;
using Mokit.Application.DTOs;
using Mokit.Application.DTOs.Project;
using Mokit.Application.DTOs.Endpoint;
using Mokit.Web.Services;
using Microsoft.JSInterop;

namespace Mokit.Web.Components.Pages;

public partial class ProjectEdit
{
    [Inject] public IMockProjectService ProjectService { get; set; } = default!;
    [Inject] public ITeamService TeamService { get; set; } = default!;
    [Inject] public IUserService UserService { get; set; } = default!;
    [Inject] public IMockEndpointService EndpointService { get; set; } = default!;
    [Inject] public NavigationManager Navigation { get; set; } = default!;
    [Inject] public AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] public IToastService ToastService { get; set; } = default!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = default!;

    [Parameter] public Guid ProjectId { get; set; }
    
    private bool isNew => ProjectId == Guid.Empty;
    private bool loading = true;
    private bool saving = false;
    private string? userId;
    private bool isAdmin = false;
    private bool canDeleteProject = false;
    
    private MockProjectDto? project;
    private List<MockEndpointDto> endpoints = new();
    
    private CreateMockProjectDto model = new();
    private bool showEndpointModal = false;
    private MockEndpointDto? editingEndpoint;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        userId = authState.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            isAdmin = await UserService.IsAdminAsync(userId);
        }

        if (!isNew)
        {
            var projectResult = await ProjectService.GetByIdAsync(ProjectId);
            if (projectResult.IsSuccess && projectResult.Data != null)
            {
                project = projectResult.Data;
                model = new CreateMockProjectDto
                {
                    Name = project.Name,
                    Slug = project.Slug,
                    Description = project.Description,
                    EnableCors = project.EnableCors,
                    EnableLogging = project.EnableLogging
                };

                // Check delete permission
                if (isAdmin)
                {
                    canDeleteProject = true;
                }
                else if (project.TeamId.HasValue && !string.IsNullOrEmpty(userId))
                {
                    canDeleteProject = await TeamService.IsUserTeamAdminAsync(project.TeamId.Value, userId);
                }
                else if (!string.IsNullOrEmpty(userId))
                {
                    // Personal project
                    canDeleteProject = project.UserId == userId;
                }

                var endpointsResult = await EndpointService.GetByProjectAsync(ProjectId);
                if (endpointsResult.IsSuccess)
                {
                    endpoints = endpointsResult.Data ?? new List<MockEndpointDto>();
                }
                else
                {
                     ToastService.ShowError(endpointsResult.Error ?? "Failed to load endpoints");
                }
            }
            else
            {
                ToastService.ShowError(projectResult.Error ?? "Failed to load project");
                Navigation.NavigateTo("/projects");
                return;
            }
        }
        else
        {
            model.EnableCors = true;
            model.EnableLogging = true;
        }

        loading = false;
    }

    private async Task SaveProject()
    {
        if (string.IsNullOrEmpty(model.Name))
        {
            ToastService.ShowError("Project name is required");
            return;
        }

        saving = true;
        StateHasChanged();

        try
        {
            if (isNew)
            {
                if (string.IsNullOrEmpty(userId))
                {
                    var authState = await AuthStateProvider.GetAuthenticationStateAsync();
                    userId = authState.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    
                    if (string.IsNullOrEmpty(userId))
                    {
                        ToastService.ShowError("User session not found. Please sign in again.");
                        return;
                    }
                }
                
                var result = await ProjectService.CreateAsync(userId, model);
                
                if (result.IsSuccess)
                {
                    ToastService.ShowSuccess("Project created successfully");
                    Navigation.NavigateTo($"/projects/{result.Data!.Id}");
                }
                else
                {
                    ToastService.ShowError(result.Error ?? "An error occurred while creating the project.");
                }
            }
            else
            {
                var updateDto = new UpdateMockProjectDto
                {
                    Name = model.Name,
                    Description = model.Description,
                    EnableCors = model.EnableCors,
                    EnableLogging = model.EnableLogging,
                    IsActive = true
                };
                var result = await ProjectService.UpdateAsync(ProjectId, updateDto);
                if (!result.IsSuccess)
                {
                    ToastService.ShowError(result.Error ?? "An error occurred while updating the project.");
                }
                else
                {
                    project = result.Data;
                    ToastService.ShowSuccess("Project updated successfully");
                }
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"An error occurred: {ex.Message}");
        }
        finally
        {
            saving = false;
            StateHasChanged();
        }
    }

    private void Cancel()
    {
        Navigation.NavigateTo("/projects");
    }

    private string GetMockBaseUrl()
    {
        if (project == null) return "";
        var baseUrl = Navigation.BaseUri.TrimEnd('/');
        return project.MockUrl;
    }

    private async Task CopyMockUrl()
    {
        var url = GetMockBaseUrl();
        if (!string.IsNullOrEmpty(url))
        {
            try {
                await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", url);
                ToastService.ShowSuccess("URL copied to clipboard");
            } catch {
                ToastService.ShowError("Failed to copy URL");
            }
        }
    }

    private void AddEndpoint()
    {
        editingEndpoint = null;
        showEndpointModal = true;
    }

    private void EditEndpoint(Guid endpointId)
    {
        editingEndpoint = endpoints.FirstOrDefault(e => e.Id == endpointId);
        if (editingEndpoint != null)
        {
            showEndpointModal = true;
        }
    }

    private void OnEndpointSaved(MockEndpointDto savedEndpoint)
    {
        if (editingEndpoint == null)
        {
            endpoints.Add(savedEndpoint);
            ToastService.ShowSuccess("Endpoint added successfully");
        }
        else
        {
            var index = endpoints.FindIndex(e => e.Id == savedEndpoint.Id);
            if (index >= 0)
            {
                endpoints[index] = savedEndpoint;
                ToastService.ShowSuccess("Endpoint updated successfully");
            }
        }
        CloseEndpointModal();
    }

    // Confirmation state
    private bool showConfirmation = false;
    private Guid? endpointIdToDelete;
    private bool isDeletingProject = false;
    private string confirmationTitle = "";
    private string confirmationMessage = "";

    private void PromptDeleteEndpoint(Guid endpointId)
    {
        endpointIdToDelete = endpointId;
        isDeletingProject = false;
        confirmationTitle = "Delete Endpoint";
        confirmationMessage = "Are you sure you want to delete this endpoint? This action cannot be undone.";
        showConfirmation = true;
    }

    private void PromptDeleteProject()
    {
        isDeletingProject = true;
        endpointIdToDelete = null;
        confirmationTitle = "Delete Project";
        confirmationMessage = "Are you sure you want to delete this ENTIRE PROJECT? All endpoints and logs will be permanently removed. This action cannot be undone.";
        showConfirmation = true;
    }

    private void CancelConfirmation()
    {
        showConfirmation = false;
        endpointIdToDelete = null;
        isDeletingProject = false;
    }

    private async Task ConfirmAction()
    {
        if (isDeletingProject)
        {
            await DeleteProject();
        }
        else if (endpointIdToDelete.HasValue)
        {
            await DeleteEndpoint(endpointIdToDelete.Value);
        }
        
        showConfirmation = false;
        endpointIdToDelete = null;
        isDeletingProject = false;
    }

    private async Task DeleteEndpoint(Guid endpointId)
    {
        var result = await EndpointService.DeleteAsync(endpointId);
        if (result.IsSuccess)
        {
            endpoints.RemoveAll(e => e.Id == endpointId);
            ToastService.ShowSuccess("Endpoint deleted successfully");
        }
        else
        {
            ToastService.ShowError(result.Error ?? "Failed to delete endpoint");
        }
    }

    private async Task DeleteProject()
    {
        var result = await ProjectService.DeleteAsync(ProjectId);
        if (result.IsSuccess)
        {
            ToastService.ShowSuccess("Project deleted successfully");
            Navigation.NavigateTo("/projects");
        }
        else
        {
            ToastService.ShowError(result.Error ?? "Failed to delete project");
        }
    }

    private void CloseEndpointModal()
    {
        showEndpointModal = false;
        editingEndpoint = null;
    }
}
