using System.Collections.ObjectModel;
using System.Linq;
using MauiAppIT13.Models;
using MauiAppIT13.Services;
using MauiAppIT13.Utils;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

#pragma warning disable CA1416 // Validate platform compatibility

namespace MauiAppIT13.Pages.Student;

public partial class AnnouncementsPage : ContentPage
{
    private readonly AnnouncementService _announcementService;
    private readonly AuthManager _authManager;
    private readonly ObservableCollection<Announcement> _allAnnouncements = new();
    private readonly ObservableCollection<Announcement> _filteredAnnouncements = new();
    private readonly HashSet<Guid> _viewedAnnouncements = new();
    private string _currentFilter = "All";
    private string _searchText = string.Empty;
    private bool _isLoading;

    public AnnouncementsPage()
    {
        InitializeComponent();

        _announcementService = AppServiceProvider.GetService<AnnouncementService>() ?? throw new InvalidOperationException("AnnouncementService not available");

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
            var currentUserId = _authManager.CurrentUser?.Id;
            if (!currentUserId.HasValue)
            {
                await DisplayAlert("Error", "User not authenticated", "OK");
                return;
            }

            var announcements = await _announcementService.GetStudentAnnouncementsAsync(currentUserId.Value, 150, currentUserId);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                _allAnnouncements.Clear();
                foreach (var announcement in announcements)
                {
                    _allAnnouncements.Add(announcement);
                }

                ApplyFilters();
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
        IEnumerable<Announcement> query = _allAnnouncements;

        if (_currentFilter == "Announcements")
        {
            query = query.Where(a => a.Visibility.Equals("all", StringComparison.OrdinalIgnoreCase));
        }
        else if (_currentFilter == "Reminders")
        {
            query = query.Where(a => a.Title?.Contains("Reminder", StringComparison.OrdinalIgnoreCase) ?? false);
        }

        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            var lowered = _searchText.ToLowerInvariant();
            query = query.Where(a =>
                (a.Title?.ToLowerInvariant().Contains(lowered) ?? false) ||
                (a.Content?.ToLowerInvariant().Contains(lowered) ?? false));
        }

        var filtered = query.ToList();

        _filteredAnnouncements.Clear();
        foreach (var announcement in filtered)
        {
            _filteredAnnouncements.Add(announcement);
        }
    }

    private void ChangeFilter(string filter)
    {
        if (_currentFilter == filter)
            return;

        _currentFilter = filter;
        ApplyFilters();
    }

    private void OnFilterChanged(object? sender, EventArgs e)
    {
        if (sender is not Picker picker || picker.SelectedIndex < 0)
            return;

        var filter = picker.SelectedItem?.ToString() ?? "All";
        ChangeFilter(filter);
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        _searchText = e.NewTextValue ?? string.Empty;
        ApplyFilters();
    }

    private async void OnClassesTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//StudentClassesPage", animate: false);
    }

    private async void OnAnnouncementTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not Announcement announcement)
            return;

        await RecordAnnouncementViewAsync(announcement);
    }

    private async Task RecordAnnouncementViewAsync(Announcement announcement)
    {
        var currentUser = _authManager.CurrentUser;
        if (currentUser == null)
            return;

        if (!_viewedAnnouncements.Add(announcement.Id))
            return;

        var recorded = await _announcementService.RecordViewAsync(announcement.Id, currentUser.Id);
        if (recorded)
        {
            announcement.ViewCount += 1;
            ApplyFilters();
        }
    }

    private async void OnProfileTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//ProfilePage", animate: false);
    }

    private async void OnHomeTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//HomePage", animate: false);
    }

    private async void OnMessagesTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//MessagesPage", animate: false);
    }

    private async void OnTicketsTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//TicketsPage", animate: false);
    }

    private async void OnLogoutTapped(object? sender, EventArgs e)
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
}