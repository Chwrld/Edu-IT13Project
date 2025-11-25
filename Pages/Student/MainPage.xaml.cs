namespace MauiAppIT13.Pages.Student
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void OnSignInTabTapped(object? sender, EventArgs e)
        {
            // Update tab styling for Sign In (active)
            SignInTab.FontAttributes = FontAttributes.Bold;
            SignInTab.TextColor = Color.FromArgb("#2C3E50");
            
            SignUpTab.FontAttributes = FontAttributes.None;
            SignUpTab.TextColor = Color.FromArgb("#95A5A6");

            // Update tab background colors
            if (SignInTab.Parent?.Parent is Border signInBorder)
                signInBorder.BackgroundColor = Colors.White;
            
            if (SignUpTab.Parent?.Parent is Border signUpBorder)
                signUpBorder.BackgroundColor = Color.FromArgb("#F8F9FA");
        }

        private void OnSignUpTabTapped(object? sender, EventArgs e)
        {
            // Update tab styling for Sign Up (active)
            SignUpTab.FontAttributes = FontAttributes.Bold;
            SignUpTab.TextColor = Color.FromArgb("#2C3E50");
            
            SignInTab.FontAttributes = FontAttributes.None;
            SignInTab.TextColor = Color.FromArgb("#95A5A6");

            // Update tab background colors
            if (SignUpTab.Parent?.Parent is Border signUpBorder)
                signUpBorder.BackgroundColor = Colors.White;
            
            if (SignInTab.Parent?.Parent is Border signInBorder)
                signInBorder.BackgroundColor = Color.FromArgb("#F8F9FA");
        }

        private async void OnSignInClicked(object? sender, EventArgs e)
        {
            string email = EmailEntry.Text?.Trim() ?? string.Empty;
            string password = PasswordEntry.Text ?? string.Empty;

            // Basic validation
            if (string.IsNullOrEmpty(email))
            {
                await DisplayAlert("Error", "Please enter your email address.", "OK");
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                await DisplayAlert("Error", "Please enter your password.", "OK");
                return;
            }

            // TODO: Implement actual authentication logic here
            // For now, navigate to HomePage on successful login
            await Shell.Current.GoToAsync("//HomePage");
        }

        private async void OnAdminLoginTapped(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//AdminLoginPage");
        }

        private async void OnTeacherLoginTapped(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//TeacherLoginPage");
        }
    }
}
