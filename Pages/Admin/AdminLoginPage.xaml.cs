using MauiAppIT13.Controllers;
using MauiAppIT13.Utils;

namespace MauiAppIT13.Pages.Admin;

public partial class AdminLoginPage : ContentPage
{
    private AuthController? _authController;

    public AdminLoginPage()
    {
        InitializeComponent();
        _authController = AppServiceProvider.GetService<AuthController>();
    }

    private async void OnAdminSignInClicked(object sender, EventArgs e)
    {
        string email = AdminEmailEntry.Text?.Trim() ?? "";
        string password = AdminPasswordEntry.Text ?? "";

        if (_authController is null)
        {
            await DisplayAlert("Error", "Authentication service unavailable.", "OK");
            return;
        }

        var result = await _authController.LoginAsync(email, password);
        if (result.Success && result.User?.Role == Models.Role.Admin)
        {
            // Navigate to admin home page
            await Shell.Current.GoToAsync("//AdminHomePage");
        }
        else
        {
            await DisplayAlert("Error", result.ErrorMessage ?? "Invalid admin credentials", "OK");
        }
    }

    private async void OnBackToStudentLoginTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//MainPage");
    }
}
