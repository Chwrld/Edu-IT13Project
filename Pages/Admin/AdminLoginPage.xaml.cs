namespace MauiAppIT13.Pages.Admin;

public partial class AdminLoginPage : ContentPage
{
    public AdminLoginPage()
    {
        InitializeComponent();
    }

    private async void OnAdminSignInClicked(object sender, EventArgs e)
    {
        string email = AdminEmailEntry.Text?.Trim() ?? "";
        string password = AdminPasswordEntry.Text ?? "";

        // Simple validation for demo - in production, use proper authentication
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlert("Error", "Please enter both email and password", "OK");
            return;
        }

        // Demo admin credentials check
        if (email.ToLower() == "admin@university.edu" && password == "admin123")
        {
            // Navigate to admin home page
            await Shell.Current.GoToAsync("//AdminHomePage");
        }
        else
        {
            await DisplayAlert("Error", "Invalid admin credentials", "OK");
        }
    }

    private async void OnBackToStudentLoginTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//MainPage");
    }
}
