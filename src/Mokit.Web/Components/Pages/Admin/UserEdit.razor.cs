using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Mokit.Application.Constants;
using Mokit.Application.DTOs.Team;
using Mokit.Application.DTOs.User;
using Mokit.Application.Interfaces;
using Mokit.Web.Services;

namespace Mokit.Web.Components.Pages.Admin;

public partial class UserEdit
{
    [Inject] public IUserService UserService { get; set; } = default!;
    [Inject] public ITeamService TeamService { get; set; } = default!;
    [Inject] public NavigationManager Navigation { get; set; } = default!;
    [Inject] public AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] public IToastService ToastService { get; set; } = default!;

    [Parameter] public string UserId { get; set; } = "";

    private UserDto? user;
    private List<TeamDto> availableTeams = new();
    private bool loading = true;
    private bool isAdmin = false;
    private string? currentUserId;

    private bool showAddTeamModal = false;
    private string selectedTeamId = "";
    private string selectedRole = RoleConstants.Member;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        currentUserId = authState.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(currentUserId))
        {
            Navigation.NavigateTo("/Account/Login");
            return;
        }

        isAdmin = await UserService.IsAdminAsync(currentUserId);

        if (isAdmin)
        {
            await LoadUser();
            await LoadAvailableTeams();
        }

        loading = false;
    }

    private async Task LoadUser()
    {
        var result = await UserService.GetByIdAsync(UserId);
        if (result.IsSuccess)
        {
            user = result.Data;
        }
        else 
        {
             ToastService.ShowError(result.Error ?? "Failed to load user");
        }
    }

    private async Task LoadAvailableTeams()
    {
        var result = await TeamService.GetAllTeamsAsync();
        if (result.IsSuccess)
        {
            availableTeams = result.Data ?? new();
        }
    }

    // Confirmation state
    private bool showConfirmation = false;
    private string confirmationTitle = "";
    private string confirmationMessage = "";
    private UserAction pendingAction = UserAction.None;
    private Guid? teamIdToRemove;

    private enum UserAction { None, Deactivate, Reactivate, ToggleAdmin, RemoveFromTeam }

    private void PromptDeactivateUser()
    {
        confirmationTitle = "Deactivate User";
        confirmationMessage = "Are you sure you want to deactivate this user? They will no longer be able to log in.";
        pendingAction = UserAction.Deactivate;
        showConfirmation = true;
    }

    private void PromptReactivateUser()
    {
        confirmationTitle = "Reactivate User";
        confirmationMessage = "Are you sure you want to reactivate this user? They will be able to log in again.";
        pendingAction = UserAction.Reactivate;
        showConfirmation = true;
    }

    private void PromptToggleAdmin()
    {
        bool willBeAdmin = !user!.IsAdmin;
        confirmationTitle = willBeAdmin ? "Grant Admin Privileges" : "Revoke Admin Privileges";
        confirmationMessage = willBeAdmin 
            ? "Are you sure you want to make this user an Admin? They will have full access to the system." 
            : "Are you sure you want to revoke Admin privileges? They will lose access to administrative features.";
        pendingAction = UserAction.ToggleAdmin;
        showConfirmation = true;
    }

    private void PromptRemoveFromTeam(Guid teamId)
    {
        teamIdToRemove = teamId;
        confirmationTitle = "Remove from Team";
        confirmationMessage = "Are you sure you want to remove this user from the team?";
        pendingAction = UserAction.RemoveFromTeam;
        showConfirmation = true;
    }

    private void CancelConfirmation()
    {
        showConfirmation = false;
        pendingAction = UserAction.None;
        teamIdToRemove = null;
    }

    private async Task ExecuteConfirmedAction()
    {
        showConfirmation = false;

        switch (pendingAction)
        {
            case UserAction.Deactivate:
                await DeactivateUser();
                break;
            case UserAction.Reactivate:
                await ReactivateUser();
                break;
            case UserAction.ToggleAdmin:
                await ToggleAdmin();
                break;
            case UserAction.RemoveFromTeam:
                if (teamIdToRemove.HasValue)
                    await RemoveFromTeam(teamIdToRemove.Value);
                break;
        }

        pendingAction = UserAction.None;
        teamIdToRemove = null;
    }

    private async Task DeactivateUser()
    {
        var result = await UserService.DeactivateUserAsync(UserId);
        if (result.IsSuccess)
        {
            ToastService.ShowSuccess("User deactivated successfully");
            await LoadUser();
            StateHasChanged();
        }
        else
        {
            ToastService.ShowError(result.Error ?? "Operation failed");
        }
    }

    private async Task ReactivateUser()
    {
        var result = await UserService.ReactivateUserAsync(UserId);
        if (result.IsSuccess)
        {
            ToastService.ShowSuccess("User activated successfully");
            await LoadUser();
            StateHasChanged();
        }
        else 
        {
            ToastService.ShowError(result.Error ?? "Operation failed");
        }
    }

    private async Task ToggleAdmin()
    {
        if (user == null) return;

        var dto = new UpdateUserDto
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            AvatarUrl = user.AvatarUrl,
            IsActive = user.IsActive,
            IsAdmin = !user.IsAdmin
        };

        var result = await UserService.UpdateUserAsync(UserId, dto);
        if (result.IsSuccess)
        {
            var msg = !user.IsAdmin ? "Admin privileges granted" : "Admin privileges revoked";
            ToastService.ShowSuccess(msg);
            await LoadUser();
            StateHasChanged();
        }
        else 
        {
            ToastService.ShowError(result.Error ?? "Operation failed");
        }
    }

    private async Task AddToTeam()
    {
        if (string.IsNullOrEmpty(selectedTeamId)) return;

        var result = await UserService.AddUserToTeamAsync(UserId, Guid.Parse(selectedTeamId), selectedRole);
        if (result.IsSuccess)
        {
            ToastService.ShowSuccess("User added to team successfully");
            showAddTeamModal = false;
            await LoadUser();
            StateHasChanged();
        }
        else 
        {
            ToastService.ShowError(result.Error ?? "Failed to add user to team");
        }
    }

    private async Task RemoveFromTeam(Guid teamId)
    {
        var result = await UserService.RemoveUserFromTeamAsync(UserId, teamId);
        if (result.IsSuccess)
        {
            ToastService.ShowSuccess("User removed from team successfully");
            await LoadUser();
            StateHasChanged();
        }
        else {
            ToastService.ShowError(result.Error ?? "Failed to remove user from team");
        }
    }

    private string GetRoleDisplayName(string role) => role switch
    {
        RoleConstants.Owner => "Owner",
        RoleConstants.Admin => "Admin",
        RoleConstants.Member => "Member",
        _ => role
    };
}
