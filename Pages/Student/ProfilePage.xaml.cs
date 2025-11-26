namespace MauiAppIT13.Pages.Student;

public partial class ProfilePage : ContentPage
    {
        public ProfilePage()
        {
            InitializeComponent();
        }

        private async void OnHomeTapped(object? sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async void OnMessagesTapped(object? sender, EventArgs e)
        {
            await Navigation.PushAsync(new MessagesPage());
        }

        private async void OnAnnouncementsTapped(object? sender, EventArgs e)
        {
            await Navigation.PushAsync(new AnnouncementsPage());
        }

        private async void OnTicketsTapped(object? sender, EventArgs e)
        {
            await Navigation.PushAsync(new TicketsPage());
        }

        private async void OnEditProfileClicked(object? sender, EventArgs e)
        {
            await DisplayAlert("Edit Profile", "Profile editing functionality - Coming soon!", "OK");
        }

    private async void OnLogoutTapped(object? sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");
        if (confirm)
        {
            await Shell.Current.GoToAsync("//MainPage");
        }
    }
}