using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.Versioning;
using MauiAppIT13.Models;
using MauiAppIT13.Services;
using MauiAppIT13.Utils;
using Microsoft.Maui.Controls.Shapes;

namespace MauiAppIT13.Pages.Teacher;

[SupportedOSPlatform("windows10.0.17763.0")]
[SupportedOSPlatform("android21.0")]
public partial class TeacherTicketsPage : ContentPage
{
    private readonly TicketService _ticketService;
    private readonly AuthManager _authManager;
    private ObservableCollection<Ticket> _allTickets = new();
    private ObservableCollection<Ticket> _filteredTickets = new();
    private Ticket? _selectedTicket;
    private string _currentFilter = "all";

    public TeacherTicketsPage()
    {
        InitializeComponent();
        _ticketService = AppServiceProvider.GetService<TicketService>()
            ?? throw new InvalidOperationException("TicketService not found");
        _authManager = AppServiceProvider.GetService<AuthManager>()
            ?? throw new InvalidOperationException("AuthManager not found");
        
        TicketsCollectionView.ItemsSource = _filteredTickets;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadTicketsAsync();
    }

    private async Task LoadTicketsAsync()
    {
        try
        {
            Debug.WriteLine("TeacherTicketsPage: Loading tickets...");
            
            // Load all tickets for teacher view
            _allTickets = await _ticketService.GetAllTicketsAsync();
            _filteredTickets.Clear();
            
            foreach (var ticket in _allTickets)
            {
                _filteredTickets.Add(ticket);
            }

            UpdateStatistics();
            Debug.WriteLine($"TeacherTicketsPage: Loaded {_allTickets.Count} tickets");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"TeacherTicketsPage: Error loading tickets - {ex.Message}");
            await DisplayAlert("Error", "Failed to load tickets. Please try again.", "OK");
        }
    }

    private void UpdateStatistics()
    {
        var openCount = _allTickets.Count(t => t.Status.Equals("open", StringComparison.OrdinalIgnoreCase));
        var inProgressCount = _allTickets.Count(t => t.Status.Equals("in_progress", StringComparison.OrdinalIgnoreCase));
        var resolvedCount = _allTickets.Count(t => t.Status.Equals("resolved", StringComparison.OrdinalIgnoreCase));

        OpenCountLabel.Text = openCount.ToString();
        InProgressCountLabel.Text = inProgressCount.ToString();
        ResolvedCountLabel.Text = resolvedCount.ToString();
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        ApplyFilters(e.NewTextValue);
    }

    private void ApplyFilters(string? searchText = null)
    {
        searchText ??= SearchEntry.Text;
        var query = _allTickets.AsEnumerable();

        // Apply status filter
        if (_currentFilter != "all")
        {
            query = query.Where(t => t.Status.Equals(_currentFilter, StringComparison.OrdinalIgnoreCase));
        }

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var search = searchText.ToLower();
            query = query.Where(t =>
                t.TicketNumber.ToLower().Contains(search) ||
                t.Title.ToLower().Contains(search) ||
                t.CreatedByName.ToLower().Contains(search));
        }

        _filteredTickets.Clear();
        foreach (var ticket in query)
        {
            _filteredTickets.Add(ticket);
        }
    }

    private void UpdateFilterButtons(string activeFilter)
    {
        _currentFilter = activeFilter;

        // Reset all buttons
        FilterAllBtn.BackgroundColor = Color.FromArgb("#F3F4F6");
        FilterAllBtn.TextColor = Color.FromArgb("#6B7280");
        FilterOpenBtn.BackgroundColor = Color.FromArgb("#F3F4F6");
        FilterOpenBtn.TextColor = Color.FromArgb("#6B7280");
        FilterInProgressBtn.BackgroundColor = Color.FromArgb("#F3F4F6");
        FilterInProgressBtn.TextColor = Color.FromArgb("#6B7280");
        FilterResolvedBtn.BackgroundColor = Color.FromArgb("#F3F4F6");
        FilterResolvedBtn.TextColor = Color.FromArgb("#6B7280");

        // Highlight active button
        switch (activeFilter)
        {
            case "all":
                FilterAllBtn.BackgroundColor = Color.FromArgb("#059669");
                FilterAllBtn.TextColor = Colors.White;
                break;
            case "open":
                FilterOpenBtn.BackgroundColor = Color.FromArgb("#059669");
                FilterOpenBtn.TextColor = Colors.White;
                break;
            case "in_progress":
                FilterInProgressBtn.BackgroundColor = Color.FromArgb("#059669");
                FilterInProgressBtn.TextColor = Colors.White;
                break;
            case "resolved":
                FilterResolvedBtn.BackgroundColor = Color.FromArgb("#059669");
                FilterResolvedBtn.TextColor = Colors.White;
                break;
        }

        ApplyFilters();
    }

    private void OnFilterAllClicked(object? sender, EventArgs e)
    {
        UpdateFilterButtons("all");
    }

    private void OnFilterOpenClicked(object? sender, EventArgs e)
    {
        UpdateFilterButtons("open");
    }

    private void OnFilterInProgressClicked(object? sender, EventArgs e)
    {
        UpdateFilterButtons("in_progress");
    }

    private void OnFilterResolvedClicked(object? sender, EventArgs e)
    {
        UpdateFilterButtons("resolved");
    }

    private async void OnViewTicketClicked(object? sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Ticket ticket)
        {
            _selectedTicket = ticket;
            await ShowTicketDetailsAsync(ticket);
        }
    }

    private async Task ShowTicketDetailsAsync(Ticket ticket)
    {
        try
        {
            DetailTitleLabel.Text = ticket.Title;
            DetailTicketNumberLabel.Text = ticket.TicketNumber;
            DetailStudentLabel.Text = ticket.CreatedByName;
            DetailCreatedLabel.Text = ticket.CreatedAt.ToString("M/d/yyyy h:mm tt");
            DetailPriorityLabel.Text = ticket.Priority;
            DetailStatusLabel.Text = ticket.Status;
            DetailDescriptionLabel.Text = ticket.Description;

            // Load comments
            var comments = await _ticketService.GetTicketCommentsAsync(ticket.Id);
            CommentsStackLayout.Clear();

            if (comments.Count == 0)
            {
                CommentsStackLayout.Add(new Label
                {
                    Text = "No comments yet",
                    TextColor = Color.FromArgb("#9CA3AF"),
                    FontSize = 14,
                    Margin = new Thickness(0, 10)
                });
            }
            else
            {
                foreach (var comment in comments)
                {
                    var isTeacher = comment.UserRole.Equals("teacher", StringComparison.OrdinalIgnoreCase) ||
                                   comment.UserRole.Equals("admin", StringComparison.OrdinalIgnoreCase);

                    var commentBorder = new Border
                    {
                        BackgroundColor = isTeacher ? Color.FromArgb("#E0F2FE") : Color.FromArgb("#F3F4F6"),
                        Padding = new Thickness(12),
                        StrokeThickness = 0,
                        Margin = new Thickness(0, 5),
                        StrokeShape = new RoundRectangle { CornerRadius = 8 },
                        Content = new VerticalStackLayout
                        {
                            Spacing = 4,
                            Children =
                            {
                                new Label
                                {
                                    Text = $"{comment.UserName} ({comment.UserRole})",
                                    FontSize = 13,
                                    FontAttributes = FontAttributes.Bold,
                                    TextColor = Color.FromArgb("#1F2937")
                                },
                                new Label
                                {
                                    Text = comment.Content,
                                    FontSize = 14,
                                    TextColor = Color.FromArgb("#374151"),
                                    LineBreakMode = LineBreakMode.WordWrap
                                },
                                new Label
                                {
                                    Text = comment.CreatedAt.ToString("M/d/yyyy h:mm tt"),
                                    FontSize = 11,
                                    TextColor = Color.FromArgb("#6B7280")
                                }
                            }
                        }
                    };

                    CommentsStackLayout.Add(commentBorder);
                }
            }

            TicketDetailsOverlay.IsVisible = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"TeacherTicketsPage: Error showing ticket details - {ex.Message}");
            await DisplayAlert("Error", "Failed to load ticket details.", "OK");
        }
    }

    private void OnCloseDetailsClicked(object? sender, EventArgs e)
    {
        TicketDetailsOverlay.IsVisible = false;
        _selectedTicket = null;
        CommentEditor.Text = string.Empty;
    }

    private async void OnSendCommentClicked(object? sender, EventArgs e)
    {
        if (_selectedTicket == null)
            return;

        var comment = CommentEditor.Text?.Trim();
        if (string.IsNullOrWhiteSpace(comment))
        {
            await DisplayAlert("Validation", "Please enter a comment.", "OK");
            return;
        }

        try
        {
            var currentUser = _authManager.CurrentUser;
            if (currentUser == null)
            {
                await DisplayAlert("Error", "User not authenticated.", "OK");
                return;
            }

            var success = await _ticketService.AddCommentAsync(_selectedTicket.Id, currentUser.Id, comment);

            if (success)
            {
                CommentEditor.Text = string.Empty;
                await ShowTicketDetailsAsync(_selectedTicket); // Refresh details
                await DisplayAlert("Success", "Comment added successfully.", "OK");
            }
            else
            {
                await DisplayAlert("Error", "Failed to add comment.", "OK");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"TeacherTicketsPage: Error sending comment - {ex.Message}");
            await DisplayAlert("Error", "Failed to send comment.", "OK");
        }
    }

    private async void OnResolveTicketClicked(object? sender, EventArgs e)
    {
        if (_selectedTicket == null)
            return;

        var confirm = await DisplayAlert("Confirm", 
            "Mark this ticket as resolved?", 
            "Yes", "No");

        if (!confirm)
            return;

        try
        {
            var currentUser = _authManager.CurrentUser;
            if (currentUser == null)
            {
                await DisplayAlert("Error", "User not authenticated.", "OK");
                return;
            }

            var success = await _ticketService.UpdateTicketStatusAsync(
                _selectedTicket.Id, 
                "resolved", 
                currentUser.Id);

            if (success)
            {
                TicketDetailsOverlay.IsVisible = false;
                await LoadTicketsAsync(); // Refresh list
                await DisplayAlert("Success", "Ticket marked as resolved.", "OK");
            }
            else
            {
                await DisplayAlert("Error", "Failed to update ticket status.", "OK");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"TeacherTicketsPage: Error resolving ticket - {ex.Message}");
            await DisplayAlert("Error", "Failed to resolve ticket.", "OK");
        }
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

    private async void OnAnnouncementsTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//TeacherAnnouncementsPage");
    }

    private async void OnFacultyTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//TeacherProfilePage");
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

    private void OnNewTicketClicked(object? sender, EventArgs e)
    {
        CreateTicketModalOverlay.IsVisible = true;
    }

    private void OnCloseModalTapped(object? sender, EventArgs e)
    {
        CreateTicketModalOverlay.IsVisible = false;
        ClearNewTicketForm();
    }

    private void OnOverlayTapped(object? sender, EventArgs e)
    {
        CreateTicketModalOverlay.IsVisible = false;
        ClearNewTicketForm();
    }

    private void OnCancelNewTicketClicked(object? sender, EventArgs e)
    {
        CreateTicketModalOverlay.IsVisible = false;
        ClearNewTicketForm();
    }

    private async void OnSubmitNewTicketClicked(object? sender, EventArgs e)
    {
        try
        {
            string title = NewTicketTitleEntry.Text?.Trim() ?? string.Empty;
            string category = NewTicketCategoryPicker.SelectedIndex > 0 ? NewTicketCategoryPicker.Items[NewTicketCategoryPicker.SelectedIndex] : string.Empty;
            string priority = NewTicketPriorityPicker.SelectedIndex > 0 ? NewTicketPriorityPicker.Items[NewTicketPriorityPicker.SelectedIndex].ToLower() : string.Empty;
            string description = NewTicketDescriptionEditor.Text?.Trim() ?? string.Empty;

            // Validation
            if (string.IsNullOrEmpty(title))
            {
                await DisplayAlert("Error", "Please enter a title for the ticket.", "OK");
                return;
            }

            if (NewTicketCategoryPicker.SelectedIndex <= 0)
            {
                await DisplayAlert("Error", "Please select a category.", "OK");
                return;
            }

            if (NewTicketPriorityPicker.SelectedIndex <= 0)
            {
                await DisplayAlert("Error", "Please select a priority level.", "OK");
                return;
            }

            if (string.IsNullOrEmpty(description))
            {
                await DisplayAlert("Error", "Please provide a description of the issue.", "OK");
                return;
            }

            // Submit ticket to database (use teacher's ID as the creator)
            var currentUser = _authManager.CurrentUser;
            if (currentUser == null)
            {
                await DisplayAlert("Error", "User not authenticated.", "OK");
                return;
            }

            bool success = await _ticketService.CreateTicketAsync(currentUser.Id, title, description, priority);
            
            if (success)
            {
                await DisplayAlert("Success", "Ticket created successfully!", "OK");
                CreateTicketModalOverlay.IsVisible = false;
                ClearNewTicketForm();
                
                // Reload tickets to show the new ticket
                await LoadTicketsAsync();
            }
            else
            {
                await DisplayAlert("Error", "Failed to create ticket. Please try again.", "OK");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"TeacherTicketsPage: Error creating ticket - {ex.Message}");
            await DisplayAlert("Error", $"Error: {ex.Message}", "OK");
        }
    }

    private void ClearNewTicketForm()
    {
        NewTicketTitleEntry.Text = string.Empty;
        NewTicketCategoryPicker.SelectedIndex = 0;
        NewTicketPriorityPicker.SelectedIndex = 0;
        NewTicketDescriptionEditor.Text = string.Empty;
    }
}
