namespace MauiAppIT13.Pages.Admin;

public partial class AdminTicketsPage : ContentPage
{
    public AdminTicketsPage()
    {
        InitializeComponent();
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        // Placeholder for search functionality
        // In a real app, this would filter the ticket list based on the search text
        string searchText = e.NewTextValue?.ToLower() ?? string.Empty;
        
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            // Here you would filter your tickets collection
            // Example: Filter by teacher name or ticket number
        }
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

    private async void OnSettingsTapped(object? sender, EventArgs e)
    {
        await DisplayAlert("Settings", "System settings interface coming soon", "OK");
    }

    private async void OnLogoutTapped(object? sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");
        if (confirm)
        {
            await Shell.Current.GoToAsync("//AdminLoginPage");
        }
    }

    private async void OnSendAnnouncementClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("AdminAnnouncementsPage");
    }

    private async void OnFilterAllClicked(object? sender, EventArgs e)
    {
        await DisplayAlert("Filter", "Showing all tickets", "OK");
    }

    private async void OnFilterOpenClicked(object? sender, EventArgs e)
    {
        await DisplayAlert("Filter", "Showing open tickets", "OK");
    }

    private async void OnFilterPendingClicked(object? sender, EventArgs e)
    {
        await DisplayAlert("Filter", "Showing pending tickets", "OK");
    }

    private async void OnFilterClosedClicked(object? sender, EventArgs e)
    {
        await DisplayAlert("Filter", "Showing closed tickets", "OK");
    }

    private async void OnViewTicketTapped(object? sender, EventArgs e)
    {
        await DisplayAlert("View Ticket", "Ticket details will be shown here.", "OK");
    }

    private async void OnAssignTicketTapped(object? sender, EventArgs e)
    {
        await DisplayAlert("Assign Ticket", "Assign ticket to adviser functionality will be implemented here.", "OK");
    }

    private async void OnMoreActionsTapped(object? sender, EventArgs e)
    {
        string action = await DisplayActionSheet("More Actions", "Cancel", null, "Change Priority", "Change Status", "Add Note");
        if (!string.IsNullOrEmpty(action) && action != "Cancel")
        {
            await DisplayAlert("Action", $"{action} will be implemented here.", "OK");
        }
    }

    private async void OnCloseTicketTapped(object? sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Close Ticket", 
            "Are you sure you want to close this ticket?", 
            "Yes", "No");
        
        if (confirm)
        {
            await DisplayAlert("Success", "Ticket has been closed.", "OK");
        }
    }

    private async void OnAssignTicketClicked(object? sender, EventArgs e)
    {
        await DisplayAlert("Assign Ticket", "Technician assignment functionality will be implemented here.", "OK");
    }

    private async void OnViewDetailsClicked(object? sender, EventArgs e)
    {
        await DisplayAlert("Ticket Details", "Detailed ticket view will be shown here.", "OK");
    }

    private async void OnAddCommentClicked(object? sender, EventArgs e)
    {
        string comment = await DisplayPromptAsync("Add Comment", "Enter your comment:", "Submit", "Cancel");
        if (!string.IsNullOrWhiteSpace(comment))
        {
            await DisplayAlert("Success", "Comment added successfully.", "OK");
        }
    }

    private async void OnResolveTicketClicked(object? sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Resolve Ticket", 
            "Mark this ticket as resolved?", 
            "Yes", "No");
        
        if (confirm)
        {
            await DisplayAlert("Success", "Ticket has been marked as resolved.", "OK");
        }
    }
}
