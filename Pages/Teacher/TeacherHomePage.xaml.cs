namespace MauiAppIT13.Pages.Teacher;

public partial class TeacherHomePage : ContentPage
{
    public TeacherHomePage()
    {
        InitializeComponent();
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
