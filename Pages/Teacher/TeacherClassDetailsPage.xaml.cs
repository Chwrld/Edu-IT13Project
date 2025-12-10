using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Versioning;
using MauiAppIT13.Database;
using MauiAppIT13.Models;
using MauiAppIT13.Services;
using MauiAppIT13.Utils;

namespace MauiAppIT13.Pages.Teacher;

[QueryProperty(nameof(ClassIdQuery), "classId")]
[SupportedOSPlatform("windows10.0.17763.0")]
public partial class TeacherClassDetailsPage : ContentPage
{
    private ClassService? _classService;
    private AssignmentService? _assignmentService;
    private GradeService? _gradeService;
    private AnnouncementService? _announcementService;
    private readonly ObservableCollection<ClassStudent> _filteredStudents = new();
    private readonly ObservableCollection<ClassAssignment> _assignments = new();
    private readonly ObservableCollection<StudentGradeSummary> _gradeSummaries = new();
    private readonly ObservableCollection<Announcement> _announcements = new();
    private readonly ObservableCollection<AssignmentSubmission> _submissions = new();
    private readonly List<ClassStudent> _allStudents = new();
    private readonly List<ClassAssignment> _allAssignments = new();
    private readonly List<StudentGradeSummary> _allGradeSummaries = new();
    private readonly List<Announcement> _allAnnouncements = new();
    private readonly MessageService _messageService;
    private readonly TicketService _ticketService;
    private readonly AuthManager _authManager;
    private readonly DbConnection? _dbConnection;
    private Guid _classId;
    private bool _hasAppeared;
    private ClassAssignment? _selectedAssignment;

    public string? ClassIdQuery
    {
        get => _classId.ToString();
        set
        {
            if (Guid.TryParse(value, out var parsed))
            {
                var shouldReload = _classId != parsed;
                _classId = parsed;
                if (_hasAppeared && shouldReload)
                {
                    _ = LoadClassAsync();
                }
            }
        }
    }

    private void PopulateSampleAssignments(int totalStudents)
    {
        _assignments.Clear();
        _allAssignments.Clear();
        if (totalStudents <= 0)
        {
            return;
        }

        var template = new[]
        {
            new { Title = "Project Brief Submission", Description = "Upload the one-page brief outlining your project goals, constraints, and team structure.", OffsetDays = 3, Points = 20 },
            new { Title = "Midterm Problem Set", Description = "Solve programming challenges covering recursion, data structures, and complexity analysis.", OffsetDays = 10, Points = 50 },
            new { Title = "Case Study Reflection", Description = "Write a reflection about the assigned industry case study and propose two improvements.", OffsetDays = 17, Points = 30 }
        };

        var randomizer = new Random(_classId.GetHashCode());

        foreach (var (item, index) in template.Select((value, idx) => (value, idx)))
        {
            var submitted = Math.Min(totalStudents, Math.Max(0, totalStudents - randomizer.Next(0, 5) - index * 2));
            _assignments.Add(new ClassAssignment
            {
                Id = Guid.NewGuid(),
                ClassId = _classId,
                Title = item.Title,
                Description = item.Description,
                Deadline = DateTime.UtcNow.AddDays(item.OffsetDays).AddHours(17),
                TotalPoints = item.Points,
                SubmittedCount = submitted,
                TotalStudents = totalStudents
            });
        }
    }

    private void PopulateSampleGradeSummaries()
    {
        _gradeSummaries.Clear();

        foreach (var student in _allStudents)
        {
            var hash = Math.Abs(student.StudentId.GetHashCode());
            double assignments = 75 + (hash % 20);
            double activities = 70 + ((hash / 2) % 25);
            double exams = 72 + ((hash / 3) % 23);
            double projects = 74 + ((hash / 5) % 20);

            _gradeSummaries.Add(new StudentGradeSummary
            {
                StudentId = student.StudentId,
                StudentName = student.DisplayName,
                AssignmentsScore = Math.Min(assignments, 100),
                ActivitiesScore = Math.Min(activities, 100),
                ExamsScore = Math.Min(exams, 100),
                ProjectsScore = Math.Min(projects, 100)
            });
        }
    }

    public TeacherClassDetailsPage()
    {
        InitializeComponent();
        _messageService = AppServiceProvider.GetService<MessageService>()
            ?? throw new InvalidOperationException("MessageService not found");
        _ticketService = AppServiceProvider.GetService<TicketService>()
            ?? throw new InvalidOperationException("TicketService not found");
        _authManager = AppServiceProvider.GetService<AuthManager>() ?? new AuthManager();
        _dbConnection = AppServiceProvider.GetService<DbConnection>();
        _assignmentService = AppServiceProvider.GetService<AssignmentService>();
        _gradeService = AppServiceProvider.GetService<GradeService>();
        _announcementService = AppServiceProvider.GetService<AnnouncementService>();
        _classService = AppServiceProvider.GetService<ClassService>();
        StudentsCollectionView.ItemsSource = _filteredStudents;
        AssignmentsCollectionView.ItemsSource = _assignments;
        GradesCollectionView.ItemsSource = _gradeSummaries;
        AnnouncementsCollectionView.ItemsSource = _announcements;
        SubmissionsCollectionView.ItemsSource = _submissions;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _hasAppeared = true;
        if (_classId != Guid.Empty)
        {
            _ = LoadClassAsync();
        }
    }

    private async Task LoadClassAsync()
    {
        try
        {
            _classService ??= AppServiceProvider.GetService<ClassService>();
            if (_classService is null || _classId == Guid.Empty)
            {
                await DisplayAlert("Error", "Class information unavailable.", "OK");
                return;
            }

            var classModel = await _classService.GetClassByIdAsync(_classId);
            if (classModel is null)
            {
                await DisplayAlert("Error", "Unable to load class details.", "OK");
                return;
            }

            ClassTitleLabel.Text = classModel.Name;
            ClassCodeLabel.Text = classModel.Code;
            ClassStudentsLabel.Text = $"{classModel.StudentCount} Students";
            ClassTermLabel.Text = classModel.AcademicTerm;

            var students = await _classService.GetClassStudentsAsync(_classId);
            var assignmentsTask = LoadAssignmentsAsync();
            var gradesTask = LoadGradesAsync();
            var announcementsTask = LoadAnnouncementsAsync();

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _allStudents.Clear();
                _allStudents.AddRange(students);
                ApplyStudentFilter(SearchEntry.Text);
            });

            await Task.WhenAll(assignmentsTask, gradesTask, announcementsTask);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load class details: {ex.Message}", "OK");
        }
    }

    private async Task LoadAssignmentsAsync()
    {
        try
        {
            if (_assignmentService is null || _classId == Guid.Empty)
            {
                PopulateSampleAssignments(_allStudents.Count);
                _allAssignments.Clear();
                _allAssignments.AddRange(_assignments);
                return;
            }

            var assignments = await _assignmentService.GetClassAssignmentsAsync(_classId);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _allAssignments.Clear();
                _allAssignments.AddRange(assignments);
                ApplyAssignmentsFilter(SearchEntry?.Text);
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Assignments", $"Unable to load assignments: {ex.Message}", "OK");
            PopulateSampleAssignments(_allStudents.Count);
            _allAssignments.Clear();
            _allAssignments.AddRange(_assignments);
        }
    }

    private async Task LoadGradesAsync()
    {
        try
        {
            if (_gradeService is null || _classId == Guid.Empty)
            {
                PopulateSampleGradeSummaries();
                _allGradeSummaries.Clear();
                _allGradeSummaries.AddRange(_gradeSummaries);
                return;
            }

            var grades = await _gradeService.GetClassGradesAsync(_classId);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _allGradeSummaries.Clear();
                _allGradeSummaries.AddRange(grades);
                ApplyGradesFilter(SearchEntry?.Text);
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Grades", $"Unable to load grades: {ex.Message}", "OK");
            PopulateSampleGradeSummaries();
            _allGradeSummaries.Clear();
            _allGradeSummaries.AddRange(_gradeSummaries);
        }
    }

    private async Task LoadAnnouncementsAsync()
    {
        try
        {
            if (_announcementService is null || _classId == Guid.Empty)
            {
                return;
            }

            var announcements = await _announcementService.GetClassAnnouncementsAsync(_classId);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _allAnnouncements.Clear();
                _allAnnouncements.AddRange(announcements);
                ApplyAnnouncementsFilter(SearchEntry?.Text);
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Announcements", $"Unable to load announcements: {ex.Message}", "OK");
        }
    }

    private async void OnBackToClassesTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//TeacherClassesPage");
    }

    private async void OnDashboardTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//TeacherHomePage");
    }

    private async void OnClassesMenuTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//TeacherClassesPage");
    }

    private async void OnMessagesTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//TeacherMessagesPage");
    }

    private async void OnAnnouncementsTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//TeacherAnnouncementsPage");
    }

    private async void OnTicketsTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//TeacherTicketsPage");
    }

    // Search functionality
    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (StudentsContent.IsVisible)
        {
            ApplyStudentFilter(e.NewTextValue);
        }
        else if (AssignmentsContent.IsVisible)
        {
            ApplyAssignmentsFilter(e.NewTextValue);
        }
        else if (GradesContent.IsVisible)
        {
            ApplyGradesFilter(e.NewTextValue);
        }
        else if (AnnouncementsContent.IsVisible)
        {
            ApplyAnnouncementsFilter(e.NewTextValue);
        }
    }

    private void ApplyStudentFilter(string? searchText)
    {
        var query = _allStudents.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var term = searchText.Trim().ToLowerInvariant();
            query = query.Where(student =>
                student.DisplayName?.ToLowerInvariant().Contains(term) == true ||
                student.StudentNumber?.ToLowerInvariant().Contains(term) == true ||
                student.Email?.ToLowerInvariant().Contains(term) == true);
        }

        _filteredStudents.Clear();
        foreach (var student in query)
        {
            _filteredStudents.Add(student);
        }
    }

    // Tab Navigation
    private void OnStudentsTabTapped(object sender, EventArgs e)
    {
        ShowTab("Students");
    }

    private void OnAssignmentsTabTapped(object sender, EventArgs e)
    {
        ShowTab("Assignments");
    }

    private void OnGradesTabTapped(object sender, EventArgs e)
    {
        ShowTab("Grades");
    }

    private void OnAnnouncementsTabTapped(object sender, EventArgs e)
    {
        ShowTab("Announcements");
    }

    private void ShowTab(string tabName)
    {
        // Hide all content
        StudentsContent.IsVisible = false;
        AssignmentsContent.IsVisible = false;
        GradesContent.IsVisible = false;
        AnnouncementsContent.IsVisible = false;

        // Reset all tab styles
        StudentsTab.BackgroundColor = Color.FromArgb("#E5E7EB");
        ((Label)StudentsTab.Content).TextColor = Color.FromArgb("#6B7280");
        ((Label)StudentsTab.Content).FontAttributes = FontAttributes.None;
        
        AssignmentsTab.BackgroundColor = Color.FromArgb("#E5E7EB");
        ((Label)AssignmentsTab.Content).TextColor = Color.FromArgb("#6B7280");
        ((Label)AssignmentsTab.Content).FontAttributes = FontAttributes.None;
        
        GradesTab.BackgroundColor = Color.FromArgb("#E5E7EB");
        ((Label)GradesTab.Content).TextColor = Color.FromArgb("#6B7280");
        ((Label)GradesTab.Content).FontAttributes = FontAttributes.None;
        
        AnnouncementsTab.BackgroundColor = Color.FromArgb("#E5E7EB");
        ((Label)AnnouncementsTab.Content).TextColor = Color.FromArgb("#6B7280");
        ((Label)AnnouncementsTab.Content).FontAttributes = FontAttributes.None;

        // Show selected tab
        switch (tabName)
        {
            case "Students":
                StudentsContent.IsVisible = true;
                StudentsTab.BackgroundColor = Color.FromArgb("#059669");
                ((Label)StudentsTab.Content).TextColor = Colors.White;
                ((Label)StudentsTab.Content).FontAttributes = FontAttributes.Bold;
                ApplyStudentFilter(SearchEntry?.Text);
                break;
            case "Assignments":
                AssignmentsContent.IsVisible = true;
                AssignmentsTab.BackgroundColor = Color.FromArgb("#059669");
                ((Label)AssignmentsTab.Content).TextColor = Colors.White;
                ((Label)AssignmentsTab.Content).FontAttributes = FontAttributes.Bold;
                ApplyAssignmentsFilter(SearchEntry?.Text);
                break;
            case "Grades":
                GradesContent.IsVisible = true;
                GradesTab.BackgroundColor = Color.FromArgb("#059669");
                ((Label)GradesTab.Content).TextColor = Colors.White;
                ((Label)GradesTab.Content).FontAttributes = FontAttributes.Bold;
                ApplyGradesFilter(SearchEntry?.Text);
                break;
            case "Announcements":
                AnnouncementsContent.IsVisible = true;
                AnnouncementsTab.BackgroundColor = Color.FromArgb("#059669");
                ((Label)AnnouncementsTab.Content).TextColor = Colors.White;
                ((Label)AnnouncementsTab.Content).FontAttributes = FontAttributes.Bold;
                ApplyAnnouncementsFilter(SearchEntry?.Text);
                break;
        }
    }

    private async void OnEditGradesClicked(object sender, EventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }

        if (_gradeService is null)
        {
            await DisplayAlert("Grades", "Grade service unavailable. Please try again later.", "OK");
            return;
        }

        var isCurrentlyEditing = button.Text == "Save Grades";

        if (!isCurrentlyEditing)
        {
            // Enable editing mode for all entries
            SetAllGradeEntriesEnabled(true);
            button.Text = "Save Grades";
            button.BackgroundColor = Color.FromArgb("#3B82F6"); // Blue color for save
            return;
        }

        // Validate and save all grades
        var confirm = await DisplayAlert(
            "Save All Changes",
            "Save all updated grades?",
            "Save",
            "Cancel");

        if (!confirm)
        {
            return;
        }

        try
        {
            bool allSuccess = true;
            foreach (var grade in _gradeSummaries)
            {
                if (!TryParseScore(grade.AssignmentsScore.ToString(), out var assignments) ||
                    !TryParseScore(grade.ActivitiesScore.ToString(), out var activities) ||
                    !TryParseScore(grade.ExamsScore.ToString(), out var exams) ||
                    !TryParseScore(grade.ProjectsScore.ToString(), out var projects))
                {
                    await DisplayAlert("Grades", $"Invalid scores for {grade.StudentName}. Scores must be numbers between 0 and 100.", "OK");
                    return;
                }

                var updated = await _gradeService.UpdateStudentGradeAsync(
                    grade.GradeId,
                    assignments,
                    activities,
                    exams,
                    projects);

                if (!updated)
                {
                    allSuccess = false;
                }
            }

            SetAllGradeEntriesEnabled(false);
            button.Text = "Edit Grades";
            button.BackgroundColor = Color.FromArgb("#059669"); // Green color for edit

            await LoadGradesAsync();
            
            if (allSuccess)
            {
                await DisplayAlert("Grades", "All grades updated successfully.", "Great!");
            }
            else
            {
                await DisplayAlert("Grades", "Some grades could not be updated. Please try again.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Grades", $"Failed to update grades: {ex.Message}", "OK");
        }
    }

    private void SetAllGradeEntriesEnabled(bool enabled)
    {
        if (GradesCollectionView?.Handler?.PlatformView is null)
        {
            return;
        }

        // Find all Entry controls in the grades collection view
        var entries = FindAllEntriesInView(GradesCollectionView);
        foreach (var entry in entries)
        {
            entry.IsEnabled = enabled;
        }
    }

    private static List<Entry> FindAllEntriesInView(VisualElement view)
    {
        var entries = new List<Entry>();
        
        if (view is Entry entry)
        {
            entries.Add(entry);
            return entries;
        }

        if (view is Layout layout)
        {
            foreach (var child in layout.Children.OfType<VisualElement>())
            {
                entries.AddRange(FindAllEntriesInView(child));
            }
        }
        else if (view is ScrollView scrollView && scrollView.Content is VisualElement scrollContent)
        {
            entries.AddRange(FindAllEntriesInView(scrollContent));
        }
        else if (view is ContentView contentView && contentView.Content is VisualElement content)
        {
            entries.AddRange(FindAllEntriesInView(content));
        }

        return entries;
    }

    private async void OnSingleGradeEditClicked(object sender, EventArgs e)
    {
        if (sender is not Button button || button.CommandParameter is not StudentGradeSummary grade)
        {
            return;
        }

        if (_gradeService is null)
        {
            await DisplayAlert("Grades", "Grade service unavailable. Please try again later.", "OK");
            return;
        }

        if (FindGradeEntries(button, out var assignmentsEntry, out var activitiesEntry, out var examsEntry, out var projectsEntry) is false)
        {
            await DisplayAlert("Grades", "Unable to edit this row. Please try again.", "OK");
            return;
        }

        var isEditing = assignmentsEntry.IsEnabled;

        if (!isEditing)
        {
            SetEntriesEnabled(assignmentsEntry, activitiesEntry, examsEntry, projectsEntry, true);
            button.Text = "Save";
            return;
        }

        if (!TryParseScore(assignmentsEntry.Text, out var assignments) ||
            !TryParseScore(activitiesEntry.Text, out var activities) ||
            !TryParseScore(examsEntry.Text, out var exams) ||
            !TryParseScore(projectsEntry.Text, out var projects))
        {
            await DisplayAlert("Grades", "Scores must be numbers between 0 and 100.", "OK");
            return;
        }

        var confirm = await DisplayAlert(
            "Save Changes",
            $"Save the updated scores for {grade.StudentName}?",
            "Save",
            "Cancel");

        if (!confirm)
        {
            return;
        }

        try
        {
            var updated = await _gradeService.UpdateStudentGradeAsync(
                grade.GradeId,
                assignments,
                activities,
                exams,
                projects);

            if (!updated)
            {
                await DisplayAlert("Grades", "Unable to save the grade. Please try again.", "OK");
                return;
            }

            SetEntriesEnabled(assignmentsEntry, activitiesEntry, examsEntry, projectsEntry, false);
            button.Text = "Edit";

            await LoadGradesAsync();
            await DisplayAlert("Grades", "Grade updated successfully.", "Great!");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Grades", $"Failed to update grade: {ex.Message}", "OK");
        }
    }

    private static bool FindGradeEntries(Button button, out Entry assignments, out Entry activities, out Entry exams, out Entry projects)
    {
        assignments = activities = exams = projects = null!;
        if (GetParentGrid(button) is not Grid grid)
        {
            return false;
        }

        var entries = grid.Children.OfType<Entry>().ToArray();
        assignments = entries.FirstOrDefault(e => Grid.GetColumn(e) == 1)!;
        activities = entries.FirstOrDefault(e => Grid.GetColumn(e) == 2)!;
        exams = entries.FirstOrDefault(e => Grid.GetColumn(e) == 3)!;
        projects = entries.FirstOrDefault(e => Grid.GetColumn(e) == 4)!;

        return assignments is not null && activities is not null && exams is not null && projects is not null;
    }

    private static Grid? GetParentGrid(VisualElement element)
    {
        return element.Parent switch
        {
            Grid grid => grid,
            VisualElement visual => GetParentGrid(visual),
            _ => null
        };
    }

    private static void SetEntriesEnabled(Entry assignments, Entry activities, Entry exams, Entry projects, bool enabled)
    {
        assignments.IsEnabled = enabled;
        activities.IsEnabled = enabled;
        exams.IsEnabled = enabled;
        projects.IsEnabled = enabled;
    }

    private static bool TryParseScore(string? text, out double score)
    {
        if (double.TryParse(text, out score))
        {
            score = Math.Clamp(score, 0, 100);
            return true;
        }

        score = 0;
        return false;
    }

    private void ApplyAssignmentsFilter(string? searchText)
    {
        var term = searchText?.Trim().ToLowerInvariant();
        IEnumerable<ClassAssignment> query = _allAssignments;

        if (!string.IsNullOrWhiteSpace(term))
        {
            query = query.Where(a =>
                (a.Title?.ToLowerInvariant().Contains(term) ?? false) ||
                (a.Description?.ToLowerInvariant().Contains(term) ?? false) ||
                a.DeadlineBadge.ToLowerInvariant().Contains(term) ||
                a.SubmissionSummary.ToLowerInvariant().Contains(term));
        }

        _assignments.Clear();
        foreach (var assignment in query)
        {
            _assignments.Add(assignment);
        }
    }

    private void ApplyGradesFilter(string? searchText)
    {
        var term = searchText?.Trim().ToLowerInvariant();
        IEnumerable<StudentGradeSummary> query = _allGradeSummaries;

        if (!string.IsNullOrWhiteSpace(term))
        {
            query = query.Where(g =>
                g.StudentName.ToLowerInvariant().Contains(term) ||
                g.AssignmentsDisplay.ToLowerInvariant().Contains(term) ||
                g.ActivitiesDisplay.ToLowerInvariant().Contains(term) ||
                g.ExamsDisplay.ToLowerInvariant().Contains(term) ||
                g.ProjectsDisplay.ToLowerInvariant().Contains(term) ||
                g.FinalAverageDisplay.ToLowerInvariant().Contains(term));
        }

        _gradeSummaries.Clear();
        foreach (var grade in query)
        {
            _gradeSummaries.Add(grade);
        }
    }

    private void ApplyAnnouncementsFilter(string? searchText)
    {
        var term = searchText?.Trim().ToLowerInvariant();
        IEnumerable<Announcement> query = _allAnnouncements;

        if (!string.IsNullOrWhiteSpace(term))
        {
            query = query.Where(a =>
                (a.Title?.ToLowerInvariant().Contains(term) ?? false) ||
                (a.Content?.ToLowerInvariant().Contains(term) ?? false) ||
                a.CreatedDisplay.ToLowerInvariant().Contains(term));
        }

        _announcements.Clear();
        foreach (var announcement in query)
        {
            _announcements.Add(announcement);
        }
    }

    // Student Actions
    private async void OnMessageStudentClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not ClassStudent student)
            return;

        var teacher = await GetCurrentTeacherAsync();
        if (teacher is null)
        {
            await DisplayAlert("Messaging", "Unable to determine your account. Please re-login.", "OK");
            return;
        }

        var content = await DisplayPromptAsync(
            $"Message {student.DisplayName}",
            "Enter the message you want to send.",
            maxLength: 500,
            keyboard: Keyboard.Chat);

        if (string.IsNullOrWhiteSpace(content))
            return;

        var success = await _messageService.SendMessageAsync(teacher.Id, student.StudentId, content.Trim());
        if (success)
        {
            await DisplayAlert("Messaging", "Message sent successfully.", "Great!");
        }
        else
        {
            await DisplayAlert("Messaging", "Failed to send the message. Please try again.", "OK");
        }
    }

    private async void OnViewConcernsClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not ClassStudent student)
            return;

        var tickets = await _ticketService.GetStudentTicketsAsync(student.StudentId);
        if (tickets.Count == 0)
        {
            await DisplayAlert("Student Concerns", $"No concerns recorded for {student.DisplayName}.", "Close");
            return;
        }

        var ticketSummaries = tickets
            .Select(t => $"{t.Title} • {t.Status.ToUpperInvariant()} ({t.Priority})")
            .Take(10)
            .ToArray();

        await DisplayActionSheet(
            $"Concerns for {student.DisplayName}",
            "Close",
            null,
            ticketSummaries);
    }

    private async void OnViewStudentDetailsClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not ClassStudent student)
            return;

        var details =
            $"Name: {student.DisplayName}\n" +
            $"Student #: {student.StudentNumber}\n" +
            $"Email: {student.Email}\n" +
            $"Status: {student.Status}";

        await DisplayAlert("Student Details", details, "Close");
    }

    private async Task<User?> GetCurrentTeacherAsync()
    {
        if (_authManager.CurrentUser is not null)
            return _authManager.CurrentUser;

        if (_dbConnection is null)
            return null;

        // Fallback to default teacher acct
        var fallback = await _dbConnection.GetUserByEmailAsync("teacher@university.edu");
        if (fallback is not null)
        {
            _authManager.SetAuthenticatedUser(fallback);
        }

        return fallback;
    }

    // Assignment Actions
    private async void OnCreateAssignmentClicked(object sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new CreateAssignmentModal(SubmitAssignmentDraftAsync));
    }

    private async Task<bool> SubmitAssignmentDraftAsync(AssignmentDraft draft)
    {
        if (_assignmentService is null || _classId == Guid.Empty)
        {
            await DisplayAlert("Assignments", "Assignments service unavailable. Please try again later.", "OK");
            return false;
        }

        var teacher = await GetCurrentTeacherAsync();
        if (teacher is null)
        {
            await DisplayAlert("Assignments", "Unable to determine your account. Please re-login.", "OK");
            return false;
        }

        var localDeadline = draft.DeadlineLocal.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(draft.DeadlineLocal, DateTimeKind.Local)
            : draft.DeadlineLocal;
        var deadlineUtc = localDeadline.ToUniversalTime();

        var assignmentId = await _assignmentService.CreateAssignmentAsync(
            _classId,
            draft.Title,
            draft.Description,
            deadlineUtc,
            Math.Max(0, draft.TotalPoints),
            teacher.Id);

        if (!assignmentId.HasValue)
        {
            await DisplayAlert("Assignments", "Unable to save the assignment. Please try again.", "OK");
            return false;
        }

        await LoadAssignmentsAsync();
        await DisplayAlert("Assignments", "Assignment created successfully.", "Great!");
        return true;
    }

    // Announcement Actions
    private async void OnSendAnnouncementClicked(object sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new SendAnnouncementModal(SubmitAnnouncementDraftAsync));
    }

    private async Task<bool> SubmitAnnouncementDraftAsync(AnnouncementDraft draft)
    {
        if (_announcementService is null || _classId == Guid.Empty)
        {
            await DisplayAlert("Announcements", "Announcement service unavailable. Please try again later.", "OK");
            return false;
        }

        var teacher = await GetCurrentTeacherAsync();
        if (teacher is null)
        {
            await DisplayAlert("Announcements", "Unable to determine your account. Please re-login.", "OK");
            return false;
        }

        var isNormalPriority = string.Equals(draft.Priority, "Normal", StringComparison.OrdinalIgnoreCase);
        var title = isNormalPriority ? draft.Subject : $"{draft.Priority.ToUpperInvariant()} • {draft.Subject}";
        var content = isNormalPriority ? draft.Message : $"[{draft.Priority}] {draft.Message}";

        var newId = await _announcementService.CreateAnnouncementAsync(
            title,
            content,
            "students",
            true,
            teacher.Id,
            teacher.Id,
            _classId);

        if (!newId.HasValue)
        {
            await DisplayAlert("Announcements", "Unable to send the announcement. Please try again.", "OK");
            return false;
        }

        await LoadAnnouncementsAsync();
        await DisplayAlert("Announcements", "Announcement sent successfully.", "Great!");
        return true;
    }

    private async void OnLogoutTapped(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");
        if (confirm)
        {
            await Shell.Current.GoToAsync("//MainPage");
        }
    }

    // Assignment Submissions Modal
    private async void OnAssignmentTapped(object sender, TappedEventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"OnAssignmentTapped called, Parameter type: {e?.Parameter?.GetType().Name}");
            
            if (e?.Parameter is not ClassAssignment assignment)
            {
                System.Diagnostics.Debug.WriteLine("OnAssignmentTapped: Parameter is not ClassAssignment");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"OnAssignmentTapped: Assignment ID = {assignment.Id}, Title = {assignment.Title}");
            
            _selectedAssignment = assignment;
            
            try
            {
                // Update modal header
                SubmissionsAssignmentTitle.Text = assignment.Title;
                SubmissionsAssignmentInfo.Text = $"{assignment.SubmittedCount}/{assignment.TotalStudents} submissions • Due: {assignment.DeadlineDisplay}";
            }
            catch (Exception labelEx)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating labels: {labelEx.Message}");
                throw;
            }

            // Show modal first
            AssignmentSubmissionsOverlay.IsVisible = true;

            // Load submissions asynchronously
            if (_assignmentService != null)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"OnAssignmentTapped: Loading submissions for assignment {assignment.Id}");
                    var submissions = await _assignmentService.GetAssignmentSubmissionsAsync(assignment.Id, assignment.TotalPoints);
                    System.Diagnostics.Debug.WriteLine($"OnAssignmentTapped: Loaded {submissions.Count} submissions");
                    
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        _submissions.Clear();
                        foreach (var submission in submissions)
                        {
                            _submissions.Add(submission);
                        }
                        
                        // Hide loading indicator and show submissions list
                        SubmissionsLoadingIndicator.IsVisible = false;
                        SubmissionsScrollView.IsVisible = true;
                    });
                }
                catch (Exception submissionEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Error fetching submissions: {submissionEx.GetType().Name}: {submissionEx.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {submissionEx.StackTrace}");
                    
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        SubmissionsLoadingIndicator.IsVisible = false;
                    });
                    
                    await DisplayAlert("Error", $"Failed to load submissions: {submissionEx.Message}", "OK");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("AssignmentService is null");
                
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    SubmissionsLoadingIndicator.IsVisible = false;
                });
                
                await DisplayAlert("Error", "Assignment service not available", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading assignment submissions: {ex.GetType().Name}: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            await DisplayAlert("Error", $"Failed to load assignment submissions: {ex.Message}", "OK");
        }
    }

    private void OnCloseSubmissionsModal(object sender, EventArgs e)
    {
        AssignmentSubmissionsOverlay.IsVisible = false;
        _selectedAssignment = null;
        _submissions.Clear();
    }

    private async void OnViewSubmissionClicked(object sender, EventArgs e)
    {
        if (sender is not Button button || button.CommandParameter is not AssignmentSubmission submission)
        {
            return;
        }

        if (_selectedAssignment is null)
        {
            await DisplayAlert("Error", "Assignment information not available.", "OK");
            return;
        }

        // Check if student has submitted
        if (!submission.HasSubmitted || submission.SubmissionId == Guid.Empty)
        {
            await DisplayAlert("No Submission", $"{submission.StudentName} has not submitted this assignment yet.", "OK");
            return;
        }

        // Open grading modal
        var gradingModal = new GradeSubmissionModal(
            submission,
            _selectedAssignment.Title,
            SaveSubmissionGradeAsync);

        await Navigation.PushModalAsync(gradingModal);
    }

    private async Task<bool> SaveSubmissionGradeAsync(Guid submissionId, int score, string? notes)
    {
        try
        {
            if (_assignmentService is null)
            {
                await DisplayAlert("Error", "Assignment service unavailable.", "OK");
                return false;
            }

            var success = await _assignmentService.UpdateSubmissionGradeAsync(submissionId, score, notes);

            if (success)
            {
                await DisplayAlert("Success", "Grade saved successfully!", "OK");
                
                // Reload submissions to reflect updated grades
                if (_selectedAssignment is not null)
                {
                    var submissions = await _assignmentService.GetAssignmentSubmissionsAsync(
                        _selectedAssignment.Id, 
                        _selectedAssignment.TotalPoints);
                    
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        _submissions.Clear();
                        foreach (var submission in submissions)
                        {
                            _submissions.Add(submission);
                        }
                    });
                }
                
                return true;
            }
            else
            {
                await DisplayAlert("Error", "Failed to save grade. Please try again.", "OK");
                return false;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to save grade: {ex.Message}", "OK");
            return false;
        }
    }
}
