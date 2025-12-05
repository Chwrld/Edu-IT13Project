using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Runtime.Versioning;
using MauiAppIT13.Database;
using MauiAppIT13.Models;
using MauiAppIT13.Services;
using MauiAppIT13.Utils;
using Microsoft.Maui.Controls.Shapes;

namespace MauiAppIT13.Pages.Admin;

[SupportedOSPlatform("windows10.0.17763.0")]
[SupportedOSPlatform("android21.0")]
public partial class AdminTicketsPage : ContentPage
{
    private readonly TicketService _ticketService;
    private readonly AuthManager _authManager;
    private readonly ObservableCollection<Ticket> _allTickets = new();
    private readonly ObservableCollection<Ticket> _filteredTickets = new();
    private Ticket? _selectedTicket;
    private bool _isLoading;
    private string _currentSearchText = string.Empty;
    private string _currentStatusFilter = "all";

    public AdminTicketsPage()
    {
        InitializeComponent();

        var dbConnection = AppServiceProvider.GetService<DbConnection>();
        _ticketService = AppServiceProvider.GetService<TicketService>() ??
                         new TicketService(dbConnection ?? throw new InvalidOperationException("DbConnection not found"));
        _authManager = AppServiceProvider.GetService<AuthManager>() ?? new AuthManager();

        TicketsCollectionView.ItemsSource = _filteredTickets;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadTicketsAsync();
    }

    private async Task LoadTicketsAsync()
    {
        if (_isLoading)
            return;

        _isLoading = true;
        try
        {
            var tickets = await _ticketService.GetAllTicketsAsync();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                _allTickets.Clear();
                foreach (var ticket in tickets)
                {
                    _allTickets.Add(ticket);
                }

                ApplyFilters();
                UpdateTicketStats();
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load tickets: {ex.Message}", "OK");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void UpdateTicketStats()
    {
        var openCount = _allTickets.Count(t => string.Equals(t.Status, "open", StringComparison.OrdinalIgnoreCase));
        var inProgressCount = _allTickets.Count(t => string.Equals(t.Status, "in_progress", StringComparison.OrdinalIgnoreCase));
        var resolvedCount = _allTickets.Count(t => string.Equals(t.Status, "resolved", StringComparison.OrdinalIgnoreCase));

        AdminOpenCountLabel.Text = openCount.ToString(CultureInfo.InvariantCulture);
        AdminInProgressCountLabel.Text = inProgressCount.ToString(CultureInfo.InvariantCulture);
        AdminResolvedCountLabel.Text = resolvedCount.ToString(CultureInfo.InvariantCulture);
    }

    private void ApplyFilters()
    {
        var query = _allTickets.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(_currentSearchText))
        {
            var search = _currentSearchText.ToLowerInvariant();
            query = query.Where(t =>
                (t.Title?.ToLowerInvariant().Contains(search) ?? false) ||
                (t.TicketNumber?.ToLowerInvariant().Contains(search) ?? false) ||
                (t.CreatedByName?.ToLowerInvariant().Contains(search) ?? false));
        }

        if (!string.Equals(_currentStatusFilter, "all", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(t => string.Equals(t.Status, _currentStatusFilter, StringComparison.OrdinalIgnoreCase));
        }

        _filteredTickets.Clear();
        foreach (var ticket in query)
        {
            _filteredTickets.Add(ticket);
        }
    }

    private void SetStatusFilter(string status)
    {
        _currentStatusFilter = status;
        ApplyFilters();
    }

    private string FormatDisplayValue(string? value, string fallback = "-")
        => string.IsNullOrWhiteSpace(value) ? fallback : value;

    private static string FormatStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return "-";

        var normalized = status.Replace('_', ' ');
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(normalized);
    }

    private async Task ShowTicketDetailsAsync(Ticket ticket)
    {
        _selectedTicket = ticket;

        DetailTitleLabel.Text = ticket.Title;
        DetailTicketNumberLabel.Text = ticket.TicketNumber;
        DetailStudentLabel.Text = FormatDisplayValue(ticket.CreatedByName, "Unknown");
        DetailAssignedLabel.Text = FormatDisplayValue(ticket.AssignedToName, "Unassigned");
        DetailPriorityLabel.Text = FormatDisplayValue(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(ticket.Priority));
        DetailStatusLabel.Text = FormatStatus(ticket.Status);
        AdminCommentEditor.Text = string.Empty;

        TicketDetailsOverlay.IsVisible = true;
        await LoadCommentsAsync(ticket.Id);
    }

    private async Task LoadCommentsAsync(Guid ticketId)
    {
        try
        {
            var comments = await _ticketService.GetTicketCommentsAsync(ticketId);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                CommentsStackLayout.Children.Clear();
                foreach (var comment in comments)
                {
                    CommentsStackLayout.Children.Add(CreateCommentView(comment));
                }
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load comments: {ex.Message}", "OK");
        }
    }

    private static View CreateCommentView(TicketComment comment)
    {
        bool isAdmin = string.Equals(comment.UserRole, "Admin", StringComparison.OrdinalIgnoreCase);
        var background = isAdmin ? Color.FromArgb("#EFF6FF") : Color.FromArgb("#F9FAFB");

        var container = new Border
        {
            BackgroundColor = background,
            Padding = new Thickness(16, 12),
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 10 }
        };

        var header = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new ColumnDefinition(), new ColumnDefinition { Width = GridLength.Auto } } };
        header.Add(new Label
        {
            Text = $"{comment.UserName} • {comment.UserRole}",
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#1F2937"),
            FontSize = 13
        });

        header.Add(new Label
        {
            Text = comment.CreatedAt.ToLocalTime().ToString("MMM d, h:mm tt"),
            TextColor = Color.FromArgb("#6B7280"),
            FontSize = 12
        }, 1, 0);

        var content = new Label
        {
            Text = comment.Content,
            TextColor = Color.FromArgb("#374151"),
            FontSize = 14,
            LineBreakMode = LineBreakMode.WordWrap,
            Margin = new Thickness(0, 8, 0, 0)
        };

        container.Content = new VerticalStackLayout
        {
            Spacing = 6,
            Children = { header, content }
        };

        return container;
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        _currentSearchText = e.NewTextValue?.Trim() ?? string.Empty;
        ApplyFilters();
    }

    private async void OnDashboardTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//AdminHomePage");
    }

    private async void OnUsersTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("AdminUsersPage");
    }

    private async void OnAnnouncementsTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("AdminAnnouncementsPage");
    }

    private async void OnReportsTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("AdminReportsPage");
    }

    private async void OnAdminProfileTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//AdminProfilePage");
    }

    private async void OnLogoutTapped(object? sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");
        if (confirm)
        {
            await Shell.Current.GoToAsync("//MainPage");
        }
    }

    private void OnFilterAllClicked(object? sender, EventArgs e) => SetStatusFilter("all");

    private void OnFilterOpenClicked(object? sender, EventArgs e) => SetStatusFilter("open");

    private void OnFilterPendingClicked(object? sender, EventArgs e) => SetStatusFilter("in_progress");

    private void OnFilterClosedClicked(object? sender, EventArgs e) => SetStatusFilter("resolved");

    private async void OnViewTicketClicked(object? sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Ticket ticket)
        {
            await ShowTicketDetailsAsync(ticket);
        }
    }

    private async void OnResolveTicketClicked(object? sender, EventArgs e)
    {
        Ticket? ticketToResolve = _selectedTicket;

        if (sender is Button button && button.CommandParameter is Ticket ticket)
        {
            ticketToResolve = ticket;
        }

        if (ticketToResolve == null)
        {
            await DisplayAlert("Error", "Please select a ticket first.", "OK");
            return;
        }

        var confirm = await DisplayAlert("Resolve Ticket", "Mark this ticket as resolved?", "Yes", "No");
        if (!confirm)
            return;

        var currentUser = _authManager.CurrentUser;
        var success = await _ticketService.UpdateTicketStatusAsync(ticketToResolve.Id, "resolved", currentUser?.Id);

        if (!success)
        {
            await DisplayAlert("Error", "Failed to resolve ticket. Please try again.", "OK");
            return;
        }

        await LoadTicketsAsync();

        if (_selectedTicket?.Id == ticketToResolve.Id)
        {
            DetailStatusLabel.Text = FormatStatus("resolved");
        }

        await DisplayAlert("Success", "Ticket marked as resolved.", "OK");
    }

    private async void OnChangeStatusClicked(object? sender, EventArgs e)
    {
        Ticket? ticketToUpdate = null;

        if (sender is Button button && button.CommandParameter is Ticket fromButton)
        {
            ticketToUpdate = fromButton;
        }
        else if (_selectedTicket != null)
        {
            ticketToUpdate = _selectedTicket;
        }

        if (ticketToUpdate == null)
        {
            await DisplayAlert("Error", "Please select a ticket first.", "OK");
            return;
        }

        var statusOptions = new (string Label, string Value)[]
        {
            ("Open", "open"),
            ("In Progress", "in_progress"),
            ("Resolved", "resolved")
        };

        var selection = await DisplayActionSheet(
            $"Status ({FormatStatus(ticketToUpdate.Status)})",
            "Cancel",
            null,
            statusOptions.Select(s => s.Label).ToArray());

        if (string.IsNullOrEmpty(selection) || selection == "Cancel")
            return;

        var selectedStatus = statusOptions.First(s => s.Label == selection).Value;
        if (string.Equals(selectedStatus, ticketToUpdate.Status, StringComparison.OrdinalIgnoreCase))
            return;

        var currentUser = _authManager.CurrentUser;
        var success = await _ticketService.UpdateTicketStatusAsync(ticketToUpdate.Id, selectedStatus, currentUser?.Id);

        if (!success)
        {
            await DisplayAlert("Error", "Failed to update ticket. Please try again.", "OK");
            return;
        }

        await LoadTicketsAsync();

        if (_selectedTicket?.Id == ticketToUpdate.Id)
        {
            DetailStatusLabel.Text = FormatStatus(selectedStatus);
        }

        await DisplayAlert("Success", $"Ticket marked as {FormatStatus(selectedStatus)}.", "OK");
    }

    private void OnCloseDetailsClicked(object? sender, EventArgs e)
    {
        TicketDetailsOverlay.IsVisible = false;
        CommentsStackLayout.Children.Clear();
        AdminCommentEditor.Text = string.Empty;
        _selectedTicket = null;
    }

    private async void OnSendCommentClicked(object? sender, EventArgs e)
    {
        if (_selectedTicket == null)
        {
            await DisplayAlert("Error", "Select a ticket to reply to.", "OK");
            return;
        }

        string content = AdminCommentEditor.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(content))
            return;

        var currentUser = _authManager.CurrentUser;
        if (currentUser == null)
        {
            await DisplayAlert("Error", "Admin user not authenticated.", "OK");
            return;
        }

        var success = await _ticketService.AddCommentAsync(_selectedTicket.Id, currentUser.Id, content);
        if (!success)
        {
            await DisplayAlert("Error", "Failed to send message. Try again.", "OK");
            return;
        }

        AdminCommentEditor.Text = string.Empty;
        await LoadCommentsAsync(_selectedTicket.Id);
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

            // Submit ticket to database (use admin's ID as the creator)
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
            System.Diagnostics.Debug.WriteLine($"AdminTicketsPage: Error creating ticket - {ex.Message}");
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
