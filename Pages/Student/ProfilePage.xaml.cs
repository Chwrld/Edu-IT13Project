using MauiAppIT13.Models;
using MauiAppIT13.Services;
using MauiAppIT13.Utils;

namespace MauiAppIT13.Pages.Student;

public partial class ProfilePage : ContentPage
{
    private readonly AuthManager _authManager;
    private readonly StudentService _studentService;

    public ProfilePage()
    {
        InitializeComponent();
        _authManager = AppServiceProvider.GetService<AuthManager>() ?? new AuthManager();
        _studentService = AppServiceProvider.GetService<StudentService>() ?? new StudentService();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadStudentProfile();
    }

    private async void LoadStudentProfile()
    {
        try
        {
            var currentUser = _authManager.CurrentUser;
            if (currentUser != null)
            {
                // Update UI with user data
                NameLabel.Text = currentUser.DisplayName ?? "Student Name";
                EmailLabel.Text = currentUser.Email ?? "student@university.edu";
                PhoneLabel.Text = currentUser.PhoneNumber ?? "+1 (555) 000-0000";
                AddressLabel.Text = currentUser.Address ?? "Campus Location";
                ProgramLabel.Text = "Computer Science";

                // Generate avatar initials
                var names = (currentUser.DisplayName ?? "SJ").Split(' ');
                string initials = names.Length > 1 
                    ? $"{names[0][0]}{names[names.Length - 1][0]}" 
                    : names[0].Length > 0 ? names[0].Substring(0, Math.Min(2, names[0].Length)) : "SJ";
                AvatarLabel.Text = initials.ToUpper();

                // Load student academic details
                var student = await _studentService.GetStudentByUserIdAsync(currentUser.Id);
                if (student != null)
                {
                    StudentIdLabel.Text = student.StudentNumber ?? "STU-0000-0000";
                    ProgramDetailLabel.Text = student.Program ?? "Computer Science";
                    YearLevelLabel.Text = student.YearLevel ?? "Year 3";
                    GPALabel.Text = student.GPA.HasValue ? $"{student.GPA:F2} / 4.0" : "3.85 / 4.0";

                    System.Diagnostics.Debug.WriteLine($"ProfilePage: Loaded student details for {student.StudentNumber}");

                    // Load student achievements
                    var achievements = await _studentService.GetStudentAchievementsAsync(currentUser.Id);
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        AchievementsCollectionView.ItemsSource = achievements;
                        System.Diagnostics.Debug.WriteLine($"ProfilePage: Loaded {achievements.Count} achievements");
                    });
                }

                System.Diagnostics.Debug.WriteLine($"ProfilePage: Loaded profile for {currentUser.DisplayName}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ProfilePage: Error loading profile - {ex.Message}");
        }
    }

    private async void OnHomeTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//HomePage");
    }

    private async void OnMessagesTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//MessagesPage");
    }

    private async void OnAnnouncementsTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//AnnouncementsPage");
    }

    private async void OnTicketsTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//TicketsPage");
    }

    private void OnEditProfileClicked(object? sender, EventArgs e)
    {
        try
        {
            var currentUser = _authManager.CurrentUser;
            if (currentUser != null)
            {
                // Populate modal with current data
                EditPhoneEntry.Text = currentUser.PhoneNumber ?? string.Empty;
                EditAddressEditor.Text = currentUser.Address ?? string.Empty;
                
                // Show modal
                EditProfileModal.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ProfilePage: Error opening edit modal - {ex.Message}");
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

            // Validate inputs
            var phone = EditPhoneEntry.Text?.Trim();
            var address = EditAddressEditor.Text?.Trim();

            if (string.IsNullOrEmpty(phone) && string.IsNullOrEmpty(address))
            {
                await DisplayAlert("Info", "No changes to save", "OK");
                return;
            }

            // Update user object
            currentUser.PhoneNumber = phone;
            currentUser.Address = address;

            // Update in database
            await UpdateUserInDatabase(currentUser);

            // Close modal and refresh profile
            EditProfileModal.IsVisible = false;
            LoadStudentProfile();

            await DisplayAlert("Success", "Profile updated successfully!", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to save profile: {ex.Message}", "OK");
            System.Diagnostics.Debug.WriteLine($"ProfilePage: Error saving profile - {ex.Message}");
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
            System.Diagnostics.Debug.WriteLine($"ProfilePage: User profile updated in database");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ProfilePage: Error updating database - {ex.Message}");
            throw;
        }
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