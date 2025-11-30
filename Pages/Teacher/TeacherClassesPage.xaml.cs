using System.Runtime.Versioning;

namespace MauiAppIT13.Pages.Teacher;

[SupportedOSPlatform("windows10.0.17763.0")]
public partial class TeacherClassesPage : ContentPage
{
    public TeacherClassesPage()
    {
        InitializeComponent();
    }

    private async void OnDashboardTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//TeacherHomePage");
    }

    private async void OnMessagesTapped(object sender, EventArgs e)
    {
        await DisplayAlert("Messages", "Messages feature coming soon!", "OK");
    }

    private async void OnViewClassClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//TeacherClassDetailsPage");
    }

    private async void OnLogoutTapped(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");
        if (confirm)
        {
            await Shell.Current.GoToAsync("//TeacherLoginPage");
        }
    }
}
