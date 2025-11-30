using System.Runtime.Versioning;

namespace MauiAppIT13.Pages.Teacher;

[SupportedOSPlatform("windows10.0.17763.0")]
[SupportedOSPlatform("android21.0")]
public partial class TeacherMessagesPage : ContentPage
{
    public TeacherMessagesPage()
    {
        InitializeComponent();
    }

    private async void OnDashboardTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//TeacherHomePage");
    }

    private async void OnClassesTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//TeacherClassesPage");
    }

    private async void OnNewMessageClicked(object sender, EventArgs e)
    {
        await DisplayAlert("New Message", "Compose new message feature coming soon!", "OK");
    }

    private void OnAllTabTapped(object sender, EventArgs e)
    {
        // Update tab styles
        AllTab.BackgroundColor = Color.FromArgb("#059669");
        if (AllTab.Content is Label allLabel)
        {
            allLabel.TextColor = Colors.White;
            allLabel.FontAttributes = FontAttributes.Bold;
        }

        StudentsTab.BackgroundColor = Colors.Transparent;
        if (StudentsTab.Content is Label studentsLabel)
        {
            studentsLabel.TextColor = Color.FromArgb("#059669");
            studentsLabel.FontAttributes = FontAttributes.None;
        }

        StaffTab.BackgroundColor = Colors.Transparent;
        if (StaffTab.Content is Label staffLabel)
        {
            staffLabel.TextColor = Color.FromArgb("#059669");
            staffLabel.FontAttributes = FontAttributes.None;
        }

        // Filter conversations (to be implemented)
    }

    private void OnStudentsTabTapped(object sender, EventArgs e)
    {
        // Update tab styles
        StudentsTab.BackgroundColor = Color.FromArgb("#059669");
        if (StudentsTab.Content is Label studentsLabel)
        {
            studentsLabel.TextColor = Colors.White;
            studentsLabel.FontAttributes = FontAttributes.Bold;
        }

        AllTab.BackgroundColor = Colors.Transparent;
        if (AllTab.Content is Label allLabel)
        {
            allLabel.TextColor = Color.FromArgb("#059669");
            allLabel.FontAttributes = FontAttributes.None;
        }

        StaffTab.BackgroundColor = Colors.Transparent;
        if (StaffTab.Content is Label staffLabel)
        {
            staffLabel.TextColor = Color.FromArgb("#059669");
            staffLabel.FontAttributes = FontAttributes.None;
        }

        // Filter to show only student conversations
    }

    private void OnStaffTabTapped(object sender, EventArgs e)
    {
        // Update tab styles
        StaffTab.BackgroundColor = Color.FromArgb("#059669");
        if (StaffTab.Content is Label staffLabel)
        {
            staffLabel.TextColor = Colors.White;
            staffLabel.FontAttributes = FontAttributes.Bold;
        }

        AllTab.BackgroundColor = Colors.Transparent;
        if (AllTab.Content is Label allLabel)
        {
            allLabel.TextColor = Color.FromArgb("#059669");
            allLabel.FontAttributes = FontAttributes.None;
        }

        StudentsTab.BackgroundColor = Colors.Transparent;
        if (StudentsTab.Content is Label studentsLabel)
        {
            studentsLabel.TextColor = Color.FromArgb("#059669");
            studentsLabel.FontAttributes = FontAttributes.None;
        }

        // Filter to show only staff conversations
    }

    private async void OnConversationTapped(object sender, EventArgs e)
    {
        // Load selected conversation (already showing sample conversation)
        await Task.CompletedTask;
    }

    private async void OnAttachFileTapped(object sender, EventArgs e)
    {
        await DisplayAlert("Attach File", "File attachment feature coming soon!", "OK");
    }

    private async void OnSendMessageTapped(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(MessageEntry.Text))
        {
            return;
        }

        // Here you would send the message to the database
        await DisplayAlert("Message Sent", $"Message: {MessageEntry.Text}", "OK");
        MessageEntry.Text = string.Empty;
    }

    private async void OnLogoutTapped(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");
        if (confirm)
        {
            await Shell.Current.GoToAsync("//TeacherLoginPage");
        }
    }
}
