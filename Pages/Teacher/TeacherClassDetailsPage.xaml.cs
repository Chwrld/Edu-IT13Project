using System.Runtime.Versioning;

namespace MauiAppIT13.Pages.Teacher;

[SupportedOSPlatform("windows10.0.17763.0")]
public partial class TeacherClassDetailsPage : ContentPage
{
    public TeacherClassDetailsPage()
    {
        InitializeComponent();
    }

    private async void OnBackToClassesTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//TeacherClassesPage");
    }

    private async void OnDashboardTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//TeacherHomePage");
    }

    private async void OnMessagesTapped(object sender, EventArgs e)
    {
        await DisplayAlert("Messages", "Messages feature coming soon!", "OK");
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
