using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using MauiAppIT13.Database;
using MauiAppIT13.Models;
using MauiAppIT13.Services;
using MauiAppIT13.Utils;
using Microsoft.Maui.ApplicationModel;

namespace MauiAppIT13.Pages.Teacher;

[SupportedOSPlatform("windows10.0.17763.0")]
public partial class TeacherHomePage : ContentPage
{
    private readonly ClassService _classService;
    private readonly TicketService _ticketService;
    private readonly MessageService _messageService;
    private readonly AuthManager _authManager;
    private readonly SyncService _syncService;
    private readonly DeltaSyncService _deltaSyncService;
    private readonly TeacherDashboardService _dashboardService;
    private readonly DbConnection _dbConnection;
    private bool _isLoading;

    public ObservableCollection<Conversation> RecentMessages { get; } = new();
    public ObservableCollection<TicketDashboardItem> TicketUpdates { get; } = new();
    public ObservableCollection<ClassPerformanceData> ClassPerformanceData { get; } = new();
    public ObservableCollection<SubmissionStatusData> SubmissionStatusData { get; } = new();
    public ObservableCollection<StudentAcademicRiskData> AtRiskStudents { get; } = new();

    private string _welcomeTitle = "Welcome, Professor!";
    public string WelcomeTitle
    {
        get => _welcomeTitle;
        set => SetProperty(ref _welcomeTitle, value);
    }

    private string _welcomeSubtitle = "Manage your classes and student progress.";
    public string WelcomeSubtitle
    {
        get => _welcomeSubtitle;
        set => SetProperty(ref _welcomeSubtitle, value);
    }

    private int _classesCount = 0;
    public int ClassesCount
    {
        get => _classesCount;
        set => SetProperty(ref _classesCount, value);
    }

    private string _classesSubtitle = "This semester";
    public string ClassesSubtitle
    {
        get => _classesSubtitle;
        set => SetProperty(ref _classesSubtitle, value);
    }

    private int _studentsCount = 0;
    public int StudentsCount
    {
        get => _studentsCount;
        set => SetProperty(ref _studentsCount, value);
    }

    private string _studentsSubtitle = "Across all classes";
    public string StudentsSubtitle
    {
        get => _studentsSubtitle;
        set => SetProperty(ref _studentsSubtitle, value);
    }

    private int _activeTicketsCount = 0;
    public int ActiveTicketsCount
    {
        get => _activeTicketsCount;
        set => SetProperty(ref _activeTicketsCount, value);
    }

    private string _ticketsSubtitle = "Support requests";
    public string TicketsSubtitle
    {
        get => _ticketsSubtitle;
        set => SetProperty(ref _ticketsSubtitle, value);
    }

    public TeacherHomePage()
    {
        InitializeComponent();

        _classService = AppServiceProvider.GetService<ClassService>()
            ?? throw new InvalidOperationException("ClassService not found");
        _ticketService = AppServiceProvider.GetService<TicketService>()
            ?? throw new InvalidOperationException("TicketService not found");
        _messageService = AppServiceProvider.GetService<MessageService>()
            ?? throw new InvalidOperationException("MessageService not found");
        _authManager = AppServiceProvider.GetService<AuthManager>() ?? new AuthManager();
        _syncService = AppServiceProvider.GetService<SyncService>()
            ?? throw new InvalidOperationException("SyncService is not registered.");
        _deltaSyncService = AppServiceProvider.GetService<DeltaSyncService>()
            ?? throw new InvalidOperationException("DeltaSyncService is not registered.");
        _dashboardService = AppServiceProvider.GetService<TeacherDashboardService>()
            ?? throw new InvalidOperationException("TeacherDashboardService is not registered.");
        _dbConnection = AppServiceProvider.GetService<DbConnection>()
            ?? throw new InvalidOperationException("DbConnection is not registered.");

        BindingContext = this;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = LoadDashboardAsync();
    }

    private async Task LoadDashboardAsync()
    {
        if (_isLoading)
            return;

        _isLoading = true;
        try
        {
            var currentUser = _authManager.CurrentUser;

            if (currentUser is null || !string.Equals(currentUser.Email, "teacher01@university.edu", StringComparison.OrdinalIgnoreCase))
            {
                Debug.WriteLine("TeacherHomePage: Loading teacher01@university.edu profile for dashboard context");
                currentUser = await _dbConnection.GetUserByEmailAsync("teacher01@university.edu");
                if (currentUser is not null)
                {
                    _authManager.SetAuthenticatedUser(currentUser);
                }
            }

            if (currentUser is null)
            {
                await DisplayAlert("Error", "Unable to load dashboard without a teacher account.", "OK");
                return;
            }

            WelcomeTitle = $"Welcome, {(!string.IsNullOrWhiteSpace(currentUser.DisplayName) ? currentUser.DisplayName : "Professor")}";
            WelcomeSubtitle = $"Here's what's happening • {DateTime.Now:MMMM d}";

            Debug.WriteLine($"TeacherHomePage: Current user ID: {currentUser.Id}, Email: {currentUser.Email}");

            var classesTask = _classService.GetTeacherClassesAsync(currentUser.Id);
            var ticketsTask = _ticketService.GetTeacherTicketsAsync(currentUser.Id, 25);
            var messagesTask = _messageService.GetConversationsAsync(currentUser.Id);

            await Task.WhenAll(classesTask, ticketsTask, messagesTask);

            var classes = await classesTask;
            var tickets = await ticketsTask;
            var conversations = await messagesTask;

            UpdateStats(classes, tickets);
            UpdateMessages(conversations);
            UpdateTickets(tickets);
            await LoadGraphDataAsync(currentUser.Id);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"TeacherHomePage: Failed to load dashboard - {ex.Message}");
            await DisplayAlert("Dashboard", "Failed to load teacher data. Please try again.", "OK");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task LoadGraphDataAsync(Guid teacherId)
    {
        try
        {
            Debug.WriteLine($"TeacherHomePage: Loading graph data for teacher {teacherId}");
            Debug.WriteLine($"TeacherHomePage: _dashboardService is null: {_dashboardService == null}");
            Debug.WriteLine($"TeacherHomePage: _dbConnection is null: {_dbConnection == null}");
            Debug.WriteLine($"TeacherHomePage: _dbConnection type: {_dbConnection?.GetType().Name}");

            var performanceTask = _dashboardService.GetClassPerformanceAsync(teacherId);
            var submissionTask = _dashboardService.GetSubmissionStatusAsync(teacherId);
            var atRiskTask = _dashboardService.GetStudentsAtAcademicRiskAsync(teacherId);

            await Task.WhenAll(performanceTask, submissionTask, atRiskTask);

            var performanceList = await performanceTask;
            var submissionList = await submissionTask;
            var atRiskList = await atRiskTask;

            Debug.WriteLine($"TeacherHomePage: Received {performanceList.Count} performance items, {submissionList.Count} submission items, and {atRiskList.Count} at-risk students");

            MainThread.BeginInvokeOnMainThread(() =>
            {
                ClassPerformanceData.Clear();
                foreach (var data in performanceList)
                {
                    ClassPerformanceData.Add(data);
                    Debug.WriteLine($"TeacherHomePage: Added performance data - {data.ClassName}: {data.AverageGrade:F2}");
                }

                SubmissionStatusData.Clear();
                foreach (var data in submissionList)
                {
                    SubmissionStatusData.Add(data);
                    Debug.WriteLine($"TeacherHomePage: Added submission data - {data.ClassName}: {data.Submitted}/{data.Total}");
                }

                AtRiskStudents.Clear();
                foreach (var data in atRiskList)
                {
                    AtRiskStudents.Add(data);
                    Debug.WriteLine($"TeacherHomePage: Added at-risk student - {data.StudentName} in {data.ClassName}: {data.Grade} ({data.RiskLevel})");
                }

                Debug.WriteLine($"TeacherHomePage: After adding - Performance items: {ClassPerformanceData.Count}, Submission items: {SubmissionStatusData.Count}, At-risk students: {AtRiskStudents.Count}");
            });

            Debug.WriteLine($"TeacherHomePage: Graph data loaded - Performance items: {ClassPerformanceData.Count}, Submission items: {SubmissionStatusData.Count}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"TeacherHomePage: Failed to load graph data - {ex.Message}\n{ex.StackTrace}");
        }
    }

    private void UpdateStats(IReadOnlyCollection<ClassModel> classes, IReadOnlyCollection<Ticket> tickets)
    {
        var classCount = classes.Count;
        var studentCount = classes.Sum(c => c.StudentCount);

        ClassesCount = classCount;
        ClassesSubtitle = classCount switch
        {
            0 => "No assigned classes",
            1 => "Class this term",
            _ => "Classes this term"
        };

        StudentsCount = studentCount;
        StudentsSubtitle = studentCount switch
        {
            0 => "No enrolled students",
            1 => "Advisee assigned",
            _ => "Across all classes"
        };

        var openCount = tickets.Count(t => string.Equals(t.Status, "open", StringComparison.OrdinalIgnoreCase));
        var inProgressCount = tickets.Count(t => string.Equals(t.Status, "in_progress", StringComparison.OrdinalIgnoreCase));
        var activeCount = tickets.Count(t => !string.Equals(t.Status, "resolved", StringComparison.OrdinalIgnoreCase));

        ActiveTicketsCount = activeCount;
        TicketsSubtitle = activeCount > 0
            ? $"{openCount} open • {inProgressCount} in progress"
            : "No active tickets";
    }

    private void UpdateMessages(IEnumerable<Conversation> conversations)
    {
        var latest = conversations
            .OrderByDescending(c => c.LastMessageTime)
            .Take(3)
            .ToList();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            RecentMessages.Clear();
            foreach (var conversation in latest)
            {
                RecentMessages.Add(conversation);
            }
        });
    }

    private void UpdateTickets(IEnumerable<Ticket> tickets)
    {
        var ticketItems = tickets
            .OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt)
            .Take(3)
            .Select(MapToDashboardTicket)
            .ToList();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            TicketUpdates.Clear();
            foreach (var ticket in ticketItems)
            {
                TicketUpdates.Add(ticket);
            }
        });
    }

    private static TicketDashboardItem MapToDashboardTicket(Ticket ticket)
    {
        var status = ticket.Status?.ToLowerInvariant() ?? "open";
        var (label, badgeColor, cardColor) = status switch
        {
            "open" => ("NEW", "#F59E0B", "#FEF3C7"),
            "in_progress" => ("OPEN", "#0284C7", "#DBEAFE"),
            "resolved" => ("DONE", "#059669", "#D1FAE5"),
            _ => ("TICKET", "#6B7280", "#F3F4F6")
        };

        return new TicketDashboardItem
        {
            Title = ticket.Title,
            Summary = string.IsNullOrWhiteSpace(ticket.Description)
                ? "No description provided."
                : ticket.Description,
            StatusLabel = label,
            StatusColor = badgeColor,
            CardBackgroundColor = cardColor,
            Timestamp = FormatRelativeTime(ticket.UpdatedAt ?? ticket.CreatedAt),
            Author = string.IsNullOrWhiteSpace(ticket.CreatedByName)
                ? (ticket.StudentName ?? "Student")
                : ticket.CreatedByName
        };
    }

    private static string FormatRelativeTime(DateTime timestamp)
    {
        var local = timestamp.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(timestamp, DateTimeKind.Utc).ToLocalTime()
            : timestamp.ToLocalTime();

        var span = DateTime.Now - local;
        if (span.TotalMinutes < 1)
            return "just now";
        if (span.TotalMinutes < 60)
            return $"{Math.Floor(span.TotalMinutes)} min ago";
        if (span.TotalHours < 24)
            return $"{Math.Floor(span.TotalHours)} hr ago";
        if (span.TotalDays < 7)
            return $"{Math.Floor(span.TotalDays)} d ago";

        return local.ToString("MMM d", System.Globalization.CultureInfo.InvariantCulture);
    }

    private async void OnClassesTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//TeacherClassesPage", animate: false);
    }

    private async void OnMessagesTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//TeacherMessagesPage", animate: false);
    }

    private async void OnAnnouncementsTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//TeacherAnnouncementsPage", animate: false);
    }

    private async void OnTicketsTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//TeacherTicketsPage", animate: false);
    }

    private async void OnFacultyTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//TeacherProfilePage", animate: false);
    }

    private async void OnLogoutTapped(object sender, EventArgs e)
    {
        LogoutModal.IsVisible = true;
    }

    private async void OnLogoutConfirmed(object sender, EventArgs e)
    {
        LogoutModal.IsVisible = false;
        await Shell.Current.GoToAsync("//MainPage", animate: false);
    }

    private void OnLogoutCancelled(object sender, EventArgs e)
    {
        LogoutModal.IsVisible = false;
    }

    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
            return false;

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private async void OnSyncClicked(object sender, EventArgs e)
    {
        await ShowSyncProgressAsync();
    }

    private async Task ShowSyncProgressAsync()
    {
        try
        {
            // Show modal
            SyncProgressOverlay.IsVisible = true;
            SyncProgressBar.Progress = 0;
            SyncPercentageLabel.Text = "0%";
            SyncStatusLabel.Text = "Checking connection...";
            SyncResultLabel.IsVisible = false;
            SyncCloseButton.IsVisible = false;
            Step1Icon.Text = "⏳";
            Step2Icon.Text = "⏳";
            Step3Icon.Text = "⏳";

            // Step 1: Check connection (20%)
            await UpdateProgress(0.2, "Checking connection...", "Step 1");
            bool isOnline = await _syncService.IsOnlineAsync();
            Debug.WriteLine($"[TeacherSync] Online status: {isOnline}");

            if (!isOnline)
            {
                Step1Icon.Text = "❌";
                SyncStatusLabel.Text = "Remote server not accessible";
                SyncResultContainer.IsVisible = false;
                SyncCloseButton.IsVisible = true;
                return;
            }

            Step1Icon.Text = "✅";

            // Step 2: Delta sync (only changed records) (60%)
            await UpdateProgress(0.6, "Syncing changed records to remote database...", "Step 2");
            var (success, recordsSynced) = await _deltaSyncService.DeltaSyncToRemoteAsync();

            if (!success)
            {
                Step2Icon.Text = "❌";
                SyncStatusLabel.Text = "Sync failed";
                SyncResultLabel.Text = "❌ Delta sync failed. Check debug output for details.";
                SyncResultContainer.BackgroundColor = Color.FromArgb("#FEF2F2");
                SyncResultContainer.Stroke = Color.FromArgb("#FECACA");
                SyncResultLabel.TextColor = Color.FromArgb("#DC2626");
                SyncResultContainer.IsVisible = true;
                SyncCloseButton.IsVisible = true;
                return;
            }

            Step2Icon.Text = "✅";

            // Step 3: Finalize (100%)
            await UpdateProgress(1.0, "Sync complete!", "Step 3");
            Step3Icon.Text = "✅";
            
            SyncStatusLabel.Text = "Sync completed successfully";
            SyncResultLabel.Text = $"✅ Successfully synced {recordsSynced} changed records to remote database!";
            SyncResultContainer.BackgroundColor = Color.FromArgb("#F0FDF4");
            SyncResultContainer.Stroke = Color.FromArgb("#86EFAC");
            SyncResultLabel.TextColor = Color.FromArgb("#10B981");
            SyncResultContainer.IsVisible = true;
            SyncCloseButton.IsVisible = true;
            Debug.WriteLine($"[TeacherSync] Sync completed successfully - {recordsSynced} records synced");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TeacherSync] Error: {ex.Message}");
            Step1Icon.Text = "❌";
            Step2Icon.Text = "❌";
            Step3Icon.Text = "❌";
            SyncStatusLabel.Text = "Error occurred";
            SyncResultLabel.Text = $"❌ Error: {ex.Message}";
            SyncResultContainer.BackgroundColor = Color.FromArgb("#FEF2F2");
            SyncResultContainer.Stroke = Color.FromArgb("#FECACA");
            SyncResultLabel.TextColor = Color.FromArgb("#DC2626");
            SyncResultContainer.IsVisible = true;
            SyncCloseButton.IsVisible = true;
        }
    }

    private async Task UpdateProgress(double progress, string status, string step)
    {
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            SyncProgressBar.Progress = progress;
            SyncPercentageLabel.Text = $"{(int)(progress * 100)}%";
            SyncStatusLabel.Text = status;
        });
        await Task.Delay(300);
    }

    private void OnSyncCloseClicked(object sender, EventArgs e)
    {
        SyncProgressOverlay.IsVisible = false;
    }

    public sealed class TicketDashboardItem
    {
        public string Title { get; init; } = string.Empty;
        public string Summary { get; init; } = string.Empty;
        public string StatusLabel { get; init; } = string.Empty;
        public string StatusColor { get; init; } = "#6B7280";
        public string CardBackgroundColor { get; init; } = "#F3F4F6";
        public string Timestamp { get; init; } = string.Empty;
        public string Author { get; init; } = string.Empty;
    }
}
