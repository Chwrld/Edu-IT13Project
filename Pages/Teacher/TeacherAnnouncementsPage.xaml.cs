using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.Versioning;
using MauiAppIT13.Utils;

namespace MauiAppIT13.Pages.Teacher;

[SupportedOSPlatform("windows10.0.17763.0")]
[SupportedOSPlatform("android21.0")]
public partial class TeacherAnnouncementsPage : ContentPage
{
    private readonly AuthManager _authManager;
    private ObservableCollection<AnnouncementItem> _allAnnouncements = new();
    private ObservableCollection<AnnouncementItem> _filteredAnnouncements = new();
    private AnnouncementItem? _selectedAnnouncement;
    private string _currentFilter = "all";
    private bool _isEditMode = false;

    public TeacherAnnouncementsPage()
    {
        InitializeComponent();
        _authManager = AppServiceProvider.GetService<AuthManager>()
            ?? throw new InvalidOperationException("AuthManager not found");
        
        AnnouncementsCollectionView.ItemsSource = _filteredAnnouncements;
        LoadSampleData();
    }

    private void LoadSampleData()
    {
        // Sample announcements data
        var announcements = new[]
        {
            new AnnouncementItem
            {
                Id = Guid.NewGuid(),
                Subject = "Midterm Exam Schedule",
                Message = "The midterm examinations will be held from December 10-15. Please review the detailed schedule posted on the course portal. Make sure to bring valid ID and writing materials.",
                Priority = "Important",
                TargetAudience = "All Students",
                CreatedAt = DateTime.Now.AddDays(-2),
                PriorityColor = Color.FromArgb("#F59E0B")
            },
            new AnnouncementItem
            {
                Id = Guid.NewGuid(),
                Subject = "Library Closure Notice",
                Message = "The university library will be closed for maintenance on December 5-6. All borrowed materials should be returned by December 4.",
                Priority = "Normal",
                TargetAudience = "All Students",
                CreatedAt = DateTime.Now.AddDays(-5),
                PriorityColor = Color.FromArgb("#10B981")
            },
            new AnnouncementItem
            {
                Id = Guid.NewGuid(),
                Subject = "Class Cancellation - December 8",
                Message = "Due to a faculty meeting, all classes on December 8 are cancelled. Make-up classes will be scheduled next week.",
                Priority = "Urgent",
                TargetAudience = "Specific Class",
                CreatedAt = DateTime.Now.AddHours(-6),
                PriorityColor = Color.FromArgb("#EF4444")
            }
        };

        foreach (var announcement in announcements)
        {
            _allAnnouncements.Add(announcement);
            _filteredAnnouncements.Add(announcement);
        }

        UpdateStatistics();
    }

    private void UpdateStatistics()
    {
        var total = _allAnnouncements.Count;
        var weekAgo = DateTime.Now.AddDays(-7);
        var thisWeek = _allAnnouncements.Count(a => a.CreatedAt >= weekAgo);
        var important = _allAnnouncements.Count(a => a.Priority.Equals("Important", StringComparison.OrdinalIgnoreCase) || 
                                                     a.Priority.Equals("Urgent", StringComparison.OrdinalIgnoreCase));

        TotalCountLabel.Text = total.ToString();
        WeekCountLabel.Text = thisWeek.ToString();
        ImportantCountLabel.Text = important.ToString();
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        ApplyFilters(e.NewTextValue);
    }

    private void ApplyFilters(string? searchText = null)
    {
        searchText ??= SearchEntry.Text;
        var query = _allAnnouncements.AsEnumerable();

        // Apply priority filter
        if (_currentFilter != "all")
        {
            query = query.Where(a => a.Priority.Equals(_currentFilter, StringComparison.OrdinalIgnoreCase));
        }

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var search = searchText.ToLower();
            query = query.Where(a =>
                a.Subject.ToLower().Contains(search) ||
                a.Message.ToLower().Contains(search) ||
                a.TargetAudience.ToLower().Contains(search));
        }

        _filteredAnnouncements.Clear();
        foreach (var announcement in query)
        {
            _filteredAnnouncements.Add(announcement);
        }
    }

    private void UpdateFilterButtons(string activeFilter)
    {
        _currentFilter = activeFilter;

        // Reset all buttons
        FilterAllBtn.BackgroundColor = Color.FromArgb("#F3F4F6");
        FilterAllBtn.TextColor = Color.FromArgb("#6B7280");
        FilterImportantBtn.BackgroundColor = Color.FromArgb("#F3F4F6");
        FilterImportantBtn.TextColor = Color.FromArgb("#6B7280");
        FilterUrgentBtn.BackgroundColor = Color.FromArgb("#F3F4F6");
        FilterUrgentBtn.TextColor = Color.FromArgb("#6B7280");

        // Highlight active button
        switch (activeFilter)
        {
            case "all":
                FilterAllBtn.BackgroundColor = Color.FromArgb("#059669");
                FilterAllBtn.TextColor = Colors.White;
                break;
            case "important":
                FilterImportantBtn.BackgroundColor = Color.FromArgb("#059669");
                FilterImportantBtn.TextColor = Colors.White;
                break;
            case "urgent":
                FilterUrgentBtn.BackgroundColor = Color.FromArgb("#059669");
                FilterUrgentBtn.TextColor = Colors.White;
                break;
        }

        ApplyFilters();
    }

    private void OnFilterAllClicked(object? sender, EventArgs e)
    {
        UpdateFilterButtons("all");
    }

    private void OnFilterImportantClicked(object? sender, EventArgs e)
    {
        UpdateFilterButtons("important");
    }

    private void OnFilterUrgentClicked(object? sender, EventArgs e)
    {
        UpdateFilterButtons("urgent");
    }

    private void OnNewAnnouncementClicked(object? sender, EventArgs e)
    {
        _isEditMode = false;
        _selectedAnnouncement = null;
        ModalTitleLabel.Text = "Create New Announcement";
        SubmitButton.Text = "Post Announcement";
        ClearForm();
        ModalOverlay.IsVisible = true;
    }

    private void OnEditAnnouncementClicked(object? sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is AnnouncementItem announcement)
        {
            _isEditMode = true;
            _selectedAnnouncement = announcement;
            ModalTitleLabel.Text = "Edit Announcement";
            SubmitButton.Text = "Update Announcement";
            
            // Populate form with existing data
            SubjectEntry.Text = announcement.Subject;
            MessageEditor.Text = announcement.Message;
            TargetPicker.SelectedItem = announcement.TargetAudience;
            
            // Show subject code field if target is "Specific Class"
            if (announcement.TargetAudience == "Specific Class")
            {
                SubjectCodeSection.IsVisible = true;
                // Extract subject code from message or set empty
                SubjectCodeEntry.Text = string.Empty;
            }
            
            ModalOverlay.IsVisible = true;
        }
    }

    private async void OnDeleteAnnouncementClicked(object? sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is AnnouncementItem announcement)
        {
            var confirm = await DisplayAlert("Delete Announcement", 
                $"Are you sure you want to delete '{announcement.Subject}'?", 
                "Delete", "Cancel");

            if (confirm)
            {
                _allAnnouncements.Remove(announcement);
                _filteredAnnouncements.Remove(announcement);
                UpdateStatistics();
                await DisplayAlert("Success", "Announcement deleted successfully.", "OK");
            }
        }
    }

    private void OnCloseModalTapped(object? sender, EventArgs e)
    {
        ModalOverlay.IsVisible = false;
        ClearForm();
    }

    private void OnCancelAnnouncementClicked(object? sender, EventArgs e)
    {
        ModalOverlay.IsVisible = false;
        ClearForm();
    }

    private async void OnSubmitAnnouncementClicked(object? sender, EventArgs e)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(SubjectEntry.Text))
        {
            await DisplayAlert("Validation", "Please enter a subject.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(MessageEditor.Text))
        {
            await DisplayAlert("Validation", "Please enter a message.", "OK");
            return;
        }

        try
        {
            var subject = SubjectEntry.Text.Trim();
            var message = MessageEditor.Text.Trim();
            var target = TargetPicker.SelectedItem?.ToString() ?? "All Students";
            var priority = "Normal";

            if (_isEditMode && _selectedAnnouncement != null)
            {
                // Update existing announcement
                _selectedAnnouncement.Subject = subject;
                _selectedAnnouncement.Message = message;
                _selectedAnnouncement.TargetAudience = target;
                _selectedAnnouncement.Priority = priority;
                _selectedAnnouncement.PriorityColor = GetPriorityColor(priority);

                await DisplayAlert("Success", "Announcement updated successfully.", "OK");
            }
            else
            {
                // Create new announcement
                var newAnnouncement = new AnnouncementItem
                {
                    Id = Guid.NewGuid(),
                    Subject = subject,
                    Message = message,
                    TargetAudience = target,
                    Priority = priority,
                    CreatedAt = DateTime.Now,
                    PriorityColor = GetPriorityColor(priority)
                };

                _allAnnouncements.Insert(0, newAnnouncement);
                
                // Apply current filter
                if (_currentFilter == "all" || 
                    (_currentFilter == "important" && priority == "Important") ||
                    (_currentFilter == "urgent" && priority == "Urgent"))
                {
                    _filteredAnnouncements.Insert(0, newAnnouncement);
                }

                await DisplayAlert("Success", "Announcement posted successfully.", "OK");
            }

            UpdateStatistics();
            ModalOverlay.IsVisible = false;
            ClearForm();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"TeacherAnnouncementsPage: Error submitting announcement - {ex.Message}");
            await DisplayAlert("Error", "Failed to save announcement. Please try again.", "OK");
        }
    }

    private void ClearForm()
    {
        SubjectEntry.Text = string.Empty;
        MessageEditor.Text = string.Empty;
        TargetPicker.SelectedIndex = 0;
        SubjectCodeEntry.Text = string.Empty;
        SubjectCodeSection.IsVisible = false;
        _isEditMode = false;
        _selectedAnnouncement = null;
    }

    private void OnTargetPickerChanged(object? sender, EventArgs e)
    {
        // Null check to prevent crashes during initialization
        if (SubjectCodeSection == null || SubjectCodeEntry == null || TargetPicker == null)
            return;

        if (TargetPicker.SelectedItem?.ToString() == "Specific Class")
        {
            SubjectCodeSection.IsVisible = true;
        }
        else
        {
            SubjectCodeSection.IsVisible = false;
            SubjectCodeEntry.Text = string.Empty;
        }
    }

    private Color GetPriorityColor(string priority)
    {
        return priority.ToLower() switch
        {
            "urgent" => Color.FromArgb("#EF4444"),
            "important" => Color.FromArgb("#F59E0B"),
            _ => Color.FromArgb("#10B981")
        };
    }

    private async void OnDashboardTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//TeacherHomePage");
    }

    private async void OnClassesTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//TeacherClassesPage");
    }

    private async void OnMessagesTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//TeacherMessagesPage");
    }

    private async void OnTicketsTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//TeacherTicketsPage");
    }

    private async void OnLogoutTapped(object? sender, EventArgs e)
    {
        var confirm = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");
        if (confirm)
        {
            _authManager.ClearAuthentication();
            await Shell.Current.GoToAsync("//MainPage");
        }
    }
}

// Helper class for announcement data
[SupportedOSPlatform("windows10.0.17763.0")]
[SupportedOSPlatform("android21.0")]
public class AnnouncementItem
{
    public Guid Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Priority { get; set; } = "Normal";
    public string TargetAudience { get; set; } = "All Students";
    public DateTime CreatedAt { get; set; }
    public Color PriorityColor { get; set; } = Color.FromArgb("#10B981");
}
