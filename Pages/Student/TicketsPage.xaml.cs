using MauiAppIT13.Services;
using MauiAppIT13.Utils;
using System.Runtime.Versioning;

namespace MauiAppIT13.Pages.Student;

[SupportedOSPlatform("windows10.0.17763.0")]
[SupportedOSPlatform("android21.0")]
public partial class TicketsPage : ContentPage
{
    private readonly TicketService _ticketService;
    private readonly AuthManager _authManager;
    private Guid _currentUserId;

    public TicketsPage()
    {
        InitializeComponent();
        
        var dbConnection = AppServiceProvider.GetService<MauiAppIT13.Database.DbConnection>();
        _ticketService = AppServiceProvider.GetService<TicketService>() ?? new TicketService(dbConnection ?? throw new InvalidOperationException("DbConnection not found"));
        _authManager = AppServiceProvider.GetService<AuthManager>() ?? new AuthManager();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        System.Diagnostics.Debug.WriteLine("TicketsPage: OnAppearing called");
        var currentUser = _authManager.CurrentUser;
        if (currentUser != null)
        {
            _currentUserId = currentUser.Id;
            _ = LoadTickets();
        }
    }

    private async Task LoadTickets()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"TicketsPage: Loading tickets for student {_currentUserId}");
            var tickets = await _ticketService.GetStudentTicketsAsync(_currentUserId);
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                TicketsCollectionView.ItemsSource = tickets;
                System.Diagnostics.Debug.WriteLine($"TicketsPage: Loaded {tickets.Count} tickets");
                
                // Update status counts
                int openCount = tickets.Count(t => t.Status?.ToLower() == "open");
                int inProgressCount = tickets.Count(t => t.Status?.ToLower() == "in_progress");
                int resolvedCount = tickets.Count(t => t.Status?.ToLower() == "resolved");
                
                OpenCountLabel.Text = openCount.ToString();
                InProgressCountLabel.Text = inProgressCount.ToString();
                ResolvedCountLabel.Text = resolvedCount.ToString();
                
                System.Diagnostics.Debug.WriteLine($"TicketsPage: Open={openCount}, InProgress={inProgressCount}, Resolved={resolvedCount}");
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TicketsPage: Error loading tickets - {ex.Message}");
        }
    }

    private async void OnProfileTapped(object? sender, EventArgs e)
    {
        try
        {
            await Shell.Current.GoToAsync("//ProfilePage");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TicketsPage: Error navigating to profile - {ex.Message}");
        }
    }

    private async void OnHomeTapped(object? sender, EventArgs e)
    {
        try
        {
            await Shell.Current.GoToAsync("//HomePage");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TicketsPage: Error navigating home - {ex.Message}");
        }
    }

    private async void OnMessagesTapped(object? sender, EventArgs e)
    {
        try
        {
            await Shell.Current.GoToAsync("//MessagesPage");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TicketsPage: Error navigating to messages - {ex.Message}");
        }
    }

    private async void OnAnnouncementsTapped(object? sender, EventArgs e)
    {
        try
        {
            await Shell.Current.GoToAsync("//AnnouncementsPage");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TicketsPage: Error navigating to announcements - {ex.Message}");
        }
    }

    private void OnNewTicketClicked(object? sender, EventArgs e)
    {
        ModalOverlay.IsVisible = true;
    }

    private void OnCloseModalTapped(object? sender, EventArgs e)
    {
        ModalOverlay.IsVisible = false;
        ClearForm();
    }

    private void OnOverlayTapped(object? sender, EventArgs e)
    {
        ModalOverlay.IsVisible = false;
        ClearForm();
    }

    private void OnCancelTicketClicked(object? sender, EventArgs e)
    {
        ModalOverlay.IsVisible = false;
        ClearForm();
    }

    private async void OnSubmitTicketClicked(object? sender, EventArgs e)
    {
        try
        {
            string title = TitleEntry.Text?.Trim() ?? string.Empty;
            string category = CategoryPicker.SelectedIndex > 0 ? CategoryPicker.Items[CategoryPicker.SelectedIndex] : string.Empty;
            string priority = PriorityPicker.SelectedIndex > 0 ? PriorityPicker.Items[PriorityPicker.SelectedIndex].ToLower() : string.Empty;
            string description = DescriptionEditor.Text?.Trim() ?? string.Empty;

            // Validation
            if (string.IsNullOrEmpty(title))
            {
                await DisplayAlert("Error", "Please enter a title for your ticket.", "OK");
                return;
            }

            if (CategoryPicker.SelectedIndex <= 0)
            {
                await DisplayAlert("Error", "Please select a category.", "OK");
                return;
            }

            if (PriorityPicker.SelectedIndex <= 0)
            {
                await DisplayAlert("Error", "Please select a priority level.", "OK");
                return;
            }

            if (string.IsNullOrEmpty(description))
            {
                await DisplayAlert("Error", "Please provide a description of your issue.", "OK");
                return;
            }

            // Submit ticket to database
            bool success = await _ticketService.CreateTicketAsync(_currentUserId, title, description, priority);
            
            if (success)
            {
                await DisplayAlert("Success", "Ticket submitted successfully!", "OK");
                ModalOverlay.IsVisible = false;
                ClearForm();
                
                // Reload tickets to show the new ticket immediately
                await LoadTickets();
            }
            else
            {
                await DisplayAlert("Error", "Failed to submit ticket. Please try again.", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TicketsPage: Error submitting ticket - {ex.Message}");
            await DisplayAlert("Error", $"Error: {ex.Message}", "OK");
        }
    }

    private void ClearForm()
    {
        TitleEntry.Text = string.Empty;
        CategoryPicker.SelectedIndex = 0;
        PriorityPicker.SelectedIndex = 0;
        DescriptionEditor.Text = string.Empty;
    }

    private async void OnViewDetailsClicked(object? sender, EventArgs e)
    {
        await DisplayAlert("View Details", "Ticket details view - Coming soon!", "OK");
    }

    private async void OnAddCommentClicked(object? sender, EventArgs e)
    {
        await DisplayAlert("Add Comment", "Add comment functionality - Coming soon!", "OK");
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