using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace MauiAppIT13.Pages.Teacher;

[SupportedOSPlatform("windows10.0.17763.0")]
public partial class CreateAssignmentModal : ContentPage
{
    private readonly Func<AssignmentDraft, Task<bool>>? _submitHandler;

    public CreateAssignmentModal()
    {
        InitializeComponent();

        DeadlineDatePicker.Date = DateTime.Now.AddDays(7);
        DeadlineTimePicker.Time = new TimeSpan(23, 59, 0);
    }

    public CreateAssignmentModal(Func<AssignmentDraft, Task<bool>> submitHandler) : this()
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

    private async void OnCreateClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleEntry.Text))
        {
            await DisplayAlert("Validation Error", "Please enter an assignment title.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(DescriptionEditor.Text))
        {
            await DisplayAlert("Validation Error", "Please enter a description.", "OK");
            return;
        }

        var deadline = DeadlineDatePicker.Date.Add(DeadlineTimePicker.Time);

        int points = 0;
        if (!string.IsNullOrWhiteSpace(PointsEntry.Text))
        {
            if (!int.TryParse(PointsEntry.Text, out points) || points < 0)
            {
                await DisplayAlert("Validation Error", "Please enter a valid non-negative number for points.", "OK");
                return;
            }
        }

        if (_submitHandler is null)
        {
            await DisplayAlert("Assignments", "Assignment creation is currently unavailable.", "OK");
            return;
        }

        var draft = new AssignmentDraft
        {
            Title = TitleEntry.Text.Trim(),
            Description = DescriptionEditor.Text.Trim(),
            DeadlineLocal = deadline,
            TotalPoints = points
        };

        bool success;
        try
        {
            success = await _submitHandler(draft);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Assignments", $"Failed to create assignment: {ex.Message}", "OK");
            return;
        }

        if (success)
        {
            await Navigation.PopModalAsync();
        }
    }
}

public sealed class AssignmentDraft
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTime DeadlineLocal { get; init; }
    public int TotalPoints { get; init; }
}
