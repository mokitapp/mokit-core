using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Mokit.Application.DTOs.Project;
using Mokit.Application.DTOs.Team;
using Mokit.Application.DTOs.User;
using Mokit.Application.Interfaces;
using Mokit.Application.Constants;
using Mokit.Web.Services;

namespace Mokit.Web.Components.Pages;

public partial class TeamDetail
{
    [Inject] public ITeamService TeamService { get; set; } = default!;
    [Inject] public IMockProjectService ProjectService { get; set; } = default!;
    [Inject] public IUserService UserService { get; set; } = default!;
    [Inject] public NavigationManager Navigation { get; set; } = default!;
    [Inject] public AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

    [Parameter] public Guid TeamId { get; set; }
    
    [Inject] public IToastService ToastService { get; set; } = default!;
    
    private TeamDto? team;
    private List<TeamMemberDto> members = new();
    private List<UserDto> allUsers = new();
    private bool loading = true;
    private string? userId;
    private bool isAdmin = false;
    private bool isTeamAdmin = false;
    
    private UpdateTeamDto editModel = new();
    private bool showProjectModal = false;
    private bool showMemberModal = false;
    private CreateMockProjectDto projectModel = new();
    private AddTeamMemberDto memberModel = new();
    
    // User search
    private string userSearchTerm = "";
    private string? selectedUserId;
    private UserDto? selectedUser;
    private bool showUserDropdown = false;

    private IEnumerable<UserDto> filteredUsers => string.IsNullOrWhiteSpace(userSearchTerm)
        ? allUsers.Where(u => !members.Any(m => m.UserId == u.Id)).Take(8)
        : allUsers.Where(u => 
            !members.Any(m => m.UserId == u.Id) &&
            (u.FullName.Contains(userSearchTerm, StringComparison.OrdinalIgnoreCase) ||
             u.Email.Contains(userSearchTerm, StringComparison.OrdinalIgnoreCase)))
          .Take(8);

    // Does the user have permission to change team settings and add members?
    private bool canManageTeam => isAdmin || isTeamAdmin;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        userId = authState.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        if (!string.IsNullOrEmpty(userId))
        {
            // Is global admin?
            isAdmin = await UserService.IsAdminAsync(userId);
            // Is team admin? (Owner or Admin)
            isTeamAdmin = await TeamService.IsUserTeamAdminAsync(TeamId, userId);
        }
        
        await LoadTeam();
        await LoadMembers();
        
        // Load users only if admin
        if (canManageTeam)
        {
            await LoadUsers();
        }
        
        loading = false;
    }

    private async Task LoadUsers()
    {
        var result = await UserService.GetAllUsersAsync();
        if (result.IsSuccess)
        {
            allUsers = result.Data?.Where(u => u.IsActive).ToList() ?? new();
        }
        else
        {
            ToastService.ShowError(result.Error ?? "Failed to load users");
        }
    }

    private void OpenUserDropdown()
    {
        showUserDropdown = true;
        StateHasChanged();
    }

    private void CloseUserDropdown()
    {
        showUserDropdown = false;
        StateHasChanged();
    }

    private void SelectUser(UserDto user)
    {
        selectedUserId = user.Id;
        selectedUser = user;
        memberModel.Email = user.Email;
        userSearchTerm = "";
        showUserDropdown = false;
    }

    private void ClearSelectedUser()
    {
        selectedUserId = null;
        selectedUser = null;
        memberModel.Email = "";
        showUserDropdown = false;
    }

    private async Task LoadTeam()
    {
        var result = await TeamService.GetByIdAsync(TeamId);
        if (result.IsSuccess && result.Data != null)
        {
            team = result.Data;
            editModel = new UpdateTeamDto
            {
                Name = team.Name,
                Description = team.Description,
                IsActive = team.IsActive
            };
        }
        else
        {
            ToastService.ShowError(result.Error ?? "Failed to load team");
        }
    }

    private async Task LoadMembers()
    {
        var result = await TeamService.GetMembersAsync(TeamId);
        if (result.IsSuccess)
        {
            members = result.Data ?? new List<TeamMemberDto>();
        }
        else
        {
             ToastService.ShowError(result.Error ?? "Failed to load members");
        }
    }

    private async Task SaveTeam()
    {
        var result = await TeamService.UpdateAsync(TeamId, editModel);
        if (result.IsSuccess)
        {
            team = result.Data;
            ToastService.ShowSuccess("Team updated successfully");
        }
        else
        {
            ToastService.ShowError(result.Error ?? "Failed to update team");
        }
    }

    private void ShowNewProjectModal()
    {
        projectModel = new CreateMockProjectDto
        {
            TeamId = TeamId,
            EnableCors = true,
            EnableLogging = true
        };
        showProjectModal = true;
    }

    private void CloseProjectModal()
    {
        showProjectModal = false;
    }

    private async Task CreateProject()
    {
        if (string.IsNullOrEmpty(projectModel.Name) || string.IsNullOrEmpty(userId))
            return;

        var result = await ProjectService.CreateAsync(userId, projectModel);
        if (result.IsSuccess)
        {
            ToastService.ShowSuccess("Project created successfully");
            await LoadTeam();
            CloseProjectModal();
            Navigation.NavigateTo($"/projects/{result.Data!.Id}");
        }
        else
        {
            ToastService.ShowError(result.Error ?? "Failed to create project");
        }
    }

    private void ShowAddMemberModal()
    {
        memberModel = new AddTeamMemberDto { Role = RoleConstants.Member };
        selectedUserId = null;
        selectedUser = null;
        userSearchTerm = "";
        showUserDropdown = false;
        showMemberModal = true;
    }

    private void CloseMemberModal()
    {
        showMemberModal = false;
        selectedUserId = null;
        selectedUser = null;
        userSearchTerm = "";
        showUserDropdown = false;
    }

    private async Task AddMember()
    {
        if (selectedUser == null || string.IsNullOrEmpty(memberModel.Email))
            return;

        var result = await TeamService.AddMemberAsync(TeamId, memberModel);
        if (result.IsSuccess)
        {
            ToastService.ShowSuccess("Member added successfully");
            await LoadMembers();
            await LoadUsers();
            CloseMemberModal();
        }
        else
        {
            ToastService.ShowError(result.Error ?? "Failed to add member");
        }
    }

    private void NavigateToProject(Guid projectId)
    {
        Navigation.NavigateTo($"/projects/{projectId}");
    }

    private string GetRoleBadgeClass(string role)
    {
        return role switch
        {
            RoleConstants.Owner => "badge-primary",
            RoleConstants.Admin => "badge-warning",
            _ => "badge-secondary"
        };
    }
}
