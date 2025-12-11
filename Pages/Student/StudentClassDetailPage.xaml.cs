using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using MauiAppIT13.Database;
using MauiAppIT13.Models;
using MauiAppIT13.Services;
using MauiAppIT13.Utils;

namespace MauiAppIT13.Pages.Student;

[SupportedOSPlatform("windows10.0.17763.0")]
[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public partial class StudentClassDetailPage : ContentPage
{
    private readonly ClassService? _classService;
    private readonly AssignmentService? _assignmentService;
    private readonly GradeService? _gradeService;
    private readonly AuthManager _authManager;
    private readonly DbConnection? _dbConnection;
    private readonly ObservableCollection<ClassAssignment> _assignments = new();
    private readonly Guid _classId;
    private readonly string _initialTab;
    private bool _hasAppeared;
    private ClassAssignment? _currentAssignment;

    public StudentClassDetailPage(Guid classId, string initialTab = "Assignments")
    {
        InitializeComponent();

        _classId = classId;
        _initialTab = initialTab;

        _dbConnection = AppServiceProvider.GetService<DbConnection>();
        _classService = AppServiceProvider.GetService<ClassService>();
        _assignmentService = AppServiceProvider.GetService<AssignmentService>();
        _gradeService = AppServiceProvider.GetService<GradeService>();
        _authManager = AppServiceProvider.GetService<AuthManager>() ?? new AuthManager();

        AssignmentsCollectionView.ItemsSource = _assignments;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_hasAppeared)
        {
            _hasAppeared = true;
            await LoadClassDetailsAsync();
            await LoadAssignmentsAsync();
            await LoadGradesAsync();

            // Set initial tab
            if (_initialTab == "Grades")
            {
                OnGradesTabTapped(null!, null!);
            }
        }
    }

    private async Task LoadClassDetailsAsync()
    {
        if (_classService is null)
        {
            return;
        }

        try
        {
            var classModel = await _classService.GetClassByIdAsync(_classId);
            if (classModel != null)
            {
                ClassTitleLabel.Text = classModel.Name;
                ClassCodeLabel.Text = classModel.Code;
                ClassTermLabel.Text = classModel.AcademicTerm;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load class details: {ex.Message}", "OK");
        }
    }

    private async Task LoadAssignmentsAsync()
    {
        if (_assignmentService is null || _dbConnection is null)
        {
            return;
        }

        try
        {
            // Get current student ID
            var currentUser = _authManager.CurrentUser;
            Guid? studentId = null;
            
            if (currentUser?.Id != null)
            {
                var student = await _dbConnection.GetStudentByUserIdAsync(currentUser.Id);
                if (student != null)
                {
                    studentId = student.StudentId;
                }
            }

            var assignments = await _assignmentService.GetClassAssignmentsAsync(_classId, studentId);
            
            _assignments.Clear();
            foreach (var assignment in assignments)
            {
                _assignments.Add(assignment);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load assignments: {ex.Message}", "OK");
        }
    }

    private async Task LoadGradesAsync()
    {
        if (_gradeService is null || _dbConnection is null)
        {
            return;
        }

        try
        {
            var currentUser = _authManager.CurrentUser;
            if (currentUser?.Id == null)
            {
                return;
            }

            var grade = await _gradeService.GetStudentGradeAsync(_classId, currentUser.Id);
            if (grade != null)
            {
                AssignmentsGradeLabel.Text = $"{grade.AssignmentsScore:0}%";
                ActivitiesGradeLabel.Text = $"{grade.ActivitiesScore:0}%";
                ExamsGradeLabel.Text = $"{grade.ExamsScore:0}%";
                ProjectsGradeLabel.Text = $"{grade.ProjectsScore:0}%";
                FinalAverageLabel.Text = $"{grade.FinalAverage:0}%";
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load grades: {ex.Message}", "OK");
        }
    }

    private void OnAssignmentsTabTapped(object sender, EventArgs e)
    {
        // Update tab appearance
        AssignmentsTab.BackgroundColor = Color.FromArgb("#0891B2");
        if (AssignmentsTab.Content is Label assignmentsLabel)
        {
            assignmentsLabel.TextColor = Colors.White;
            assignmentsLabel.FontAttributes = FontAttributes.Bold;
        }

        GradesTab.BackgroundColor = Color.FromArgb("#E5E7EB");
        if (GradesTab.Content is Label gradesLabel)
        {
            gradesLabel.TextColor = Color.FromArgb("#6B7280");
            gradesLabel.FontAttributes = FontAttributes.None;
        }

        // Show/hide content
        AssignmentsContent.IsVisible = true;
        GradesContent.IsVisible = false;
    }

    private void OnGradesTabTapped(object sender, EventArgs e)
    {
        // Update tab appearance
        GradesTab.BackgroundColor = Color.FromArgb("#0891B2");
        if (GradesTab.Content is Label gradesLabel)
        {
            gradesLabel.TextColor = Colors.White;
            gradesLabel.FontAttributes = FontAttributes.Bold;
        }

        AssignmentsTab.BackgroundColor = Color.FromArgb("#E5E7EB");
        if (AssignmentsTab.Content is Label assignmentsLabel)
        {
            assignmentsLabel.TextColor = Color.FromArgb("#6B7280");
            assignmentsLabel.FontAttributes = FontAttributes.None;
        }

        // Show/hide content
        AssignmentsContent.IsVisible = false;
        GradesContent.IsVisible = true;
    }

    private async void OnViewAssignmentClicked(object sender, EventArgs e)
    {
        if (sender is not Button button || button.CommandParameter is not ClassAssignment assignment)
        {
            return;
        }

        _currentAssignment = assignment;

        // Populate modal with assignment details
        ModalAssignmentTitle.Text = assignment.Title;
        ModalAssignmentDescription.Text = assignment.Description;
        ModalAssignmentDeadline.Text = $"Due: {assignment.DeadlineDisplay}";
        ModalAssignmentPoints.Text = assignment.TotalPoints.ToString();

        // Check if student already submitted
        bool hasSubmitted = assignment.StudentHasSubmitted;
        
        if (hasSubmitted)
        {
            // Fetch the actual submission data
            var currentUser = _authManager.CurrentUser;
            if (currentUser?.Id != null && _assignmentService != null)
            {
                var student = await _dbConnection.GetStudentByUserIdAsync(currentUser.Id);
                if (student != null)
                {
                    var submission = await _assignmentService.GetStudentSubmissionAsync(assignment.Id, student.StudentId);
                    if (submission != null)
                    {
                        // Show read-only view with actual submission data
                        SubmissionLinkEntry.IsEnabled = false;
                        SubmissionCommentsEditor.IsEnabled = false;
                        SubmissionLinkEntry.Text = submission.SubmissionContent ?? "No submission link provided";
                        SubmissionCommentsEditor.Text = submission.Notes ?? "No comments provided";
                        SubmitButton.IsVisible = false;
                    }
                }
            }
        }
        else
        {
            // Show editable form
            SubmissionLinkEntry.IsEnabled = true;
            SubmissionCommentsEditor.IsEnabled = true;
            SubmissionLinkEntry.Text = string.Empty;
            SubmissionCommentsEditor.Text = string.Empty;
            SubmitButton.IsVisible = true;
        }

        // Show modal
        AssignmentModalOverlay.IsVisible = true;
    }

    private void OnCloseAssignmentModalTapped(object sender, EventArgs e)
    {
        AssignmentModalOverlay.IsVisible = false;
        _currentAssignment = null;
    }

    private void OnCancelSubmissionClicked(object sender, EventArgs e)
    {
        AssignmentModalOverlay.IsVisible = false;
        _currentAssignment = null;
    }

    private async void OnSubmitAssignmentClicked(object sender, EventArgs e)
    {
        if (_currentAssignment == null)
        {
            return;
        }

        var submissionLink = SubmissionLinkEntry.Text?.Trim();
        var comments = SubmissionCommentsEditor.Text?.Trim();

        // Validate submission
        if (string.IsNullOrEmpty(submissionLink))
        {
            await DisplayAlert("Validation Error", "Please provide a submission link or file path.", "OK");
            return;
        }

        var confirm = await DisplayAlert(
            "Confirm Submission",
            $"Submit your work for '{_currentAssignment.Title}'?\n\nThis action cannot be undone.",
            "Submit",
            "Cancel");

        if (!confirm)
        {
            return;
        }

        try
        {
            if (_assignmentService is null)
            {
                await DisplayAlert("Error", "Assignment service unavailable. Please try again later.", "OK");
                return;
            }

            var currentUser = _authManager.CurrentUser;
            if (currentUser?.Id == null)
            {
                await DisplayAlert("Error", "Unable to determine your account. Please re-login.", "OK");
                return;
            }

            // Get student ID from database
            var student = await _dbConnection?.GetStudentByUserIdAsync(currentUser.Id)!;
            if (student is null)
            {
                await DisplayAlert("Error", "Unable to find your student profile. Please contact support.", "OK");
                return;
            }

            var submissionId = await _assignmentService.SubmitAssignmentAsync(
                _currentAssignment.Id,
                student.StudentId,
                submissionLink,
                comments);

            if (!submissionId.HasValue)
            {
                await DisplayAlert("Error", "Failed to submit assignment. Please try again.", "OK");
                return;
            }

            // Close modal immediately
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                AssignmentModalOverlay.IsVisible = false;
                SubmissionLinkEntry.Text = string.Empty;
                SubmissionCommentsEditor.Text = string.Empty;
                _currentAssignment = null;
            });

            // Show success message
            await DisplayAlert(
                "Success",
                "Your assignment has been submitted successfully!",
                "OK");

            // Reload assignments to show updated status
            await LoadAssignmentsAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to submit assignment: {ex.Message}", "OK");
        }
    }

    private async void OnBackToClassesTapped(object sender, EventArgs e)
    {
        await Navigation.PopAsync(false);
    }

    private async void OnHomeTapped(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new HomePage(), false);
    }

    private async void OnMessagesTapped(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new MessagesPage(), false);
    }

    private async void OnClassesTapped(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new StudentClassesPage(), false);
    }

    private async void OnAnnouncementsTapped(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new AnnouncementsPage(), false);
    }

    private async void OnTicketsTapped(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new TicketsPage(), false);
    }

    private async void OnProfileTapped(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ProfilePage(), false);
    }

    private async void OnLogoutTapped(object sender, EventArgs e)
    {
        var confirm = await DisplayAlert(
            "Logout",
            "Are you sure you want to logout?",
            "Yes",
            "No");

        if (confirm)
        {
            _authManager.ClearAuthentication();
            await Navigation.PopToRootAsync();
        }
    }
}
