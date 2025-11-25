namespace MauiAppIT13.Pages.Student;

public partial class MessagesPage : ContentPage
    {
        public MessagesPage()
        {
            InitializeComponent();
        }

        private async void OnProfileTapped(object? sender, EventArgs e)
        {
            await Navigation.PushAsync(new ProfilePage());
        }

        private async void OnHomeTapped(object? sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async void OnAnnouncementsTapped(object? sender, EventArgs e)
        {
            await Navigation.PushAsync(new AnnouncementsPage());
        }

        private async void OnTicketsTapped(object? sender, EventArgs e)
        {
            await Navigation.PushAsync(new TicketsPage());
        }

        private async void OnConversation1Tapped(object? sender, EventArgs e)
        {
            // Already showing Dr. Johnson conversation
        }

        private async void OnConversation2Tapped(object? sender, EventArgs e)
        {
            await DisplayAlert("Academic Office", "Loading conversation...", "OK");
        }

        private async void OnConversation3Tapped(object? sender, EventArgs e)
        {
            await DisplayAlert("Prof. Martinez", "Loading conversation...", "OK");
        }

        private async void OnConversation4Tapped(object? sender, EventArgs e)
        {
            await DisplayAlert("Student Services", "Loading conversation...", "OK");
        }

        private async void OnChatMenuTapped(object? sender, EventArgs e)
        {
            await DisplayAlert("Menu", "Chat options - Coming soon!", "OK");
        }

        private async void OnAttachFileTapped(object? sender, EventArgs e)
        {
            await DisplayAlert("Attach", "File attachment - Coming soon!", "OK");
        }

        private async void OnSendMessageTapped(object? sender, EventArgs e)
        {
            string message = MessageEntry.Text?.Trim() ?? string.Empty;
            
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            // TODO: Implement actual message sending logic
            await DisplayAlert("Message", $"Sending: {message}", "OK");
            MessageEntry.Text = string.Empty;
        }

        private async void OnLogoutTapped(object? sender, EventArgs e)
        {
            // Navigate back to login page by popping to root
            await Navigation.PopToRootAsync();
        }
    }
