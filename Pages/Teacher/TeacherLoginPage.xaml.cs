namespace MauiAppIT13.Pages.Teacher;

public partial class TeacherLoginPage : ContentPage
{
    public TeacherLoginPage()
    {
        InitializeComponent();
    }

    private async void OnTeacherSignInClicked(object sender, EventArgs e)
    {
        string email = TeacherEmailEntry.Text?.Trim() ?? "";
        string password = TeacherPasswordEntry.Text ?? "";

        // Simple validation for demo
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlert("Error", "Please enter both email and password", "OK");
            return;
        }

        // Demo teacher credentials check
        if (email.ToLower() == "teacher@university.edu" && password == "teacher123")
        {
            // Navigate to teacher home page
            await Shell.Current.GoToAsync("//TeacherHomePage");
        }
        else
        {
            await DisplayAlert("Error", "Invalid teacher credentials", "OK");
        }
    }

    private async void OnBackToStudentLoginTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//MainPage");
    }
}
