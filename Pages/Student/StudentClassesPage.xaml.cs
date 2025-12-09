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
public partial class StudentClassesPage : ContentPage
{
    private readonly ClassService? _classService;
    private readonly AuthManager _authManager;
    private readonly DbConnection? _dbConnection;
    private readonly ObservableCollection<ClassModel> _classes = new();
    private bool _hasAppeared;

    public StudentClassesPage()
    {
        InitializeComponent();

        _dbConnection = AppServiceProvider.GetService<DbConnection>();
        _classService = AppServiceProvider.GetService<ClassService>();
        _authManager = AppServiceProvider.GetService<AuthManager>() ?? new AuthManager();

        ClassesCollectionView.ItemsSource = _classes;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_hasAppeared)
        {
            _hasAppeared = true;
            await LoadClassesAsync();
        }
    }

    private async Task LoadClassesAsync()
    {
        if (_classService is null || _dbConnection is null)
        {
            await DisplayAlert("Error", "Service unavailable. Please try again later.", "OK");
            return;
        }

        try
        {
            var currentUser = _authManager.CurrentUser;
            if (currentUser?.Id == null)
            {
                await DisplayAlert("Error", "User not authenticated.", "OK");
                return;
            }

            // Get student's enrolled classes
            var classes = await _classService.GetStudentClassesAsync(currentUser.Id);
            
            _classes.Clear();
            foreach (var classItem in classes)
            {
                _classes.Add(classItem);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load classes: {ex.Message}", "OK");
        }
    }

    private async void OnViewAssignmentsClicked(object sender, EventArgs e)
    {
        if (sender is not Button button || button.CommandParameter is not ClassModel classModel)
        {
            return;
        }

        await Navigation.PushAsync(new StudentClassDetailPage(classModel.Id, "Assignments"), false);
    }

    private async void OnViewGradesClicked(object sender, EventArgs e)
    {
        if (sender is not Button button || button.CommandParameter is not ClassModel classModel)
        {
            return;
        }

        await Navigation.PushAsync(new StudentClassDetailPage(classModel.Id, "Grades"), false);
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
