using System.Runtime.Versioning;

namespace MauiAppIT13.Pages.Admin;

[SupportedOSPlatform("windows10.0.17763.0")]
[SupportedOSPlatform("android21.0")]
public partial class AdminHomePage : ContentPage
{
    public AdminHomePage()
    {
        InitializeComponent();
    }

    private async void OnAdminProfileTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//AdminProfilePage");
    }

    private async void OnUsersTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("AdminUsersPage");
    }

    private async void OnAnnouncementsTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("AdminAnnouncementsPage");
    }

    private async void OnTicketsTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("AdminTicketsPage");
    }

    private async void OnReportsTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("AdminReportsPage");
    }

    private async void OnLogoutTapped(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");
        if (confirm)
        {
            await Shell.Current.GoToAsync("//MainPage");
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

    private async void OnDownloadDataClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Download Data", "Data export functionality coming soon", "OK");
    }
}
