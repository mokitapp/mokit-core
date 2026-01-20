using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Mokit.Application.DTOs.Project;
using Mokit.Application.Interfaces;

namespace Mokit.Web.Components.Pages;

public partial class Projects
{
    [Inject] public IMockProjectService ProjectService { get; set; } = default!;
    [Inject] public NavigationManager Navigation { get; set; } = default!;
    [Inject] public AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

    private List<MockProjectDto> projects = new();
    private bool loading = true;
    private string? userId;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        userId = authState.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        if (!string.IsNullOrEmpty(userId))
        {
            var result = await ProjectService.GetAllAsync(userId);
            if (result.IsSuccess)
            {
                projects = result.Data ?? new List<MockProjectDto>();
            }
        }
        
        loading = false;
    }

    private void NavigateToProject(Guid projectId)
    {
        Navigation.NavigateTo($"/projects/{projectId}");
    }
}
