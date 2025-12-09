using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using MauiAppIT13.Database;
using MauiAppIT13.Models;
using MauiAppIT13.Services;
using MauiAppIT13.Utils;

namespace MauiAppIT13.Pages.Student;

public partial class HomePage : ContentPage
{
    private readonly MessageService _messageService;
    private readonly AnnouncementService _announcementService;
    private readonly TicketService _ticketService;
    private readonly AuthManager _authManager;
    private readonly DbConnection? _dbConnection;
    private bool _isLoadingDashboard;

    public ObservableCollection<Conversation> RecentMessages { get; } = new();
    public ObservableCollection<Announcement> LatestAnnouncements { get; } = new();

    private string _welcomeMessage = "Welcome back, Student!";
    public string WelcomeMessage
    {
        get => _welcomeMessage;
        set => SetProperty(ref _welcomeMessage, value);
    }

    private int _unreadMessagesCount;
    public int UnreadMessagesCount
    {
        get => _unreadMessagesCount;
        set => SetProperty(ref _unreadMessagesCount, value);
    }

    private string _unreadMessagesSubtitle = "You're all caught up";
    public string UnreadMessagesSubtitle
    {
        get => _unreadMessagesSubtitle;
        set => SetProperty(ref _unreadMessagesSubtitle, value);
    }

    private int _newAnnouncementsCount;
    public int NewAnnouncementsCount
    {
        get => _newAnnouncementsCount;
        set => SetProperty(ref _newAnnouncementsCount, value);
    }

    private string _newAnnouncementsSubtitle = "No new announcements yet";
    public string NewAnnouncementsSubtitle
    {
        get => _newAnnouncementsSubtitle;
        set => SetProperty(ref _newAnnouncementsSubtitle, value);
    }

    private int _activeTicketsCount;
    public int ActiveTicketsCount
    {
        get => _activeTicketsCount;
        set => SetProperty(ref _activeTicketsCount, value);
    }

    private string _activeTicketsSubtitle = "No active tickets";
    public string ActiveTicketsSubtitle
    {
        get => _activeTicketsSubtitle;
        set => SetProperty(ref _activeTicketsSubtitle, value);
    }

    public HomePage()
    {
        InitializeComponent();

        _messageService = AppServiceProvider.GetService<MessageService>()
            ?? throw new InvalidOperationException("MessageService not registered");
        _announcementService = AppServiceProvider.GetService<AnnouncementService>()
            ?? throw new InvalidOperationException("AnnouncementService not available");

        _dbConnection = AppServiceProvider.GetService<DbConnection>();
        _ticketService = AppServiceProvider.GetService<TicketService>()
            ?? throw new InvalidOperationException("TicketService not available");

        _authManager = AppServiceProvider.GetService<AuthManager>() ?? new AuthManager();

        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDashboardDataAsync();
    }

    private async Task LoadDashboardDataAsync()
    {
        if (_isLoadingDashboard)
            return;

        _isLoadingDashboard = true;
        try
        {
            var currentUser = _authManager.CurrentUser;
            if (currentUser is null && _dbConnection is not null)
            {
                currentUser = await _dbConnection.GetUserByEmailAsync("student@university.edu");
                if (currentUser is not null)
                {
                    _authManager.SetAuthenticatedUser(currentUser);
                }
            }

            if (currentUser is null)
            {
                await DisplayAlert("Error", "Unable to load dashboard without an authenticated user.", "OK");
                return;
            }

            WelcomeMessage = $"Welcome back, {currentUser.DisplayName ?? currentUser.Email}!";

            var userId = currentUser.Id;
            var unreadTask = _messageService.GetUnreadMessageSummaryAsync(userId);
            var conversationsTask = _messageService.GetConversationsAsync(userId);
            var announcementsTask = _announcementService.GetAnnouncementsAsync(25, userId);
            var ticketsTask = _ticketService.GetStudentTicketsAsync(userId);

            await Task.WhenAll(unreadTask, conversationsTask, announcementsTask, ticketsTask);

            var unreadSummary = await unreadTask;
            var conversations = await conversationsTask;
            var announcements = await announcementsTask;
            var tickets = await ticketsTask;

            UpdateMessagesSection(unreadSummary, conversations);
            UpdateAnnouncementsSection(announcements);
            UpdateTicketsSection(tickets);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"HomePage: Failed to load dashboard - {ex.Message}");
        }
        finally
        {
            _isLoadingDashboard = false;
        }
    }

    private void UpdateMessagesSection((int TotalUnread, int AdvisorUnread) summary, IEnumerable<Conversation> conversations)
    {
        UnreadMessagesCount = summary.TotalUnread;
        UnreadMessagesSubtitle = summary.TotalUnread switch
        {
            0 => "You're all caught up",
            _ when summary.AdvisorUnread > 0 => $"{summary.AdvisorUnread} from advisors",
            _ => "No advisor messages"
        };

        RecentMessages.Clear();
        foreach (var conversation in conversations
                     .OrderByDescending(c => c.LastMessageTime)
                     .Take(3))
        {
            RecentMessages.Add(conversation);
        }
    }

    private void UpdateAnnouncementsSection(IEnumerable<Announcement> announcements)
    {
        var studentAnnouncements = announcements
            .Where(a => a.IsPublished &&
                        (a.Visibility.Equals("all", StringComparison.OrdinalIgnoreCase) ||
                         a.Visibility.Equals("students", StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(a => a.CreatedAt)
            .ToList();

        var unviewedCount = studentAnnouncements.Count(a => !a.HasViewed);

        NewAnnouncementsCount = unviewedCount;
        NewAnnouncementsSubtitle = unviewedCount switch
        {
            0 => "You're all caught up",
            1 => "1 announcement to read",
            _ => $"{unviewedCount} announcements to read"
        };

        LatestAnnouncements.Clear();
        foreach (var announcement in studentAnnouncements.Take(3))
        {
            LatestAnnouncements.Add(announcement);
        }
    }

    private void UpdateTicketsSection(IEnumerable<Ticket> tickets)
    {
        var openCount = tickets.Count(t => t.Status?.Equals("open", StringComparison.OrdinalIgnoreCase) == true);
        var inProgressCount = tickets.Count(t => t.Status?.Equals("in_progress", StringComparison.OrdinalIgnoreCase) == true);
        var activeCount = tickets.Count(t => t.Status?.Equals("resolved", StringComparison.OrdinalIgnoreCase) != true);

        ActiveTicketsCount = activeCount;
        ActiveTicketsSubtitle = activeCount > 0
            ? $"{openCount} open • {inProgressCount} in progress"
            : "No active tickets";
    }

    private async void OnProfileTapped(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new ProfilePage(), false);
    }

    private async void OnMessagesTapped(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new MessagesPage(), false);
    }

    private async void OnClassesTapped(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new StudentClassesPage(), false);
    }

    private async void OnAnnouncementsTapped(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new AnnouncementsPage(), false);
    }

    private async void OnTicketsTapped(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new TicketsPage(), false);
    }

    private async void OnViewAllMessagesTapped(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new MessagesPage(), false);
    }

    private async void OnViewAllAnnouncementsTapped(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new AnnouncementsPage(), false);
    }

    private async void OnLogoutTapped(object? sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");
        if (confirm)
        {
            await Shell.Current.GoToAsync("//MainPage");
        }
    }

    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
            return false;

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}