using System.Collections.ObjectModel;
using Microsoft.Maui.Controls.Shapes;
using MauiAppIT13.Database;
using MauiAppIT13.Models;
using MauiAppIT13.Services;
using MauiAppIT13.Utils;

namespace MauiAppIT13.Pages.Student;

public partial class MessagesPage : ContentPage
{
    private readonly MessageService _messageService;
    private readonly AuthManager _authManager;
    private ObservableCollection<Conversation> _conversations = new();
    private List<Message> _currentMessages = new();
    private Guid _currentUserId;
    private Conversation? _selectedConversation;

    public MessagesPage()
    {
        InitializeComponent();
        System.Diagnostics.Debug.WriteLine("MessagesPage: Constructor called");
        
        var dbConnection = AppServiceProvider.GetService<DbConnection>();
        _messageService = AppServiceProvider.GetService<MessageService>() ?? new MessageService(dbConnection ?? throw new InvalidOperationException("DbConnection not found"));
        _authManager = AppServiceProvider.GetService<AuthManager>() ?? new AuthManager();
        
        System.Diagnostics.Debug.WriteLine("MessagesPage: Services initialized");
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        System.Diagnostics.Debug.WriteLine("MessagesPage: OnAppearing called");
        _ = LoadConversations();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        System.Diagnostics.Debug.WriteLine("MessagesPage: OnDisappearing called");
        try
        {
            // Clear messages to free memory
            MainThread.BeginInvokeOnMainThread(() =>
            {
                MessagesStackLayout?.Children.Clear();
                NoMessagesPlaceholder.IsVisible = true;
                MessagesScrollView.IsVisible = false;
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MessagesPage: Error in OnDisappearing - {ex.Message}");
        }
    }

    private async Task LoadConversations()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("MessagesPage: LoadConversations started");
            var currentUser = _authManager.CurrentUser;
            System.Diagnostics.Debug.WriteLine($"MessagesPage: Current user = {currentUser?.Email}");
            
            if (currentUser == null)
            {
                System.Diagnostics.Debug.WriteLine("MessagesPage: User not authenticated");
                await DisplayAlert("Error", "User not authenticated", "OK");
                return;
            }

            _currentUserId = currentUser.Id;
            System.Diagnostics.Debug.WriteLine($"MessagesPage: Loading conversations for user {_currentUserId}");
            
            var conversations = await _messageService.GetConversationsAsync(_currentUserId);
            System.Diagnostics.Debug.WriteLine($"MessagesPage: Got {conversations.Count} conversations");
            
            _conversations = conversations;
            
            // Bind conversations to CollectionView
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ConversationsCollectionView.ItemsSource = _conversations;
                System.Diagnostics.Debug.WriteLine($"MessagesPage: Bound {_conversations.Count} conversations to CollectionView");
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MessagesPage: Error - {ex.GetType().Name}: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"MessagesPage: Stack trace: {ex.StackTrace}");
            await DisplayAlert("Error", $"Failed to load conversations: {ex.Message}", "OK");
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
            System.Diagnostics.Debug.WriteLine($"MessagesPage: Error navigating to profile - {ex.Message}");
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
            System.Diagnostics.Debug.WriteLine($"MessagesPage: Error navigating home - {ex.Message}");
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
            System.Diagnostics.Debug.WriteLine($"MessagesPage: Error navigating to announcements - {ex.Message}");
        }
    }

    private async void OnTicketsTapped(object? sender, EventArgs e)
    {
        try
        {
            await Shell.Current.GoToAsync("//TicketsPage");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MessagesPage: Error navigating to tickets - {ex.Message}");
        }
    }

    private async void OnConversationsSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        try
        {
            var selectedConversation = e.CurrentSelection.FirstOrDefault() as Conversation;
            if (selectedConversation == null)
            {
                System.Diagnostics.Debug.WriteLine("MessagesPage: No conversation selected");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"MessagesPage: SelectionChanged - {selectedConversation.ParticipantName}");
            await SelectConversation(selectedConversation);

            if (sender is CollectionView collectionView)
            {
                collectionView.SelectedItem = null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MessagesPage: Error in OnConversationsSelectionChanged - {ex.Message}");
            await DisplayAlert("Error", $"Error: {ex.Message}", "OK");
        }
    }

    private async Task SelectConversation(Conversation conversation)
    {
        try
        {
            _selectedConversation = conversation;
            System.Diagnostics.Debug.WriteLine($"MessagesPage: Selected conversation with {conversation.ParticipantName}");
            
            // Load messages for this conversation
            var messages = await _messageService.GetConversationMessagesAsync(conversation.Id);
            _currentMessages = messages;
            
            System.Diagnostics.Debug.WriteLine($"MessagesPage: Loaded {messages.Count} messages");
            
            // Update UI with messages
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Hide placeholder and show messages
                NoMessagesPlaceholder.IsVisible = false;
                MessagesScrollView.IsVisible = true;
                
                // Clear and populate messages
                MessagesStackLayout.Children.Clear();
                foreach (var message in _currentMessages)
                {
                    var messageView = CreateMessageView(message);
                    MessagesStackLayout.Children.Add(messageView);
                }
                
                // Scroll to bottom
                MessagesScrollView.ScrollToAsync(MessagesStackLayout, ScrollToPosition.End, true);
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MessagesPage: Error selecting conversation - {ex.Message}");
            await DisplayAlert("Error", $"Failed to load conversation: {ex.Message}", "OK");
        }
    }

    private View CreateMessageView(Message message)
    {
        var isFromCurrentUser = message.SenderId == _currentUserId;
        
        var layout = new HorizontalStackLayout
        {
            Spacing = 12,
            HorizontalOptions = isFromCurrentUser ? LayoutOptions.End : LayoutOptions.Start,
            Margin = new Thickness(0, 8)
        };
        
        if (!isFromCurrentUser)
        {
            // Received message - show avatar
            var avatar = new Border
            {
                BackgroundColor = Color.Parse(message.AvatarColor),
                WidthRequest = 40,
                HeightRequest = 40,
                StrokeThickness = 0,
                VerticalOptions = LayoutOptions.Start,
                Content = new Label
                {
                    Text = message.SenderInitials,
                    TextColor = Colors.White,
                    FontSize = 14,
                    FontAttributes = FontAttributes.Bold,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                }
            };
            avatar.StrokeShape = new Ellipse();
            layout.Add(avatar);
        }
        
        var messageContent = new VerticalStackLayout { Spacing = 6 };
        
        var messageBubble = new Border
        {
            BackgroundColor = isFromCurrentUser ? Color.Parse("#0891B2") : Colors.White,
            Padding = new Thickness(15, 12),
            StrokeThickness = 0,
            Content = new Label
            {
                Text = message.Content,
                FontSize = 14,
                TextColor = isFromCurrentUser ? Colors.White : Color.Parse("#2C3E50"),
                LineBreakMode = LineBreakMode.WordWrap,
                MaximumWidthRequest = 500
            }
        };
        messageBubble.StrokeShape = new RoundRectangle 
        { 
            CornerRadius = isFromCurrentUser ? new CornerRadius(12, 12, 4, 12) : new CornerRadius(12, 12, 12, 4)
        };
        
        messageContent.Add(messageBubble);
        
        var timeLabel = new Label
        {
            Text = message.CreatedAtUtc.ToString("h:mm tt"),
            FontSize = 11,
            TextColor = Color.Parse("#9CA3AF"),
            Margin = new Thickness(12, 0, 0, 0)
        };
        messageContent.Add(timeLabel);
        
        layout.Add(new VerticalStackLayout { Children = { messageContent }, Spacing = 0 });
        
        return layout;
    }

    private async void OnChatMenuTapped(object? sender, EventArgs e)
    {
        await DisplayAlert("Menu", "Chat options - Coming soon!", "OK");
    }

    private async void OnAttachFileTapped(object? sender, EventArgs e)
    {
        await DisplayAlert("Attach", "File attachment - Coming soon!", "OK");
    }

    private async void OnSendMessageTapped(object? sender, EventArgs e)
    {
        try
        {
            string message = MessageEntry.Text?.Trim() ?? string.Empty;
            
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            if (_selectedConversation == null)
            {
                await DisplayAlert("Error", "Please select a conversation first", "OK");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"MessagesPage: Sending message to {_selectedConversation.ParticipantId}");
            
            // Send message to the database
            var success = await _messageService.SendMessageAsync(_currentUserId, _selectedConversation.ParticipantId, message);
            if (success)
            {
                MessageEntry.Text = string.Empty;
                System.Diagnostics.Debug.WriteLine("MessagesPage: Message sent successfully");
                
                // Reload messages to show the new message
                await SelectConversation(_selectedConversation);
            }
            else
            {
                await DisplayAlert("Error", "Failed to send message", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MessagesPage: Error sending message - {ex.Message}");
            await DisplayAlert("Error", $"Error: {ex.Message}", "OK");
        }
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