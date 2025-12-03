namespace MauiAppIT13.Pages.Admin;

public partial class AdminReportsPage : ContentPage
{
    public AdminReportsPage()
    {
        InitializeComponent();
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

    private async void OnTicketsTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("AdminTicketsPage");
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
            await Shell.Current.GoToAsync("//MainPage");
        }
    }

    private async void OnGenerateReportClicked(object? sender, EventArgs e)
    {
        string reportType = ReportTypePicker.SelectedItem?.ToString() ?? "Unknown";
        await DisplayAlert("Generate Report", 
            $"Generating {reportType} report...", 
            "OK");
    }

    private async void OnExportPdfClicked(object? sender, EventArgs e)
    {
        await DisplayAlert("Export PDF", "PDF export functionality will be implemented here.", "OK");
    }

    private async void OnExportCsvClicked(object? sender, EventArgs e)
    {
        await DisplayAlert("Export CSV", "CSV export functionality will be implemented here.", "OK");
    }
}
