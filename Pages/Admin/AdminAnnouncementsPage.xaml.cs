namespace MauiAppIT13.Pages.Admin;

public partial class AdminAnnouncementsPage : ContentPage
{
    public AdminAnnouncementsPage()
    {
        InitializeComponent();
    }

    private async void OnDashboardTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//AdminHomePage");
    }

    private async void OnUsersTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("AdminUsersPage");
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

    private async void OnLogoutTapped(object? sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");
        if (confirm)
        {
            await Shell.Current.GoToAsync("//AdminLoginPage");
        }
    }

    private async void OnNewAnnouncementClicked(object? sender, EventArgs e)
    {
        await DisplayAlert("New Announcement", "Announcement creation form will be implemented here.", "OK");
    }

    private async void OnEditAnnouncementClicked(object? sender, EventArgs e)
    {
        await DisplayAlert("Edit Announcement", "Edit announcement functionality will be implemented here.", "OK");
    }

    private async void OnDeleteAnnouncementClicked(object? sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Delete Announcement", 
            "Are you sure you want to delete this announcement?", 
            "Delete", "Cancel");
        
        if (confirm)
        {
            await DisplayAlert("Success", "Announcement has been deleted.", "OK");
        }
    }
}
