using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Versioning;
using System.Linq;
using Microsoft.Maui.Dispatching;
using MauiAppIT13.Database;
using MauiAppIT13.Models;
using MauiAppIT13.Services;
using MauiAppIT13.Utils;

#pragma warning disable CA1416 // MAUI elements are supported on configured targets

namespace MauiAppIT13.Pages.Teacher;

[SupportedOSPlatform("windows10.0.17763.0")]
public partial class TeacherClassesPage : ContentPage
{
    private readonly ClassService _classService;
    private readonly AuthManager _authManager;
    private readonly DbConnection _dbConnection;
    private readonly ObservableCollection<ClassModel> _classes = new();
    private readonly List<ClassModel> _allClasses = new();
    private string _currentFilter = "All";

    public TeacherClassesPage()
    {
        InitializeComponent();
        _classService = AppServiceProvider.GetService<ClassService>() ?? throw new InvalidOperationException("ClassService not found");
        _authManager = AppServiceProvider.GetService<AuthManager>() ?? new AuthManager();
        _dbConnection = AppServiceProvider.GetService<DbConnection>() ?? throw new InvalidOperationException("DbConnection not found");
        CoursesCollectionView.ItemsSource = _classes;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = LoadClassesAsync();
    }

    private async void OnDashboardTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//TeacherHomePage", animate: false);
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

    private async void OnViewClassClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is ClassModel classModel)
        {
            await Shell.Current.GoToAsync($"//TeacherClassDetailsPage?classId={classModel.Id}", animate: false);
        }
    }

    private async Task LoadClassesAsync()
    {
        try
        {
            var teacher = _authManager.CurrentUser;
            if (teacher is null)
            {
                teacher = await _dbConnection.GetUserByEmailAsync("teacher@university.edu");
                if (teacher is null)
                {
                    await DisplayAlert("Error", "Unable to determine the signed-in teacher.", "OK");
                    return;
                }
            }

            var classes = await _classService.GetTeacherClassesAsync(teacher.Id);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _allClasses.Clear();
                foreach (var classModel in classes)
                {
                    _allClasses.Add(classModel);
                }

                ApplyFilter();
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load classes: {ex.Message}", "OK");
        }
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

    private void OnFilterChanged(object sender, EventArgs e)
    {
        if (sender is not Picker picker || picker.SelectedIndex < 0)
            return;

        _currentFilter = picker.SelectedItem?.ToString() ?? "All";
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        IEnumerable<ClassModel> filtered = _currentFilter switch
        {
            "Active" => FilterByStatus("Active"),
            "Inactive" => FilterByStatus("Inactive"),
            _ => _allClasses
        };

        _classes.Clear();
        foreach (var classModel in filtered)
        {
            _classes.Add(classModel);
        }
    }

    private IEnumerable<ClassModel> FilterByStatus(string status) =>
        _allClasses.Where(classModel =>
            classModel.Status is not null &&
            classModel.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
}
