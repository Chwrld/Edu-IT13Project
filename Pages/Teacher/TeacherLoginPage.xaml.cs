using MauiAppIT13.Controllers;
using MauiAppIT13.Models;
using MauiAppIT13.Utils;

namespace MauiAppIT13.Pages.Teacher;

public partial class TeacherLoginPage : ContentPage
{
    private AuthController? _authController;

    public TeacherLoginPage()
    {
        InitializeComponent();
        _authController = AppServiceProvider.GetService<AuthController>();
    }

    private async void OnTeacherSignInClicked(object sender, EventArgs e)
    {
        string email = TeacherEmailEntry.Text?.Trim() ?? "";
        string password = TeacherPasswordEntry.Text ?? "";

        if (_authController is null)
        {
            await DisplayAlert("Error", "Authentication service unavailable.", "OK");
            return;
        }

        var result = await _authController.LoginAsync(email, password);
        if (result.Success && result.User?.Role == Role.Teacher)
        {
            // Navigate to teacher home page
            await Shell.Current.GoToAsync("//TeacherHomePage");
        }
        else
        {
            await DisplayAlert("Error", result.ErrorMessage ?? "Invalid teacher credentials", "OK");
        }
    }

    private async void OnBackToStudentLoginTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//MainPage");
    }
}
