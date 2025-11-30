using System.Runtime.Versioning;

namespace MauiAppIT13.Pages.Teacher;

[SupportedOSPlatform("windows10.0.17763.0")]
public partial class SendAnnouncementModal : ContentPage
{
    public SendAnnouncementModal()
    {
        InitializeComponent();
    }

    private async void OnBackgroundTapped(object sender, EventArgs e)
    {
        // Close modal when clicking outside
        await Navigation.PopModalAsync();
    }

    private async void OnCloseTapped(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    private async void OnSendClicked(object sender, EventArgs e)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(SubjectEntry.Text))
        {
            await DisplayAlert("Validation Error", "Please enter a subject.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(MessageEditor.Text))
        {
            await DisplayAlert("Validation Error", "Please enter a message.", "OK");
            return;
        }

        // Get priority
        string priority = PriorityPicker.SelectedIndex >= 0 
            ? PriorityPicker.Items[PriorityPicker.SelectedIndex] 
            : "Normal";

        // Here you would typically save the announcement to a database
        // For now, just show a success message
        await DisplayAlert("Success", 
            $"Announcement sent successfully!\n\n" +
            $"Subject: {SubjectEntry.Text}\n" +
            $"Priority: {priority}\n" +
            $"Message: {MessageEditor.Text.Substring(0, Math.Min(50, MessageEditor.Text.Length))}...", 
            "OK");

        // Close modal
        await Navigation.PopModalAsync();
    }
}
