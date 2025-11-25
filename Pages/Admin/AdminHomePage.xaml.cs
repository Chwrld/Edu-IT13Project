namespace MauiAppIT13.Pages.Admin;

public partial class AdminHomePage : ContentPage
{
    public AdminHomePage()
    {
        InitializeComponent();
    }

    private async void OnAdminProfileTapped(object sender, EventArgs e)
    {
        await DisplayAlert("Admin Profile", "Admin profile settings coming soon", "OK");
    }

    private async void OnUsersTapped(object sender, EventArgs e)
    {
        await DisplayAlert("Users", "User management interface coming soon", "OK");
    }

    private async void OnAnnouncementsTapped(object sender, EventArgs e)
    {
        await DisplayAlert("Announcements", "Announcements management interface coming soon", "OK");
    }

    private async void OnTicketsTapped(object sender, EventArgs e)
    {
        await DisplayAlert("Tickets", "Ticket management interface coming soon", "OK");
    }

    private async void OnReportsTapped(object sender, EventArgs e)
    {
        await DisplayAlert("Reports", "Reports interface coming soon", "OK");
    }

    private async void OnSettingsTapped(object sender, EventArgs e)
    {
        await DisplayAlert("Settings", "System settings interface coming soon", "OK");
    }

    private async void OnLogoutTapped(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");
        if (confirm)
        {
            await Shell.Current.GoToAsync("//AdminLoginPage");
        }
    }

    private async void OnCreateAnnouncementClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Create Announcement", "Announcement creation form coming soon", "OK");
    }

    private async void OnAddUserClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Add User", "User creation form coming soon", "OK");
    }

    private async void OnViewReportsClicked(object sender, EventArgs e)
    {
        await DisplayAlert("View Reports", "Reports viewer coming soon", "OK");
    }

    private async void OnSystemSettingsClicked(object sender, EventArgs e)
    {
        await DisplayAlert("System Settings", "System configuration interface coming soon", "OK");
    }

    private async void OnDownloadDataClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Download Data", "Data export functionality coming soon", "OK");
    }
}
