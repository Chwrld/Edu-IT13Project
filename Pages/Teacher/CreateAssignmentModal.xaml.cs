using System.Runtime.Versioning;

namespace MauiAppIT13.Pages.Teacher;

[SupportedOSPlatform("windows10.0.17763.0")]
public partial class CreateAssignmentModal : ContentPage
{
    public CreateAssignmentModal()
    {
        InitializeComponent();
        
        // Set default deadline to 7 days from now
        DeadlineDatePicker.Date = DateTime.Now.AddDays(7);
        DeadlineTimePicker.Time = new TimeSpan(23, 59, 0); // 11:59 PM
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

    private async void OnCreateClicked(object sender, EventArgs e)
    {
        // Validate inputs
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

        // Combine date and time
        var deadline = DeadlineDatePicker.Date.Add(DeadlineTimePicker.Time);
        
        // Get points (optional)
        int points = 0;
        if (!string.IsNullOrWhiteSpace(PointsEntry.Text))
        {
            if (!int.TryParse(PointsEntry.Text, out points))
            {
                await DisplayAlert("Validation Error", "Please enter a valid number for points.", "OK");
                return;
            }
        }

        // Here you would typically save the assignment to a database
        // For now, just show a success message
        await DisplayAlert("Success", 
            $"Assignment created successfully!\n\n" +
            $"Title: {TitleEntry.Text}\n" +
            $"Deadline: {deadline:MMM dd, yyyy hh:mm tt}\n" +
            $"Points: {(points > 0 ? points.ToString() : "Not specified")}", 
            "OK");

        // Close modal
        await Navigation.PopModalAsync();
    }
}
