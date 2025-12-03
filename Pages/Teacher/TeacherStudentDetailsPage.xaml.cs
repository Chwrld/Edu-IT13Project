using System.Runtime.Versioning;

namespace MauiAppIT13.Pages.Teacher;

[SupportedOSPlatform("windows10.0.17763.0")]
public partial class TeacherStudentDetailsPage : ContentPage
{
    public TeacherStudentDetailsPage()
    {
        InitializeComponent();
    }

    private async void OnBackToClassTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//TeacherClassDetailsPage");
    }

    private async void OnDashboardTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//TeacherHomePage");
    }

    private async void OnMessagesTapped(object sender, EventArgs e)
    {
        await DisplayAlert("Messages", "Messages feature coming soon!", "OK");
    }

    private async void OnMessageStudentClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Message Student", "Message feature coming soon!", "OK");
    }

    private async void OnViewConcernsClicked(object sender, EventArgs e)
    {
        await DisplayAlert("View Concerns", "This will show all concerns submitted by this student for this class.", "OK");
    }

    private async void OnViewGradesClicked(object sender, EventArgs e)
    {
        await DisplayAlert("View Grades", "This will navigate to the grades page with this student's details.", "OK");
    }

    private async void OnSaveNotesClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Save Notes", "Notes saved successfully!", "OK");
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
