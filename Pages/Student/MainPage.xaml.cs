using MauiAppIT13.Controllers;
using MauiAppIT13.Models;
using MauiAppIT13.Utils;

#pragma warning disable CA1416 // MAUI primitives are available on supported targets configured for the app

namespace MauiAppIT13.Pages.Student
{
    public partial class MainPage : ContentPage
    {
        private AuthController? _authController;

        public MainPage()
        {
            InitializeComponent();
            _authController = AppServiceProvider.GetService<AuthController>();
        }

        private async void OnSignInClicked(object? sender, EventArgs e)
        {
            string email = EmailEntry.Text?.Trim() ?? string.Empty;
            string password = PasswordEntry.Text ?? string.Empty;

            if (_authController is null)
            {
                await DisplayAlert("Error", "Authentication service unavailable.", "OK");
                return;
            }

            var result = await _authController.LoginAsync(email, password);
            if (!result.Success || result.User is null)
            {
                await DisplayAlert("Login Failed", result.ErrorMessage ?? "Invalid credentials.", "OK");
                return;
            }

            string targetRoute = result.User.Role switch
            {
                Role.Admin => "//AdminHomePage",
                Role.Teacher => "//TeacherHomePage",
                _ => "//HomePage"
            };

            await Shell.Current.GoToAsync(targetRoute);
        }
    }
}

#pragma warning restore CA1416
