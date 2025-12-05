using System.Runtime.Versioning;
using MauiAppIT13.Models;
using MauiAppIT13.Utils;

namespace MauiAppIT13.Pages.Admin;

[SupportedOSPlatform("windows10.0.17763.0")]
[SupportedOSPlatform("android21.0")]
public partial class AdminProfilePage : ContentPage
{
    private readonly AuthManager _authManager;

    public AdminProfilePage()
    {
        InitializeComponent();
        _authManager = AppServiceProvider.GetService<AuthManager>() ?? new AuthManager();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadAdminProfile();
    }

    private void LoadAdminProfile()
    {
        try
        {
            var currentUser = _authManager.CurrentUser;
            if (currentUser != null)
            {
                NameLabel.Text = currentUser.DisplayName ?? "Administrator Name";
                EmailLabel.Text = currentUser.Email ?? "admin@university.edu";
                PhoneLabel.Text = currentUser.PhoneNumber ?? "+1 (555) 000-0000";
                OfficeLabel.Text = currentUser.Address ?? "Administration Building, Room 101";
                
                RoleLabel.Text = "System Administrator";
                RoleDetailLabel.Text = "System Administrator";
                EmployeeIdLabel.Text = "ADM-2019-0001";
                AccessLevelLabel.Text = "Full System Access";
                DepartmentLabel.Text = "Information Technology";

                var names = (currentUser.DisplayName ?? "AD").Split(' ');
                string initials = names.Length > 1 
                    ? $"{names[0][0]}{names[names.Length - 1][0]}" 
                    : names[0].Length > 0 ? names[0].Substring(0, Math.Min(2, names[0].Length)) : "AD";
                AvatarLabel.Text = initials.ToUpper();

                System.Diagnostics.Debug.WriteLine($"AdminProfilePage: Loaded profile for {currentUser.DisplayName}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AdminProfilePage: Error loading profile - {ex.Message}");
        }
    }

    private async void OnDashboardTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//AdminHomePage");
    }

    private async void OnUsersTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//AdminUsersPage");
    }

    private async void OnAnnouncementsTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//AdminAnnouncementsPage");
    }

    private async void OnTicketsTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//AdminTicketsPage");
    }

    private async void OnReportsTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//AdminReportsPage");
    }

    private void OnEditProfileClicked(object? sender, EventArgs e)
    {
        try
        {
            var currentUser = _authManager.CurrentUser;
            if (currentUser != null)
            {
                EditPhoneEntry.Text = currentUser.PhoneNumber ?? string.Empty;
                EditOfficeEntry.Text = currentUser.Address ?? string.Empty;
                EditProfileModal.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AdminProfilePage: Error opening edit modal - {ex.Message}");
        }
    }

    private void OnEditProfileModalBackgroundTapped(object? sender, EventArgs e)
    {
        EditProfileModal.IsVisible = false;
    }

    private void OnCloseEditProfileModalTapped(object? sender, EventArgs e)
    {
        EditProfileModal.IsVisible = false;
    }

    private void OnCancelEditProfileClicked(object? sender, EventArgs e)
    {
        EditProfileModal.IsVisible = false;
    }

    private async void OnSaveEditProfileClicked(object? sender, EventArgs e)
    {
        try
        {
            var currentUser = _authManager.CurrentUser;
            if (currentUser == null)
            {
                await DisplayAlert("Error", "No user logged in", "OK");
                return;
            }

            var phone = EditPhoneEntry.Text?.Trim();
            var office = EditOfficeEntry.Text?.Trim();

            if (string.IsNullOrEmpty(phone) && string.IsNullOrEmpty(office))
            {
                await DisplayAlert("Info", "No changes to save", "OK");
                return;
            }

            currentUser.PhoneNumber = phone;
            currentUser.Address = office;

            await UpdateUserInDatabase(currentUser);

            EditProfileModal.IsVisible = false;
            LoadAdminProfile();

            await DisplayAlert("Success", "Profile updated successfully!", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to save profile: {ex.Message}", "OK");
            System.Diagnostics.Debug.WriteLine($"AdminProfilePage: Error saving profile - {ex.Message}");
        }
    }

    private async Task UpdateUserInDatabase(User user)
    {
        try
        {
            const string sql = @"
                UPDATE users 
                SET phone_number = @PhoneNumber, 
                    address = @Address
                WHERE user_id = @UserId";

            const string connectionString = "Data Source=LAPTOP-L1R9L9R3\\SQLEXPRESS01;Initial Catalog=EduCRM;Integrated Security=True;Connect Timeout=10;Encrypt=False;Trust Server Certificate=True;";
            
            await using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
            await connection.OpenAsync();
            
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("@UserId", user.Id);
            command.Parameters.AddWithValue("@PhoneNumber", user.PhoneNumber ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Address", user.Address ?? (object)DBNull.Value);

            await command.ExecuteNonQueryAsync();
            System.Diagnostics.Debug.WriteLine($"AdminProfilePage: User profile updated in database");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AdminProfilePage: Error updating database - {ex.Message}");
            throw;
        }
    }

    private async void OnLogoutTapped(object? sender, EventArgs e)
    {
        var confirm = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");
        if (confirm)
        {
            _authManager.ClearAuthentication();
            await Shell.Current.GoToAsync("//MainPage");
        }
    }
}
