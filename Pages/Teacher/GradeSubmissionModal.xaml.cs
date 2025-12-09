using System;
using System.Runtime.Versioning;
using MauiAppIT13.Models;
using MauiAppIT13.Services;
using MauiAppIT13.Utils;

namespace MauiAppIT13.Pages.Teacher;

[SupportedOSPlatform("windows10.0.17763.0")]
public partial class GradeSubmissionModal : ContentPage
{
    private readonly AssignmentSubmission _submission;
    private readonly Func<Guid, int, string?, Task<bool>> _onSaveGrade;
    private readonly AssignmentService? _assignmentService;

    public GradeSubmissionModal(
        AssignmentSubmission submission,
        string assignmentTitle,
        Func<Guid, int, string?, Task<bool>> onSaveGrade)
    {
        InitializeComponent();
        _submission = submission;
        _onSaveGrade = onSaveGrade;
        _assignmentService = AppServiceProvider.GetService<AssignmentService>();

        // Set up UI
        AssignmentTitleLabel.Text = assignmentTitle;
        StudentInfoLabel.Text = $"{submission.StudentName} • {submission.StudentNumber}";
        SubmittedAtLabel.Text = submission.SubmittedAtDisplay;
        StatusLabel.Text = submission.Status;
        StatusBadge.BackgroundColor = Color.FromArgb(submission.StatusBadgeColor);
        MaxScoreLabel.Text = $"{submission.MaxScore} points";
        OutOfLabel.Text = $"/ {submission.MaxScore}";

        // Pre-fill existing grade if available
        if (submission.Score.HasValue)
        {
            ScoreEntry.Text = submission.Score.Value.ToString();
        }

        // Pre-fill existing notes if available
        if (!string.IsNullOrEmpty(submission.Notes))
        {
            NotesEditor.Text = submission.Notes;
        }

        // Load submission content if service is available
        LoadSubmissionContent();
    }

    private async void LoadSubmissionContent()
    {
        try
        {
            if (_assignmentService != null)
            {
                var content = await _assignmentService.GetSubmissionContentAsync(_submission.SubmissionId);
                if (!string.IsNullOrEmpty(content))
                {
                    SubmissionContentLabel.Text = content;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading submission content: {ex.Message}");
            SubmissionContentLabel.Text = "Unable to load submission content.";
        }
    }

    private async void OnSaveGradeClicked(object sender, EventArgs e)
    {
        // Validate score
        if (string.IsNullOrWhiteSpace(ScoreEntry.Text))
        {
            await DisplayAlert("Validation Error", "Please enter a score.", "OK");
            return;
        }

        if (!int.TryParse(ScoreEntry.Text, out int score))
        {
            await DisplayAlert("Validation Error", "Score must be a valid number.", "OK");
            return;
        }

        if (score < 0 || score > _submission.MaxScore)
        {
            await DisplayAlert("Validation Error", 
                $"Score must be between 0 and {_submission.MaxScore}.", "OK");
            return;
        }

        // Get notes
        string? notes = string.IsNullOrWhiteSpace(NotesEditor.Text) 
            ? null 
            : NotesEditor.Text.Trim();

        // Disable button to prevent double-click
        SaveButton.IsEnabled = false;

        try
        {
            // Call the save callback
            bool success = await _onSaveGrade(_submission.SubmissionId, score, notes);

            if (success)
            {
                await Navigation.PopModalAsync();
            }
            else
            {
                SaveButton.IsEnabled = true;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to save grade: {ex.Message}", "OK");
            SaveButton.IsEnabled = true;
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        bool confirmed = await DisplayAlert(
            "Cancel Grading",
            "Are you sure you want to cancel? Any unsaved changes will be lost.",
            "Yes, Cancel",
            "No");

        if (confirmed)
        {
            await Navigation.PopModalAsync();
        }
    }
}
