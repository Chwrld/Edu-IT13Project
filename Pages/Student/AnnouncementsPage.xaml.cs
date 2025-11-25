namespace MauiAppIT13.Pages.Student;

public partial class AnnouncementsPage : ContentPage
    {
        private string currentFilter = "All";

        public AnnouncementsPage()
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

        private async void OnMessagesTapped(object? sender, EventArgs e)
        {
            await Navigation.PushAsync(new MessagesPage());
        }

        private async void OnTicketsTapped(object? sender, EventArgs e)
        {
            await Navigation.PushAsync(new TicketsPage());
        }

        private void OnAllTabTapped(object? sender, EventArgs e)
        {
            currentFilter = "All";
            UpdateTabStyles();
            FilterAnnouncements();
        }

        private void OnAnnouncementsTabTapped(object? sender, EventArgs e)
        {
            currentFilter = "Announcements";
            UpdateTabStyles();
            FilterAnnouncements();
        }

        private void OnRemindersTabTapped(object? sender, EventArgs e)
        {
            currentFilter = "Reminders";
            UpdateTabStyles();
            FilterAnnouncements();
        }

        private void UpdateTabStyles()
        {
            // Reset all tabs to inactive state
            AllTab.BackgroundColor = Colors.Transparent;
            AllTabLabel.TextColor = Color.FromArgb("#0891B2");
            AllTabLabel.FontAttributes = FontAttributes.None;

            AnnouncementsTab.BackgroundColor = Colors.Transparent;
            AnnouncementsTabLabel.TextColor = Color.FromArgb("#0891B2");
            AnnouncementsTabLabel.FontAttributes = FontAttributes.None;

            RemindersTab.BackgroundColor = Colors.Transparent;
            RemindersTabLabel.TextColor = Color.FromArgb("#0891B2");
            RemindersTabLabel.FontAttributes = FontAttributes.None;

            // Set active tab
            switch (currentFilter)
            {
                case "All":
                    AllTab.BackgroundColor = Color.FromArgb("#0891B2");
                    AllTabLabel.TextColor = Colors.White;
                    AllTabLabel.FontAttributes = FontAttributes.Bold;
                    break;
                case "Announcements":
                    AnnouncementsTab.BackgroundColor = Color.FromArgb("#0891B2");
                    AnnouncementsTabLabel.TextColor = Colors.White;
                    AnnouncementsTabLabel.FontAttributes = FontAttributes.Bold;
                    break;
                case "Reminders":
                    RemindersTab.BackgroundColor = Color.FromArgb("#0891B2");
                    RemindersTabLabel.TextColor = Colors.White;
                    RemindersTabLabel.FontAttributes = FontAttributes.Bold;
                    break;
            }
        }

        private void FilterAnnouncements()
        {
            // Show/hide items based on filter
            switch (currentFilter)
            {
                case "All":
                    // Show all items
                    Reminder1.IsVisible = true;
                    Announcement1.IsVisible = true;
                    Announcement2.IsVisible = true;
                    Reminder2.IsVisible = true;
                    break;

                case "Announcements":
                    // Show only announcements
                    Reminder1.IsVisible = false;
                    Announcement1.IsVisible = true;
                    Announcement2.IsVisible = true;
                    Reminder2.IsVisible = false;
                    break;

                case "Reminders":
                    // Show only reminders
                    Reminder1.IsVisible = true;
                    Announcement1.IsVisible = false;
                    Announcement2.IsVisible = false;
                    Reminder2.IsVisible = true;
                    break;
            }
        }

        private async void OnLogoutTapped(object? sender, EventArgs e)
        {
            // Navigate back to login page by popping to root
            await Navigation.PopToRootAsync();
        }
    }
