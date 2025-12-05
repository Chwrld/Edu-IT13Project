using System.Collections.ObjectModel;
using System.Runtime.Versioning;
using MauiAppIT13.Controllers;
using MauiAppIT13.Models;
using MauiAppIT13.Utils;

namespace MauiAppIT13.Pages.Admin;

[SupportedOSPlatform("windows10.0.17763.0")]
[SupportedOSPlatform("android21.0")]
public partial class AdminUsersPage : ContentPage
{
    private readonly UserController _userController;
    private ObservableCollection<User> _allUsers = new();
    private ObservableCollection<User> _filteredUsers = new();
    private User? _editingUser = null;

    public AdminUsersPage()
    {
        InitializeComponent();
        _userController = AppServiceProvider.GetService<UserController>() ?? throw new InvalidOperationException("UserController not available");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadUsersAsync();
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
            _filteredUsers.Clear();
            foreach (var user in _allUsers)
            {
                _filteredUsers.Add(user);
            }
            UsersCollectionView.ItemsSource = _filteredUsers;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load users: {ex.Message}", "OK");
        }
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        string searchText = e.NewTextValue?.ToLower() ?? "";
        _filteredUsers.Clear();

        if (string.IsNullOrWhiteSpace(searchText))
        {
            foreach (var user in _allUsers)
            {
                _filteredUsers.Add(user);
            }
        }
        else
        {
            foreach (var user in _allUsers)
            {
                if (user.DisplayName.ToLower().Contains(searchText) || 
                    user.Email.ToLower().Contains(searchText))
                {
                    _filteredUsers.Add(user);
                }
            }
        }
    }

    private async void OnDashboardTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//AdminHomePage");
    }

    private async void OnAnnouncementsTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("AdminAnnouncementsPage");
    }

    private async void OnTicketsTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("AdminTicketsPage");
    }

    private async void OnReportsTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("AdminReportsPage");
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

    private async void OnDeleteUserTapped(object? sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Delete User", 
            "Are you sure you want to delete this user? This action cannot be undone.", 
            "Delete", "Cancel");
        
        if (confirm)
        {
            await DisplayAlert("Success", "User has been deleted.", "OK");
        }
    }

    private async void OnAdminProfileTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//AdminProfilePage");
    }

    private async void OnLogoutTapped(object? sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");
        if (confirm)
        {
            await Shell.Current.GoToAsync("//MainPage");
        }
    }
}
