using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;
using MauiAppIT13.Models;
using MauiAppIT13.Services;
using MauiAppIT13.Utils;
using Microsoft.Maui.ApplicationModel;

namespace MauiAppIT13.Pages.Teacher;

[SupportedOSPlatform("windows10.0.17763.0")]
[SupportedOSPlatform("android21.0")]
public partial class TeacherAnnouncementsPage : ContentPage
{
    private readonly AnnouncementService _announcementService;
    private readonly AuthManager _authManager;
    private ObservableCollection<AnnouncementItem> _allAnnouncements = new();
    private ObservableCollection<AnnouncementItem> _filteredAnnouncements = new();
    private AnnouncementItem? _selectedAnnouncement;
    private string _currentFilter = "all";
    private bool _isEditMode = false;
    private bool _isLoading = false;

    public TeacherAnnouncementsPage()
    {
        InitializeComponent();
        _announcementService = AppServiceProvider.GetService<AnnouncementService>()
            ?? throw new InvalidOperationException("AnnouncementService not found");
        _authManager = AppServiceProvider.GetService<AuthManager>()
            ?? throw new InvalidOperationException("AuthManager not found");

        AnnouncementsCollectionView.ItemsSource = _filteredAnnouncements;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAnnouncementsAsync();
    }

    private async Task LoadAnnouncementsAsync()
    {
        if (_isLoading)
            return;

        var currentUser = _authManager.CurrentUser;
        if (currentUser == null)
        {
            await DisplayAlert("Authentication Required", "Please log in again to manage announcements.", "OK");
            return;
        }

        _isLoading = true;
        try
        {
            var announcements = await _announcementService.GetAnnouncementsAsync(150);
            var teacherAnnouncements = announcements
                .Where(a => TeacherCanViewAnnouncement(a, currentUser.Id))
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => MapToAnnouncementItem(a, currentUser.Id))
                .ToList();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                _allAnnouncements.Clear();
                foreach (var announcement in teacherAnnouncements)
                {
                    _allAnnouncements.Add(announcement);
                }

                ApplyFilters();
                UpdateStatistics();
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"TeacherAnnouncementsPage: Failed to load announcements - {ex.Message}");
            await DisplayAlert("Error", "Failed to load announcements. Please try again.", "OK");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private static bool TeacherCanViewAnnouncement(Announcement announcement, Guid teacherId)
    {
        var visibility = (announcement.Visibility ?? "all").ToLowerInvariant();
        if (IsAnnouncementOwnedByTeacher(announcement, teacherId))
            return true;

        return visibility is "all" or "advisers";
    }

    private static bool IsAnnouncementOwnedByTeacher(Announcement announcement, Guid teacherId) =>
        announcement.AuthorId == teacherId || announcement.CreatedBy == teacherId;

    private AnnouncementItem MapToAnnouncementItem(Announcement announcement, Guid teacherId)
    {
        var visibility = (announcement.Visibility ?? "all").ToLowerInvariant();
        var isOwned = IsAnnouncementOwnedByTeacher(announcement, teacherId);
        return new AnnouncementItem
        {
            Id = announcement.Id,
            Subject = announcement.Title,
            Message = announcement.Content,
            TargetAudience = GetTargetLabelFromVisibility(visibility),
            CreatedAt = announcement.CreatedAt,
            TargetColor = GetTargetColor(visibility),
            Visibility = visibility,
            IsPublished = announcement.IsPublished,
            AuthorName = announcement.AuthorName ?? "Unknown",
            IsOwnedByCurrentUser = isOwned
        };
    }

    private void UpdateStatistics()
    {
        var total = _allAnnouncements.Count;
        var weekAgo = DateTime.Now.AddDays(-7);
        var thisWeek = _allAnnouncements.Count(a => a.CreatedAt >= weekAgo);
        var studentsTargeted = _allAnnouncements.Count(a => a.Visibility.Equals("students", StringComparison.OrdinalIgnoreCase));

        TotalCountLabel.Text = total.ToString();
        WeekCountLabel.Text = thisWeek.ToString();
        ImportantCountLabel.Text = studentsTargeted.ToString();
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        ApplyFilters(e.NewTextValue);
    }

    private void ApplyFilters(string? searchText = null)
    {
        searchText ??= SearchEntry.Text;
        var query = _allAnnouncements.AsEnumerable();

        // Apply visibility filter
        query = _currentFilter switch
        {
            "students" => query.Where(a => a.Visibility.Equals("students", StringComparison.OrdinalIgnoreCase)),
            "advisers" => query.Where(a => a.Visibility.Equals("advisers", StringComparison.OrdinalIgnoreCase)),
            _ => query
        };

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
        FilterStudentsBtn.BackgroundColor = Color.FromArgb("#F3F4F6");
        FilterStudentsBtn.TextColor = Color.FromArgb("#6B7280");
        FilterAdvisersBtn.BackgroundColor = Color.FromArgb("#F3F4F6");
        FilterAdvisersBtn.TextColor = Color.FromArgb("#6B7280");

        // Highlight active button
        switch (activeFilter)
        {
            case "all":
                FilterAllBtn.BackgroundColor = Color.FromArgb("#059669");
                FilterAllBtn.TextColor = Colors.White;
                break;
            case "students":
                FilterStudentsBtn.BackgroundColor = Color.FromArgb("#059669");
                FilterStudentsBtn.TextColor = Colors.White;
                break;
            case "advisers":
                FilterAdvisersBtn.BackgroundColor = Color.FromArgb("#059669");
                FilterAdvisersBtn.TextColor = Colors.White;
                break;
        }

        ApplyFilters();
    }

    private void OnFilterAllClicked(object? sender, EventArgs e)
    {
        UpdateFilterButtons("all");
    }

    private void OnFilterStudentsClicked(object? sender, EventArgs e) => UpdateFilterButtons("students");

    private void OnFilterAdvisersClicked(object? sender, EventArgs e) => UpdateFilterButtons("advisers");

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
            TargetPicker.SelectedIndex = GetTargetPickerIndex(announcement.Visibility);
            PublishSwitch.IsToggled = announcement.IsPublished;
            
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
                var success = await _announcementService.DeleteAnnouncementAsync(announcement.Id);
                if (!success)
                {
                    await DisplayAlert("Error", "Failed to delete announcement. Please try again.", "OK");
                    return;
                }

                await LoadAnnouncementsAsync();
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
            var visibility = GetVisibilityFromPicker();
            var isPublished = PublishSwitch.IsToggled;

            var currentUser = _authManager.CurrentUser;
            if (currentUser == null)
            {
                await DisplayAlert("Authentication Required", "Please log in again to continue.", "OK");
                return;
            }

            bool success;

            if (_isEditMode && _selectedAnnouncement != null)
            {
                success = await _announcementService.UpdateAnnouncementAsync(
                    _selectedAnnouncement.Id,
                    subject,
                    message,
                    visibility,
                    isPublished,
                    currentUser.Id);
            }
            else
            {
                var newId = await _announcementService.CreateAnnouncementAsync(
                    subject,
                    message,
                    visibility,
                    isPublished,
                    currentUser.Id,
                    currentUser.Id);

                success = newId.HasValue;
            }

            if (!success)
            {
                await DisplayAlert("Error", "Failed to save announcement. Please try again.", "OK");
                return;
            }

            var successMessage = _isEditMode ? "Announcement updated successfully." : "Announcement posted successfully.";

            await LoadAnnouncementsAsync();
            ModalOverlay.IsVisible = false;
            ClearForm();
            await DisplayAlert("Success", successMessage, "OK");
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
        PublishSwitch.IsToggled = true;
        _isEditMode = false;
        _selectedAnnouncement = null;
    }

    private static string GetVisibilityFromPickerIndex(int index) => index switch
    {
        1 => "students",
        2 => "advisers",
        _ => "all"
    };

    private string GetVisibilityFromPicker() => GetVisibilityFromPickerIndex(TargetPicker.SelectedIndex);

    private static string GetTargetLabelFromVisibility(string visibility) => visibility switch
    {
        "students" => "Students",
        "advisers" => "Advisers",
        _ => "All Users"
    };

    private static int GetTargetPickerIndex(string visibility) => visibility switch
    {
        "students" => 1,
        "advisers" => 2,
        _ => 0
    };

    private static Color GetTargetColor(string visibility) => visibility switch
    {
        "students" => Color.FromArgb("#2563EB"),
        "advisers" => Color.FromArgb("#D97706"),
        _ => Color.FromArgb("#059669")
    };

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
    public string TargetAudience { get; set; } = "All Users";
    public DateTime CreatedAt { get; set; }
    public Color TargetColor { get; set; } = Color.FromArgb("#059669");
    public string Visibility { get; set; } = "all";
    public bool IsPublished { get; set; }
    public string AuthorName { get; set; } = "Unknown";
    public bool IsOwnedByCurrentUser { get; set; }
}
