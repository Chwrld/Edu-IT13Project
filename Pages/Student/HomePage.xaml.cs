namespace MauiAppIT13.Pages.Student;

public partial class HomePage : ContentPage
    {
        public HomePage()
        {
            InitializeComponent();
        }

        private async void OnProfileTapped(object? sender, EventArgs e)
        {
            await Navigation.PushAsync(new ProfilePage());
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

        private async void OnViewAllMessagesTapped(object? sender, EventArgs e)
        {
            await Navigation.PushAsync(new MessagesPage());
        }

        private async void OnLogoutTapped(object? sender, EventArgs e)
        {
            // Navigate back to login page by popping to root
            await Navigation.PopToRootAsync();
        }
    }
