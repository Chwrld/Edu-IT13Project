using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace MauiAppIT13.Pages.Teacher;

[SupportedOSPlatform("windows10.0.17763.0")]
public partial class SendAnnouncementModal : ContentPage
{
    private readonly Func<AnnouncementDraft, Task<bool>>? _submitHandler;

    public SendAnnouncementModal()
    {
        InitializeComponent();
        PriorityPicker.SelectedIndex = 0;
    }

    public SendAnnouncementModal(Func<AnnouncementDraft, Task<bool>> submitHandler) : this()
    {
        _submitHandler = submitHandler;
    }

    private async void OnBackgroundTapped(object sender, EventArgs e)
    {
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

    private void OnCloseValidationErrorTapped(object sender, EventArgs e)
    {
        ValidationErrorOverlay.IsVisible = false;
    }

    private async void OnSendClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SubjectEntry.Text))
        {
            ValidationErrorMessage.Text = "Please enter a subject.";
            ValidationErrorOverlay.IsVisible = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(MessageEditor.Text))
        {
            ValidationErrorMessage.Text = "Please enter a message.";
            ValidationErrorOverlay.IsVisible = true;
            return;
        }

        string priority = PriorityPicker.SelectedIndex >= 0
            ? PriorityPicker.Items[PriorityPicker.SelectedIndex]
            : "Normal";

        if (_submitHandler is null)
        {
            await DisplayAlert("Announcements", "Sending announcements is currently unavailable.", "OK");
            return;
        }

        var draft = new AnnouncementDraft
        {
            Subject = SubjectEntry.Text.Trim(),
            Message = MessageEditor.Text.Trim(),
            Priority = priority
        };

        bool success;
        try
        {
            success = await _submitHandler(draft);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Announcements", $"Failed to send announcement: {ex.Message}", "OK");
            return;
        }

        if (success)
        {
            await Navigation.PopModalAsync();
        }
    }
}

public sealed class AnnouncementDraft
{
    public string Subject { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Priority { get; init; } = "Normal";
}
