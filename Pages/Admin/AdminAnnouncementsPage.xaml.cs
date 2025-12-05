using System.Collections.ObjectModel;
using System.Linq;
using MauiAppIT13.Models;
using MauiAppIT13.Services;
using MauiAppIT13.Utils;
using Microsoft.Maui.ApplicationModel;

namespace MauiAppIT13.Pages.Admin;

public partial class AdminAnnouncementsPage : ContentPage
{
    private readonly AnnouncementService _announcementService;
    private readonly AuthManager _authManager;
    private readonly ObservableCollection<Announcement> _allAnnouncements = new();
    private readonly ObservableCollection<Announcement> _filteredAnnouncements = new();
    private Announcement? _editingAnnouncement;
    private bool _isLoading;
    private string _searchText = string.Empty;

    public AdminAnnouncementsPage()
    {
        InitializeComponent();

        _announcementService = AppServiceProvider.GetService<AnnouncementService>() ?? new AnnouncementService();
        _authManager = AppServiceProvider.GetService<AuthManager>() ?? new AuthManager();

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

        _isLoading = true;
        try
        {
            var announcements = await _announcementService.GetAnnouncementsAsync();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                _allAnnouncements.Clear();
                foreach (var announcement in announcements)
                {
                    _allAnnouncements.Add(announcement);
                }

                ApplyFilters();
                UpdateStats();
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load announcements: {ex.Message}", "OK");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void ApplyFilters()
    {
        var query = _allAnnouncements.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            var lowered = _searchText.ToLowerInvariant();
            query = query.Where(a =>
                (a.Title?.ToLowerInvariant().Contains(lowered) ?? false) ||
                (a.Content?.ToLowerInvariant().Contains(lowered) ?? false) ||
                a.VisibilityLabel.ToLowerInvariant().Contains(lowered));
        }

        _filteredAnnouncements.Clear();
        foreach (var announcement in query)
        {
            _filteredAnnouncements.Add(announcement);
        }
    }

    private void UpdateStats()
    {
        TotalAnnouncementsLabel.Text = _allAnnouncements.Count.ToString();
        PublishedCountLabel.Text = _allAnnouncements.Count(a => a.IsPublished).ToString();
        DraftCountLabel.Text = _allAnnouncements.Count(a => !a.IsPublished).ToString();
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        _searchText = e.NewTextValue ?? string.Empty;
        ApplyFilters();
    }

    private async void OnRefreshAnnouncementsClicked(object? sender, EventArgs e)
    {
        await LoadAnnouncementsAsync();
    }

    private void OnNewAnnouncementClicked(object? sender, EventArgs e)
    {
        _editingAnnouncement = null;
        ModalTitleLabel.Text = "New Announcement";
        SaveAnnouncementButton.Text = "Publish";
        AnnouncementTitleEntry.Text = string.Empty;
        AnnouncementContentEditor.Text = string.Empty;
        VisibilityPicker.SelectedIndex = 0;
        PublishedSwitch.IsToggled = true;
        AnnouncementModal.IsVisible = true;
    }

    private void OnCloseModalClicked(object? sender, EventArgs e) => HideModal();

    private void OnCancelAnnouncementClicked(object? sender, EventArgs e) => HideModal();

    private void HideModal()
    {
        AnnouncementModal.IsVisible = false;
        AnnouncementTitleEntry.Text = string.Empty;
        AnnouncementContentEditor.Text = string.Empty;
        VisibilityPicker.SelectedIndex = 0;
        PublishedSwitch.IsToggled = true;
        _editingAnnouncement = null;
    }

    private async void OnSaveAnnouncementClicked(object? sender, EventArgs e)
    {
        string title = AnnouncementTitleEntry.Text?.Trim() ?? string.Empty;
        string content = AnnouncementContentEditor.Text?.Trim() ?? string.Empty;
        string visibility = GetVisibilityValue(VisibilityPicker.SelectedIndex);
        bool isPublished = PublishedSwitch.IsToggled;

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
        {
            await DisplayAlert("Validation Error", "Title and content are required.", "OK");
            return;
        }

        var currentUser = _authManager.CurrentUser;
        if (currentUser == null)
        {
            await DisplayAlert("Error", "You must be logged in as an admin to perform this action.", "OK");
            return;
        }

        bool success;
        if (_editingAnnouncement == null)
        {
            var newId = await _announcementService.CreateAnnouncementAsync(
                title,
                content,
                visibility,
                isPublished,
                currentUser.Id,
                currentUser.Id);

            success = newId.HasValue;
        }
        else
        {
            success = await _announcementService.UpdateAnnouncementAsync(
                _editingAnnouncement.Id,
                title,
                content,
                visibility,
                isPublished,
                currentUser.Id);
        }

        if (!success)
        {
            await DisplayAlert("Error", "Failed to save announcement. Please try again.", "OK");
            return;
        }

        HideModal();
        await LoadAnnouncementsAsync();
        await DisplayAlert("Success", "Announcement saved successfully.", "OK");
    }

    private static string GetVisibilityValue(int selectedIndex) => selectedIndex switch
    {
        1 => "students",
        2 => "advisers",
        _ => "all"
    };

    private static int GetPickerIndexForVisibility(string? visibility) => visibility?.ToLowerInvariant() switch
    {
        "students" => 1,
        "advisers" => 2,
        _ => 0
    };

    private void PopulateModalForEdit(Announcement announcement)
    {
        _editingAnnouncement = announcement;
        ModalTitleLabel.Text = "Edit Announcement";
        SaveAnnouncementButton.Text = "Save Changes";
        AnnouncementTitleEntry.Text = announcement.Title;
        AnnouncementContentEditor.Text = announcement.Content;
        VisibilityPicker.SelectedIndex = GetPickerIndexForVisibility(announcement.Visibility);
        PublishedSwitch.IsToggled = announcement.IsPublished;
        AnnouncementModal.IsVisible = true;
    }

    private async void OnEditAnnouncementClicked(object? sender, EventArgs e)
    {
        var announcement = GetAnnouncementFromSender(sender);
        if (announcement == null)
            return;

        PopulateModalForEdit(announcement);
    }

    private async void OnDeleteAnnouncementClicked(object? sender, EventArgs e)
    {
        var announcement = GetAnnouncementFromSender(sender);
        if (announcement == null)
            return;

        bool confirm = await DisplayAlert(
            "Delete Announcement",
            "Are you sure you want to delete this announcement?",
            "Delete",
            "Cancel");

        if (!confirm)
            return;

        var success = await _announcementService.DeleteAnnouncementAsync(announcement.Id);
        if (!success)
        {
            await DisplayAlert("Error", "Failed to delete announcement. Please try again.", "OK");
            return;
        }

        await LoadAnnouncementsAsync();
        await DisplayAlert("Success", "Announcement deleted.", "OK");
    }

    private static Announcement? GetAnnouncementFromSender(object? sender)
    {
        if (sender is Button button && button.CommandParameter is Announcement announcement)
        {
            return announcement;
        }

        return null;
    }

    private async void OnDashboardTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//AdminHomePage");
    }

    private async void OnUsersTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("AdminUsersPage");
    }

    private async void OnTicketsTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("AdminTicketsPage");
    }

    private async void OnReportsTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("AdminReportsPage");
    }

    private async void OnLogoutTapped(object? sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");
        if (confirm)
        {
            await Shell.Current.GoToAsync("//MainPage");
        }
    }
}
