using System.Collections.ObjectModel;
using System.Runtime.Versioning;
using MauiAppIT13.Controllers;
using MauiAppIT13.Models;
using MauiAppIT13.Utils;
using Microsoft.Maui.ApplicationModel;

namespace MauiAppIT13.Pages.Admin;

[SupportedOSPlatform("windows10.0.17763.0")]
[SupportedOSPlatform("android21.0")]
public partial class AdminUsersPage : ContentPage, IQueryAttributable
{
    private readonly UserController _userController;
    private readonly ObservableCollection<User> _allUsers = new();
    private readonly ObservableCollection<User> _filteredUsers = new();
    private User? _editingUser = null;
    private bool _showActive = true;
    private bool _showInactive;
    private string _currentSearchText = string.Empty;
    private string? _pendingAction;

    public AdminUsersPage()
    {
        InitializeComponent();
        _userController = AppServiceProvider.GetService<UserController>() ?? throw new InvalidOperationException("UserController not available");
        UpdateStatusFilterVisuals();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadUsersAsync();
        HandlePendingAction();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("action", out var action))
        {
            _pendingAction = action?.ToString()?.ToLowerInvariant();
        }
    }

    private async Task LoadUsersAsync()
    {
        try
        {
            var users = await _userController.GetAllUsersAsync();
            _allUsers.Clear();
            foreach (var user in users)
            {
                _allUsers.Add(user);
            }

            ApplyFilters();
            UsersCollectionView.ItemsSource = _filteredUsers;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load users: {ex.Message}", "OK");
        }
    }

    private void OnStatusFilterTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not string parameter)
            return;

        var status = User.NormalizeStatus(parameter);

        if (status == User.StatusActive)
        {
            _showActive = !_showActive;
            if (!_showActive && !_showInactive)
            {
                _showActive = true; // ensure at least one filter stays enabled
            }
        }
        else if (status == User.StatusInactive)
        {
            _showInactive = !_showInactive;
            if (!_showInactive && !_showActive)
            {
                _showInactive = true;
            }
        }

        UpdateStatusFilterVisuals();
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        _filteredUsers.Clear();

        foreach (var user in _allUsers)
        {
            if (!ShouldIncludeUser(user))
                continue;

            _filteredUsers.Add(user);
        }
    }

    private bool ShouldIncludeUser(User user)
    {
        var normalizedStatus = User.NormalizeStatus(user.Status);
        if (normalizedStatus == User.StatusArchived)
        {
            return false; // archived users hidden from standard lists
        }

        bool matchesStatus = (normalizedStatus == User.StatusActive && _showActive) ||
                             (normalizedStatus == User.StatusInactive && _showInactive);

        if (!matchesStatus)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(_currentSearchText))
        {
            return true;
        }

        return user.DisplayName.Contains(_currentSearchText, StringComparison.OrdinalIgnoreCase) ||
               user.Email.Contains(_currentSearchText, StringComparison.OrdinalIgnoreCase);
    }

    private void UpdateStatusFilterVisuals()
    {
        SetFilterVisual(ActiveFilterButton, ActiveFilterLabel, _showActive);
        SetFilterVisual(InactiveFilterButton, InactiveFilterLabel, _showInactive);
    }

    private static void SetFilterVisual(Border border, Label label, bool isSelected)
    {
        if (border is null || label is null)
            return;

        if (isSelected)
        {
            border.BackgroundColor = Color.FromArgb("#005BA5");
            border.StrokeThickness = 0;
            label.TextColor = Colors.White;
            label.FontAttributes = FontAttributes.Bold;
        }
        else
        {
            border.BackgroundColor = Colors.White;
            border.StrokeThickness = 1;
            border.Stroke = Color.FromArgb("#D1D5DB");
            label.TextColor = Color.FromArgb("#374151");
            label.FontAttributes = FontAttributes.None;
        }
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        _currentSearchText = e.NewTextValue?.Trim() ?? string.Empty;
        ApplyFilters();
    }

    private async void OnStatusChipTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not User user)
            return;

        var currentStatus = User.NormalizeStatus(user.Status);
        string? targetStatus = currentStatus switch
        {
            User.StatusActive => User.StatusInactive,
            User.StatusInactive => User.StatusActive,
            _ => null
        };

        if (targetStatus is null)
        {
            await DisplayAlert("Unavailable", "This status cannot be changed from here.", "OK");
            return;
        }

        string actionText = targetStatus == User.StatusActive ? "activate" : "mark as inactive";
        bool confirm = await DisplayAlert("Change Status",
            $"Do you want to {actionText} {user.DisplayName}?",
            "Yes", "No");

        if (!confirm)
            return;

        if (targetStatus == User.StatusActive)
        {
            user.ArchiveReason = null;
        }

        try
        {
            user.Status = targetStatus;
            await _userController.UpdateUserAsync(user, user.DisplayName, user.Email, user.Role, user.PhoneNumber, user.Address);
            ApplyFilters();
            await DisplayAlert("Success", $"User status updated to {targetStatus}.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to update status: {ex.Message}", "OK");
        }
    }

    private async void OnDashboardTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//AdminHomePage", animate: false);
    }

    private async void OnAnnouncementsTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("AdminAnnouncementsPage", animate: false);
    }

    private async void OnTicketsTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("AdminTicketsPage", animate: false);
    }

    private async void OnReportsTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("AdminReportsPage", animate: false);
    }

    private void OnAddUserClicked(object? sender, EventArgs e)
    {
        _editingUser = null;
        ClearModalFields();
        ModalHeaderLabel.Text = "Add New User";
        ConfirmButton.Text = "Add User";
        PasswordEntry.IsEnabled = true;
        AddUserModal.IsVisible = true;
    }

    private void HandlePendingAction()
    {
        if (_pendingAction == "new")
        {
            _pendingAction = null;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                OnAddUserClicked(this, EventArgs.Empty);
            });
        }
    }

    private void OnCloseModalTapped(object? sender, EventArgs e)
    {
        AddUserModal.IsVisible = false;
    }

    private void OnModalBackgroundTapped(object? sender, EventArgs e)
    {
        AddUserModal.IsVisible = false;
    }

    private void OnCancelAddUserClicked(object? sender, EventArgs e)
    {
        AddUserModal.IsVisible = false;
        ClearModalFields();
    }

    private async void OnConfirmAddUserClicked(object? sender, EventArgs e)
    {
        // Get form values
        string displayName = DisplayNameEntry.Text?.Trim() ?? "";
        string email = EmailEntry.Text?.Trim() ?? "";
        string password = PasswordEntry.Text ?? "";
        string phone = PhoneEntry.Text?.Trim() ?? "";
        string address = AddressEntry.Text?.Trim() ?? "";
        int roleIndex = RolePicker.SelectedIndex;

        // Parse role
        if (roleIndex < 0)
        {
            await DisplayAlert("Validation Error", "Please select a role.", "OK");
            return;
        }

        var roleText = RolePicker.Items[roleIndex];
        var role = roleText switch
        {
            "Student" => Role.Student,
            "Teacher" => Role.Teacher,
            "Admin" => Role.Admin,
            _ => Role.Student
        };

        try
        {
            if (_editingUser == null)
            {
                // ADD NEW USER
                var (success, message, _) = await _userController.CreateUserAsync(
                    displayName, email, password, role, phone, address);

                if (success)
                {
                    await LoadUsersAsync();
                    AddUserModal.IsVisible = false;
                    ClearModalFields();
                    await DisplayAlert("Success", message, "OK");
                }
                else
                {
                    await DisplayAlert("Validation Error", message, "OK");
                }
            }
            else
            {
                // EDIT EXISTING USER
                var (success, message) = await _userController.UpdateUserAsync(
                    _editingUser, displayName, email, role, phone, address, 
                    string.IsNullOrWhiteSpace(password) ? null : password);

                if (success)
                {
                    await LoadUsersAsync();
                    AddUserModal.IsVisible = false;
                    ClearModalFields();
                    _editingUser = null;
                    await DisplayAlert("Success", message, "OK");
                }
                else
                {
                    await DisplayAlert("Validation Error", message, "OK");
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to save user: {ex.Message}", "OK");
        }
    }

    private void ClearModalFields()
    {
        DisplayNameEntry.Text = "";
        EmailEntry.Text = "";
        PasswordEntry.Text = "";
        PhoneEntry.Text = "";
        AddressEntry.Text = "";
        RolePicker.SelectedIndex = -1;
    }

    private void OnEditUserTapped(object? sender, EventArgs e)
    {
        if (sender is Label label && label.BindingContext is User user)
        {
            _editingUser = user;
            ModalHeaderLabel.Text = "Edit User";
            ConfirmButton.Text = "Update User";
            PasswordEntry.IsEnabled = false;
            PasswordEntry.Placeholder = "Leave empty to keep current password";
            
            DisplayNameEntry.Text = user.DisplayName;
            EmailEntry.Text = user.Email;
            PhoneEntry.Text = user.PhoneNumber ?? "";
            AddressEntry.Text = user.Address ?? "";
            
            var roleIndex = user.Role switch
            {
                Role.Student => 0,
                Role.Teacher => 1,
                Role.Admin => 2,
                _ => 0
            };
            RolePicker.SelectedIndex = roleIndex;
            
            AddUserModal.IsVisible = true;
        }
    }

    private async void OnResetPasswordTapped(object? sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Reset Password", 
            "Are you sure you want to reset this user's password?", 
            "Yes", "No");
        
        if (confirm)
        {
            await DisplayAlert("Success", "Password reset email has been sent.", "OK");
        }
    }

    private async void OnArchiveUserTapped(object? sender, EventArgs e)
    {
        if (sender is not Label label || label.BindingContext is not User user)
            return;

        bool confirm = await DisplayAlert("Archive User", 
            $"Archive {user.DisplayName}? They will no longer be able to sign in, but their data is preserved.", 
            "Archive", "Cancel");

        if (!confirm)
            return;

        string? reason = await DisplayPromptAsync("Archive Reason",
            "Please provide a reason for archiving this user.",
            placeholder: "Reason for archiving",
            maxLength: 200);

        if (reason is null)
            return; // user cancelled prompt

        reason = reason.Trim();
        if (string.IsNullOrWhiteSpace(reason))
        {
            await DisplayAlert("Required", "Please enter a reason for archiving.", "OK");
            return;
        }

        user.ArchiveReason = reason;

        try
        {
            var (success, message) = await _userController.DeleteUserAsync(user, reason);
            if (success)
            {
                await LoadUsersAsync();
                await DisplayAlert("Archived", $"{message}\nReason: {reason}", "OK");
            }
            else
            {
                await DisplayAlert("Error", message, "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to archive user: {ex.Message}", "OK");
        }
    }

    private async void OnAdminProfileTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//AdminProfilePage", animate: false);
    }

    private async void OnLogoutTapped(object? sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");
        if (confirm)
        {
            await Shell.Current.GoToAsync("//MainPage", animate: false);
        }
    }
}
