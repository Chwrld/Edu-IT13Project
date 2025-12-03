using System.Runtime.Versioning;

namespace MauiAppIT13.Pages.Teacher;

[SupportedOSPlatform("windows10.0.17763.0")]
public partial class TeacherHomePage : ContentPage
{
    public TeacherHomePage()
    {
        InitializeComponent();
    }

    private async void OnClassesTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//TeacherClassesPage");
    }

    private async void OnMessagesTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//TeacherMessagesPage");
    }

    private async void OnLogoutTapped(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");
        if (confirm)
        {
            await Shell.Current.GoToAsync("//MainPage");
        }
    }
}
