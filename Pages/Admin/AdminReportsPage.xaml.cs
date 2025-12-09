using System.Globalization;
using System.IO;
using Microsoft.Maui.Graphics;
using MauiAppIT13.Models;
using MauiAppIT13.Services;
using MauiAppIT13.Utils;

namespace MauiAppIT13.Pages.Admin;

public partial class AdminReportsPage : ContentPage
{
    private readonly ReportsService _reportsService;
    private readonly ReportExportService _reportExportService;
    private AdminReportMetrics? _currentMetrics;
    private ReportPeriod _currentPeriod = ReportPeriod.Last30Days;
    private bool _isLoading;
    private bool _exportPdfSelected;
    private bool _exportCsvSelected;

    public AdminReportsPage()
    {
        InitializeComponent();
        _reportsService = AppServiceProvider.GetService<ReportsService>()
            ?? throw new InvalidOperationException("ReportsService is not registered.");
        _reportExportService = AppServiceProvider.GetService<ReportExportService>()
            ?? throw new InvalidOperationException("ReportExportService is not registered.");
        UpdateQuickExportButtons();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadMetricsAsync();
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

    private async void OnGenerateReportClicked(object? sender, EventArgs e)
    {
        string reportType = ReportTypePicker.SelectedItem?.ToString() ?? "Unknown";

        if (_currentMetrics is null)
        {
            await DisplayAlert("Generate Report", "Analytics are still loading. Please try again in a moment.", "OK");
            return;
        }

        var message = $"Report \"{reportType}\" for {_currentPeriod} prepared.\n" +
                      $"Total Tickets: {_currentMetrics.TotalTicketsCurrent:N0}\n" +
                      $"Active Users: {_currentMetrics.ActiveUsersTotal:N0}\n" +
                      $"Avg Response: {FormatDuration(_currentMetrics.AvgResponseMinutesCurrent)}";

        var exports = await ExportSelectedFormatsAsync();
        if (exports.Count > 0)
        {
            message += "\n\nExports:\n" + string.Join('\n', exports.Select(path => $"• {path}"));
        }

        await DisplayAlert("Generate Report", message, "OK");
    }

    private async Task LoadMetricsAsync()
    {
        if (_isLoading)
            return;

        _isLoading = true;
        try
        {
            PreviewGeneratedLabel.Text = "Loading...";
            _currentPeriod = GetSelectedPeriod();
            _currentMetrics = await _reportsService.GetMetricsAsync(_currentPeriod);
            UpdateDashboard(_currentMetrics);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Analytics", $"Failed to load analytics: {ex.Message}", "OK");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void UpdateDashboard(AdminReportMetrics metrics)
    {
        TotalTicketsValueLabel.Text = metrics.TotalTicketsCurrent.ToString("N0", CultureInfo.InvariantCulture);
        ConfigureChangeLabel(TotalTicketsChangeLabel, metrics.TotalTicketsChangePercent);

        AvgResponseValueLabel.Text = FormatDuration(metrics.AvgResponseMinutesCurrent);
        ConfigureChangeLabel(AvgResponseChangeLabel, metrics.AvgResponseChangePercent, invert: true);

        ResolutionRateValueLabel.Text = $"{metrics.ResolutionRateCurrentPercent:0.#}%";
        ConfigureChangeLabel(ResolutionRateChangeLabel, metrics.ResolutionRateChangePercent);

        ActiveUsersValueLabel.Text = metrics.ActiveUsersTotal.ToString("N0", CultureInfo.InvariantCulture);
        ConfigureChangeLabel(ActiveUsersChangeLabel, metrics.ActiveUsersChangePercent);

        PreviewPeriodLabel.Text = $"{metrics.PeriodStartUtc:MMM d} - {metrics.PeriodEndUtc:MMM d}";
        PreviewGeneratedLabel.Text = $"Generated {metrics.ReportGeneratedUtc:MMM d, yyyy h:mm tt}";

        ResponseInsightValueLabel.Text = FormatDuration(metrics.AvgResponseMinutesCurrent);
        ResponseInsightLabel.Text = metrics.AvgResponseChangePercent switch
        {
            < -1 => $"Average response time decreased by {Math.Abs(metrics.AvgResponseChangePercent):0.#}% this period.",
            > 1 => $"Average response time increased by {metrics.AvgResponseChangePercent:0.#}%. Investigate bottlenecks.",
            _ => "Response times held steady compared to the previous period."
        };

        EngagementInsightValueLabel.Text = $"{metrics.StudentEngagementCurrent:N0} activities";
        EngagementInsightLabel.Text = metrics.StudentEngagementChangePercent switch
        {
            < -1 => $"Student engagement dipped by {Math.Abs(metrics.StudentEngagementChangePercent):0.#}%.",
            > 1 => $"Student engagement grew by {metrics.StudentEngagementChangePercent:0.#}% compared to last period.",
            _ => "Student engagement remained stable compared to the previous period."
        };

        CommunicationInsightValueLabel.Text = $"{metrics.MessagesCurrent:N0} messages";
        CommunicationInsightLabel.Text = metrics.MessageVolumeChangePercent switch
        {
            < -1 => $"Message volume dropped by {Math.Abs(metrics.MessageVolumeChangePercent):0.#}% this period.",
            > 1 => $"{metrics.MessagesCurrent:N0} messages sent (+{metrics.MessageVolumeChangePercent:0.#}%).",
            _ => "Message volume is steady compared to the previous period."
        };
    }

    private static string FormatDuration(double minutes)
    {
        if (minutes <= 0)
            return "n/a";

        if (minutes < 60)
            return $"{minutes:0.#} mins";

        var hours = minutes / 60d;
        return $"{hours:0.#} hrs";
    }

    private static void ConfigureChangeLabel(Label label, double percentChange, bool invert = false)
    {
        string sign = percentChange >= 0 ? "+" : string.Empty;
        label.Text = $"{sign}{percentChange:0.#}% from previous period";

        bool positive = invert ? percentChange < 0 : percentChange >= 0;
        label.TextColor = positive ? Color.FromArgb("#10B981") : Color.FromArgb("#EF4444");
    }

    private ReportPeriod GetSelectedPeriod()
    {
        return DateRangePicker.SelectedIndex switch
        {
            0 => ReportPeriod.Last7Days,
            1 => ReportPeriod.Last30Days,
            2 => ReportPeriod.Last90Days,
            _ => ReportPeriod.Last30Days
        };
    }

    private async void OnDateRangeChanged(object? sender, EventArgs e)
    {
        if (DateRangePicker.SelectedIndex == 3)
        {
            await DisplayAlert("Coming Soon", "Custom date ranges will be supported in a future update.", "OK");
            DateRangePicker.SelectedIndex = _currentPeriod switch
            {
                ReportPeriod.Last7Days => 0,
                ReportPeriod.Last30Days => 1,
                ReportPeriod.Last90Days => 2,
                _ => 1
            };
            return;
        }

        await LoadMetricsAsync();
    }

    private ReportCategory GetSelectedCategory()
    {
        var selection = ReportTypePicker.SelectedItem?.ToString()?.ToLowerInvariant() ?? string.Empty;
        return selection switch
        {
            var s when s.Contains("student") => ReportCategory.StudentActivity,
            var s when s.Contains("adviser") || s.Contains("advisor") => ReportCategory.AdviserPerformance,
            var s when s.Contains("communication") => ReportCategory.CommunicationAnalytics,
            _ => ReportCategory.TicketSummary
        };
    }

    private void OnReportTypeChanged(object? sender, EventArgs e)
    {
        _exportPdfSelected = false;
        _exportCsvSelected = false;
        UpdateQuickExportButtons();
    }

    private async Task<List<string>> ExportSelectedFormatsAsync()
    {
        var exports = new List<string>();
        if (!_exportPdfSelected && !_exportCsvSelected)
            return exports;

        if (_currentMetrics is null)
            return exports;

        var category = GetSelectedCategory();
        var reportData = await _reportsService.BuildReportAsync(category, _currentPeriod, _currentMetrics);
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "EduCRM", "Reports");

        if (_exportPdfSelected)
        {
            var pdfPath = await _reportExportService.ExportPdfAsync(reportData, directory);
            exports.Add($"PDF saved to {pdfPath}");
        }

        if (_exportCsvSelected)
        {
            var csvPath = await _reportExportService.ExportCsvAsync(reportData, directory);
            exports.Add($"CSV saved to {csvPath}");
        }

        return exports;
    }

    private void OnQuickExportPdfClicked(object? sender, EventArgs e)
    {
        _exportPdfSelected = !_exportPdfSelected;
        UpdateQuickExportButtons();
    }

    private void OnQuickExportCsvClicked(object? sender, EventArgs e)
    {
        _exportCsvSelected = !_exportCsvSelected;
        UpdateQuickExportButtons();
    }

    private void UpdateQuickExportButtons()
    {
        if (QuickPdfButton is null || QuickCsvButton is null)
            return;

        StyleQuickButton(QuickPdfButton, _exportPdfSelected);
        StyleQuickButton(QuickCsvButton, _exportCsvSelected);
    }

    private static void StyleQuickButton(Button button, bool isSelected)
    {
        if (isSelected)
        {
            button.BackgroundColor = Color.FromArgb("#DC2626");
            button.TextColor = Colors.White;
            button.BorderColor = Color.FromArgb("#B91C1C");
        }
        else
        {
            button.BackgroundColor = Colors.White;
            button.TextColor = Color.FromArgb("#005BA5");
            button.BorderColor = Color.FromArgb("#D1D5DB");
        }
    }
}
