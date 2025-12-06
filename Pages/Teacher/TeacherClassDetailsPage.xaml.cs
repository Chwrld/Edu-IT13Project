using System.Collections.ObjectModel;
using System.Runtime.Versioning;
using MauiAppIT13.Models;
using MauiAppIT13.Services;
using MauiAppIT13.Utils;

namespace MauiAppIT13.Pages.Teacher;

[QueryProperty(nameof(ClassIdQuery), "classId")]
[SupportedOSPlatform("windows10.0.17763.0")]
public partial class TeacherClassDetailsPage : ContentPage
{
    private ClassService? _classService;
    private readonly ObservableCollection<ClassStudent> _students = new();
    private Guid _classId;
    private bool _hasAppeared;

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

    public TeacherClassDetailsPage()
    {
        InitializeComponent();
        StudentsCollectionView.ItemsSource = _students;
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

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _students.Clear();
                foreach (var student in students)
                {
                    _students.Add(student);
                }
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load class details: {ex.Message}", "OK");
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
        // TODO: Implement search filtering logic based on active tab
        string searchText = e.NewTextValue?.ToLower() ?? "";
        
        // This can be expanded to filter students, assignments, grades, or announcements
        // depending on which tab is currently active
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
                break;
            case "Assignments":
                AssignmentsContent.IsVisible = true;
                AssignmentsTab.BackgroundColor = Color.FromArgb("#059669");
                ((Label)AssignmentsTab.Content).TextColor = Colors.White;
                ((Label)AssignmentsTab.Content).FontAttributes = FontAttributes.Bold;
                break;
            case "Grades":
                GradesContent.IsVisible = true;
                GradesTab.BackgroundColor = Color.FromArgb("#059669");
                ((Label)GradesTab.Content).TextColor = Colors.White;
                ((Label)GradesTab.Content).FontAttributes = FontAttributes.Bold;
                break;
            case "Announcements":
                AnnouncementsContent.IsVisible = true;
                AnnouncementsTab.BackgroundColor = Color.FromArgb("#059669");
                ((Label)AnnouncementsTab.Content).TextColor = Colors.White;
                ((Label)AnnouncementsTab.Content).FontAttributes = FontAttributes.Bold;
                break;
        }
    }

    // Student Actions
    private async void OnMessageStudentClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Message Student", "Message feature coming soon!", "OK");
    }

    private async void OnViewConcernsClicked(object sender, EventArgs e)
    {
        await DisplayAlert("View Concerns", "Student concerns feature coming soon!", "OK");
    }

    private async void OnViewStudentDetailsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//TeacherStudentDetailsPage");
    }

    // Assignment Actions
    private async void OnCreateAssignmentClicked(object sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new CreateAssignmentModal());
    }

    // Announcement Actions
    private async void OnSendAnnouncementClicked(object sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new SendAnnouncementModal());
    }

    private async void OnLogoutTapped(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");
        if (confirm)
        {
            await Shell.Current.GoToAsync("//MainPage");
        }
    }
}
