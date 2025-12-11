using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.Versioning;
using MauiAppIT13.Models;
using MauiAppIT13.Services;
using MauiAppIT13.Utils;

namespace MauiAppIT13.Pages.Admin;

[SupportedOSPlatform("windows10.0.17763.0")]
[SupportedOSPlatform("android21.0")]
public partial class AdminHomePage : ContentPage
{
    private readonly AdminDashboardService _dashboardService;
    private readonly ReportsService _reportsService;
    private readonly ReportExportService _reportExportService;
    private readonly AdminDataExportService _adminDataExportService;
    private readonly SyncService _syncService;
    private readonly DeltaSyncService _deltaSyncService;
    private bool _isLoading;

    public ObservableCollection<AdminActivityItem> RecentActivities { get; } = new();

    public AdminHomePage()
    {
        InitializeComponent();
        _dashboardService = AppServiceProvider.GetService<AdminDashboardService>()
            ?? throw new InvalidOperationException("AdminDashboardService is not registered.");
        _reportsService = AppServiceProvider.GetService<ReportsService>()
            ?? throw new InvalidOperationException("ReportsService is not registered.");
        _reportExportService = AppServiceProvider.GetService<ReportExportService>()
            ?? throw new InvalidOperationException("ReportExportService is not registered.");
        _adminDataExportService = AppServiceProvider.GetService<AdminDataExportService>()
            ?? throw new InvalidOperationException("AdminDataExportService is not registered.");
        _syncService = AppServiceProvider.GetService<SyncService>()
            ?? throw new InvalidOperationException("SyncService is not registered.");
        _deltaSyncService = AppServiceProvider.GetService<DeltaSyncService>()
            ?? throw new InvalidOperationException("DeltaSyncService is not registered.");
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDashboardAsync();
    }

    private async Task LoadDashboardAsync()
    {
        if (_isLoading)
            return;

        _isLoading = true;
        try
        {
            var summary = await _dashboardService.GetSummaryAsync();
            UpdateSummaryCards(summary);
            UpdateRecentActivity(summary.RecentActivities);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Dashboard", $"Failed to load dashboard data: {ex.Message}", "OK");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void UpdateSummaryCards(AdminDashboardSummary summary)
    {
        TotalUsersValueLabel.Text = summary.TotalUsers.ToString("N0", CultureInfo.InvariantCulture);
        TotalUsersChangeLabel.Text = FormatChangeLabel(summary.UserGrowthPercent);

        ActiveTicketsValueLabel.Text = summary.ActiveTickets.ToString("N0", CultureInfo.InvariantCulture);
        ActiveTicketsSubLabel.Text = $"{summary.OpenTickets} open • {summary.InProgressTickets} in progress";

        AnnouncementsValueLabel.Text = summary.AnnouncementsTotal.ToString("N0", CultureInfo.InvariantCulture);
        AnnouncementsSubLabel.Text = $"{summary.AnnouncementsThisWeek} this week";
    }

    private void UpdateRecentActivity(IReadOnlyList<AdminActivityItem> activities)
    {
        RecentActivities.Clear();
        foreach (var activity in activities)
        {
            RecentActivities.Add(activity);
        }

        RecentActivityList.IsVisible = RecentActivities.Count > 0;
        RecentActivityEmptyLabel.IsVisible = RecentActivities.Count == 0;
    }

    private static string FormatChangeLabel(double percent)
    {
        var arrow = percent >= 0 ? "↑" : "↓";
        return $"{arrow} {Math.Abs(percent):0.#}% this month";
    }

    private async void OnAdminProfileTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//AdminProfilePage", animate: false);
    }

    private async void OnUsersTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("AdminUsersPage", animate: false);
    }

    private async void OnAnnouncementsTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("AdminAnnouncementsPage", animate: false);
    }

    private async void OnTicketsTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("AdminTicketsPage", animate: false);
    }

    private async void OnReportsTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("AdminReportsPage", animate: false);
    }

    private async void OnLogoutTapped(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");
        if (confirm)
        {
            await Shell.Current.GoToAsync("//MainPage", animate: false);
        }
    }

    private async void OnCreateAnnouncementClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("AdminAnnouncementsPage?action=new", animate: false);
    }

    private async void OnAddUserClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("AdminUsersPage?action=new", animate: false);
    }

    private async void OnViewReportsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("AdminReportsPage", animate: false);
    }

    private async void OnDownloadDataClicked(object sender, EventArgs e)
    {
        await ExportDashboardDataAsync();
    }

    private async void OnSyncToRemoteClicked(object sender, EventArgs e)
    {
        await ShowSyncProgressAsync();
    }

    private async Task ShowSyncProgressAsync()
    {
        try
        {
            // Show modal
            SyncProgressOverlay.IsVisible = true;
            SyncProgressBar.Progress = 0;
            SyncPercentageLabel.Text = "0%";
            SyncStatusLabel.Text = "Checking connection...";
            SyncResultLabel.IsVisible = false;
            SyncCloseButton.IsVisible = false;
            Step1Icon.Text = "⏳";
            Step2Icon.Text = "⏳";
            Step3Icon.Text = "⏳";

            // Step 1: Check connection (20%)
            await UpdateProgress(0.2, "Checking connection...", "Step 1");
            bool isOnline = await _syncService.IsOnlineAsync();
            Debug.WriteLine($"[SyncTest] Online status: {isOnline}");

            if (!isOnline)
            {
                Step1Icon.Text = "❌";
                SyncStatusLabel.Text = "Remote server not accessible";
                SyncResultContainer.IsVisible = false;
                SyncCloseButton.IsVisible = true;
                return;
            }

            Step1Icon.Text = "✅";

            // Step 2: Delta sync (only changed records) (60%)
            await UpdateProgress(0.6, "Syncing changed records to remote database...", "Step 2");
            var (success, recordsSynced) = await _deltaSyncService.DeltaSyncToRemoteAsync();

            if (!success)
            {
                Step2Icon.Text = "❌";
                SyncStatusLabel.Text = "Sync failed";
                SyncResultLabel.Text = "❌ Delta sync failed. Check debug output for details.";
                SyncResultContainer.BackgroundColor = Color.FromArgb("#FEF2F2");
                SyncResultContainer.Stroke = Color.FromArgb("#FECACA");
                SyncResultLabel.TextColor = Color.FromArgb("#DC2626");
                SyncResultContainer.IsVisible = true;
                SyncCloseButton.IsVisible = true;
                return;
            }

            Step2Icon.Text = "✅";

            // Step 3: Finalize (100%)
            await UpdateProgress(1.0, "Sync complete!", "Step 3");
            Step3Icon.Text = "✅";
            
            SyncStatusLabel.Text = "Sync completed successfully";
            SyncResultLabel.Text = $"✅ Successfully synced {recordsSynced} changed records to remote database!";
            SyncResultContainer.BackgroundColor = Color.FromArgb("#F0FDF4");
            SyncResultContainer.Stroke = Color.FromArgb("#86EFAC");
            SyncResultLabel.TextColor = Color.FromArgb("#10B981");
            SyncResultContainer.IsVisible = true;
            SyncCloseButton.IsVisible = true;
            Debug.WriteLine($"[DeltaSync] Sync completed successfully - {recordsSynced} records synced");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SyncTest] Error: {ex.Message}");
            Step1Icon.Text = "❌";
            Step2Icon.Text = "❌";
            Step3Icon.Text = "❌";
            SyncStatusLabel.Text = "Error occurred";
            SyncResultLabel.Text = $"❌ Error: {ex.Message}";
            SyncResultContainer.BackgroundColor = Color.FromArgb("#FEF2F2");
            SyncResultContainer.Stroke = Color.FromArgb("#FECACA");
            SyncResultLabel.TextColor = Color.FromArgb("#DC2626");
            SyncResultContainer.IsVisible = true;
            SyncCloseButton.IsVisible = true;
        }
    }

    private async Task UpdateProgress(double progress, string status, string step)
    {
        SyncProgressBar.Progress = progress;
        SyncPercentageLabel.Text = $"{(int)(progress * 100)}%";
        SyncStatusLabel.Text = status;
        await Task.Delay(300); // Small delay for visual feedback
    }

    private void OnSyncCloseClicked(object sender, EventArgs e)
    {
        SyncProgressOverlay.IsVisible = false;
    }

    private async Task ExportDashboardDataAsync()
    {
        try
        {
            var exports = new List<string>();
            var period = ReportPeriod.Last30Days;
            var metrics = await _reportsService.GetMetricsAsync(period);
            var reportData = await _reportsService.BuildReportAsync(ReportCategory.TicketSummary, period, metrics);
            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "EduCRM", "Reports");
            var summaryPath = await _reportExportService.ExportCsvAsync(reportData, directory);
            exports.Add($"Ticket summary → {summaryPath}");

            var adminExport = await _adminDataExportService.ExportAsync(directory);
            exports.AddRange(adminExport.FilePaths.Select(path => $"Dataset → {path}"));

            var message = "Dashboard export generated:\n" + string.Join("\n", exports.Select(path => $"• {path}"));
            await DisplayAlert("Export Complete", message, "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Export Failed", $"Unable to export data: {ex.Message}", "OK");
        }
    }
}
