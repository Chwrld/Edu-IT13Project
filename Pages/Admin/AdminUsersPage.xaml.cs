namespace MauiAppIT13.Pages.Admin;

public partial class AdminUsersPage : ContentPage
{
    public AdminUsersPage()
    {
        InitializeComponent();
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

    private async void OnSettingsTapped(object? sender, EventArgs e)
    {
        await DisplayAlert("Settings", "System settings interface coming soon", "OK");
    }

    private async void OnAddUserClicked(object? sender, EventArgs e)
    {
        await DisplayAlert("Add User", "Add user functionality will be implemented here.", "OK");
    }

    private async void OnEditUserTapped(object? sender, EventArgs e)
    {
        await DisplayAlert("Edit User", "Edit user functionality will be implemented here.", "OK");
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

    private async void OnLogoutTapped(object? sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");
        if (confirm)
        {
            await Shell.Current.GoToAsync("//AdminLoginPage");
        }
    }
}
