using System.Runtime.Versioning;
using MauiAppIT13.Models;
using MauiAppIT13.Utils;

namespace MauiAppIT13.Pages.Teacher;

[SupportedOSPlatform("windows10.0.17763.0")]
[SupportedOSPlatform("android21.0")]
public partial class TeacherProfilePage : ContentPage
{
    private readonly AuthManager _authManager;

    public TeacherProfilePage()
    {
        InitializeComponent();
        _authManager = AppServiceProvider.GetService<AuthManager>() ?? new AuthManager();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadTeacherProfile();
    }

    private void LoadTeacherProfile()
    {
        try
        {
            var currentUser = _authManager.CurrentUser;
            if (currentUser != null)
            {
                NameLabel.Text = currentUser.DisplayName ?? "Faculty Name";
                EmailLabel.Text = currentUser.Email ?? "teacher@university.edu";
                PhoneLabel.Text = currentUser.PhoneNumber ?? "+1 (555) 000-0000";
                OfficeLabel.Text = currentUser.Address ?? "Faculty Building, Room 305";
                
                DepartmentLabel.Text = "Computer Science Department";
                DepartmentDetailLabel.Text = "Computer Science";
                EmployeeIdLabel.Text = "FAC-2020-0123";
                PositionLabel.Text = "Assistant Professor";
                RankLabel.Text = "Assistant Professor";
                SpecializationLabel.Text = "Software Engineering, Artificial Intelligence";

                var names = (currentUser.DisplayName ?? "TF").Split(' ');
                string initials = names.Length > 1 
                    ? $"{names[0][0]}{names[names.Length - 1][0]}" 
                    : names[0].Length > 0 ? names[0].Substring(0, Math.Min(2, names[0].Length)) : "TF";
                AvatarLabel.Text = initials.ToUpper();

                System.Diagnostics.Debug.WriteLine($"TeacherProfilePage: Loaded profile for {currentUser.DisplayName}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TeacherProfilePage: Error loading profile - {ex.Message}");
        }
    }

    private async void OnDashboardTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//TeacherHomePage");
    }

    private async void OnClassesTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//TeacherClassesPage");
    }

    private async void OnMessagesTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//TeacherMessagesPage");
    }

    private async void OnAnnouncementsTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//TeacherAnnouncementsPage");
    }

    private async void OnTicketsTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//TeacherTicketsPage");
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
            System.Diagnostics.Debug.WriteLine($"TeacherProfilePage: Error opening edit modal - {ex.Message}");
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
            LoadTeacherProfile();

            await DisplayAlert("Success", "Profile updated successfully!", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to save profile: {ex.Message}", "OK");
            System.Diagnostics.Debug.WriteLine($"TeacherProfilePage: Error saving profile - {ex.Message}");
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
            System.Diagnostics.Debug.WriteLine($"TeacherProfilePage: User profile updated in database");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TeacherProfilePage: Error updating database - {ex.Message}");
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
