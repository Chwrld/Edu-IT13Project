using System.Collections.ObjectModel;
using System.Runtime.Versioning;
using Microsoft.Maui.Controls.Shapes;
using MauiAppIT13.Database;
using MauiAppIT13.Models;
using MauiAppIT13.Services;
using MauiAppIT13.Utils;

namespace MauiAppIT13.Pages.Teacher;

[SupportedOSPlatform("windows10.0.17763.0")]
[SupportedOSPlatform("android21.0")]
public partial class TeacherMessagesPage : ContentPage
{
    private readonly MessageService _messageService;
    private readonly AuthManager _authManager;
    private readonly DbConnection _dbConnection;
    private ObservableCollection<Conversation> _conversations = new();
    private List<Message> _currentMessages = new();
    private Guid _currentUserId;
    private Conversation? _selectedConversation;
    private bool _isInitialized = false;
    private bool _isSendingNewMessage = false;

    public TeacherMessagesPage()
    {
        InitializeComponent();
        _dbConnection = AppServiceProvider.GetService<DbConnection>() ?? throw new InvalidOperationException("DbConnection not found");
        _messageService = AppServiceProvider.GetService<MessageService>() ?? new MessageService(_dbConnection);
        _authManager = AppServiceProvider.GetService<AuthManager>() ?? new AuthManager();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        // Ensure page is properly initialized only once
        if (!_isInitialized)
        {
            _isInitialized = true;
            _ = LoadConversations();
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _isInitialized = false;
        MessagesStackLayout?.Children.Clear();
        MessagesScrollView.IsVisible = false;
        NoMessagesPlaceholder.IsVisible = true;
    }

    private async void OnDashboardTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//TeacherHomePage");
    }

    private async void OnClassesTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//TeacherClassesPage");
    }

    private async void OnNewMessageClicked(object sender, EventArgs e)
    {
        if (!_isInitialized)
            return;

        ShowComposeOverlay();
    }

    private async void OnAnnouncementsTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//TeacherAnnouncementsPage");
    }

    private async void OnTicketsTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//TeacherTicketsPage");
    }

    private void OnAllTabTapped(object sender, EventArgs e)
    {
        // Update tab styles
        AllTab.BackgroundColor = Color.FromArgb("#059669");
        if (AllTab.Content is Label allLabel)
        {
            allLabel.TextColor = Colors.White;
            allLabel.FontAttributes = FontAttributes.Bold;
        }

        StudentsTab.BackgroundColor = Colors.Transparent;
        if (StudentsTab.Content is Label studentsLabel)
        {
            studentsLabel.TextColor = Color.FromArgb("#059669");
            studentsLabel.FontAttributes = FontAttributes.None;
        }

        StaffTab.BackgroundColor = Colors.Transparent;
        if (StaffTab.Content is Label staffLabel)
        {
            staffLabel.TextColor = Color.FromArgb("#059669");
            staffLabel.FontAttributes = FontAttributes.None;
        }

        // Filter conversations (to be implemented)
    }

    private void OnStudentsTabTapped(object sender, EventArgs e)
    {
        // Update tab styles
        StudentsTab.BackgroundColor = Color.FromArgb("#059669");
        if (StudentsTab.Content is Label studentsLabel)
        {
            studentsLabel.TextColor = Colors.White;
            studentsLabel.FontAttributes = FontAttributes.Bold;
        }

        AllTab.BackgroundColor = Colors.Transparent;
        if (AllTab.Content is Label allLabel)
        {
            allLabel.TextColor = Color.FromArgb("#059669");
            allLabel.FontAttributes = FontAttributes.None;
        }

        StaffTab.BackgroundColor = Colors.Transparent;
        if (StaffTab.Content is Label staffLabel)
        {
            staffLabel.TextColor = Color.FromArgb("#059669");
            staffLabel.FontAttributes = FontAttributes.None;
        }

        // Filter to show only student conversations
    }

    private void OnStaffTabTapped(object sender, EventArgs e)
    {
        // Update tab styles
        StaffTab.BackgroundColor = Color.FromArgb("#059669");
        if (StaffTab.Content is Label staffLabel)
        {
            staffLabel.TextColor = Colors.White;
            staffLabel.FontAttributes = FontAttributes.Bold;
        }

        AllTab.BackgroundColor = Colors.Transparent;
        if (AllTab.Content is Label allLabel)
        {
            allLabel.TextColor = Color.FromArgb("#059669");
            allLabel.FontAttributes = FontAttributes.None;
        }

        StudentsTab.BackgroundColor = Colors.Transparent;
        if (StudentsTab.Content is Label studentsLabel)
        {
            studentsLabel.TextColor = Color.FromArgb("#059669");
            studentsLabel.FontAttributes = FontAttributes.None;
        }

        // Filter to show only staff conversations
    }

    private async void OnConversationsSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var selectedConversation = e.CurrentSelection.FirstOrDefault() as Conversation;
        if (selectedConversation == null)
            return;

        await SelectConversation(selectedConversation);
        if (sender is CollectionView collectionView)
        {
            collectionView.SelectedItem = null;
        }
    }

    private async void OnAttachFileTapped(object sender, EventArgs e)
    {
        await DisplayAlert("Attach File", "File attachment feature coming soon!", "OK");
    }

    private void OnComposeCancelClicked(object sender, EventArgs e)
    {
        HideComposeOverlay();
    }

    private async void OnComposeSendClicked(object sender, EventArgs e)
    {
        if (_isSendingNewMessage)
            return;

        var recipientEmail = ComposeRecipientEntry.Text?.Trim() ?? string.Empty;
        var messageContent = ComposeMessageEditor.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(recipientEmail) || !recipientEmail.Contains('@'))
        {
            SetComposeError("Please enter a valid recipient email.");
            return;
        }

        if (string.IsNullOrWhiteSpace(messageContent))
        {
            SetComposeError("Message content cannot be empty.");
            return;
        }

        try
        {
            _isSendingNewMessage = true;
            SetComposeError(string.Empty);

            var recipient = await _dbConnection.GetUserByEmailAsync(recipientEmail);
            if (recipient == null)
            {
                SetComposeError("No user found with that email address.");
                return;
            }

            if (recipient.Id == _currentUserId)
            {
                SetComposeError("You cannot send a message to yourself.");
                return;
            }

            var success = await _messageService.SendMessageAsync(_currentUserId, recipient.Id, messageContent);
            if (!success)
            {
                SetComposeError("Failed to send the message. Please try again.");
                return;
            }

            HideComposeOverlay();
            await LoadConversations();
            var newConversation = _conversations.FirstOrDefault(c => c.ParticipantId == recipient.Id);
            if (newConversation != null)
            {
                await SelectConversation(newConversation);
            }
        }
        catch (Exception ex)
        {
            SetComposeError($"Error sending message: {ex.Message}");
        }
        finally
        {
            _isSendingNewMessage = false;
        }
    }

    private async void OnSendMessageTapped(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(MessageEntry.Text) || _selectedConversation == null)
        {
            return;
        }

        var content = MessageEntry.Text.Trim();
        var success = await _messageService.SendMessageAsync(_currentUserId, _selectedConversation.ParticipantId, content);
        if (success)
        {
            MessageEntry.Text = string.Empty;
            await SelectConversation(_selectedConversation);
        }
        else
        {
            await DisplayAlert("Error", "Failed to send message", "OK");
        }
    }

    private async void OnLogoutTapped(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");
        if (confirm)
        {
            await Shell.Current.GoToAsync("//MainPage");
        }
    }

    private async Task LoadConversations()
    {
        var currentUser = _authManager.CurrentUser;
        if (currentUser == null)
        {
            currentUser = await _dbConnection.GetUserByEmailAsync("teacher@university.edu");
            if (currentUser == null)
            {
                await DisplayAlert("Error", "User not authenticated", "OK");
                return;
            }
        }

        _currentUserId = currentUser.Id;
        var conversations = await _messageService.GetConversationsAsync(_currentUserId);
        _conversations = conversations;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            ConversationsCollectionView.ItemsSource = _conversations;
            var hasConversations = _conversations.Count > 0;
            NoMessagesPlaceholder.IsVisible = true;
            MessagesScrollView.IsVisible = false;
            ChatParticipantNameLabel.Text = hasConversations ? "Select a conversation" : "No conversations";
            ChatParticipantRoleLabel.Text = hasConversations ? string.Empty : "Start a chat to begin messaging";
            ChatAvatarBorder.BackgroundColor = Color.Parse("#DBEAFE");
            ChatAvatarInitialsLabel.Text = "--";
        });
    }

    private async Task SelectConversation(Conversation conversation)
    {
        _selectedConversation = conversation;
        var messages = await _messageService.GetConversationMessagesAsync(conversation.Id);
        _currentMessages = messages;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            var hasMessages = _currentMessages.Count > 0;
            NoMessagesPlaceholder.IsVisible = !hasMessages;
            MessagesScrollView.IsVisible = hasMessages;

            if (hasMessages)
            {
                ChatAvatarBorder.BackgroundColor = Color.Parse(conversation.AvatarColor);
                ChatAvatarInitialsLabel.Text = conversation.Initials;
                ChatParticipantNameLabel.Text = conversation.ParticipantName;
                ChatParticipantRoleLabel.Text = conversation.ParticipantRole;

                MessagesStackLayout.Children.Clear();
                foreach (var message in _currentMessages)
                {
                    var view = CreateMessageView(message);
                    MessagesStackLayout.Children.Add(view);
                }

                _ = MessagesScrollView.ScrollToAsync(MessagesStackLayout, ScrollToPosition.End, true);
            }
            else
            {
                ChatParticipantNameLabel.Text = "Select a conversation";
                ChatParticipantRoleLabel.Text = string.Empty;
                ChatAvatarInitialsLabel.Text = "--";
            }
        });
    }

    private void ShowComposeOverlay()
    {
        ComposeRecipientEntry.Text = string.Empty;
        ComposeMessageEditor.Text = string.Empty;
        SetComposeError(string.Empty);
        ComposeOverlay.IsVisible = true;
    }

    private void HideComposeOverlay()
    {
        ComposeOverlay.IsVisible = false;
        ComposeRecipientEntry.Text = string.Empty;
        ComposeMessageEditor.Text = string.Empty;
        SetComposeError(string.Empty);
    }

    private void SetComposeError(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            ComposeErrorLabel.IsVisible = false;
            ComposeErrorLabel.Text = string.Empty;
        }
        else
        {
            ComposeErrorLabel.IsVisible = true;
            ComposeErrorLabel.Text = message;
        }
    }

    private View CreateMessageView(Message message)
    {
        var isFromCurrentUser = message.SenderId == _currentUserId;

        var layout = new HorizontalStackLayout
        {
            Spacing = 10,
            HorizontalOptions = isFromCurrentUser ? LayoutOptions.End : LayoutOptions.Start
        };

        if (!isFromCurrentUser)
        {
            var avatar = new Border
            {
                BackgroundColor = Color.Parse(message.AvatarColor),
                WidthRequest = 32,
                HeightRequest = 32,
                StrokeThickness = 0,
                VerticalOptions = LayoutOptions.End,
                Content = new Label
                {
                    Text = message.SenderInitials,
                    TextColor = Colors.White,
                    FontSize = 11,
                    FontAttributes = FontAttributes.Bold,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                }
            };
            avatar.StrokeShape = new Ellipse();
            layout.Add(avatar);
        }

        var messageStack = new VerticalStackLayout { Spacing = 4, MaximumWidthRequest = 500 };
        var bubble = new Border
        {
            BackgroundColor = isFromCurrentUser ? Color.Parse("#059669") : Colors.White,
            Padding = new Thickness(12, 10),
            StrokeThickness = 0,
            HorizontalOptions = isFromCurrentUser ? LayoutOptions.End : LayoutOptions.Start,
            Content = new Label
            {
                Text = message.Content,
                FontSize = 14,
                TextColor = isFromCurrentUser ? Colors.White : Color.Parse("#2C3E50"),
                LineBreakMode = LineBreakMode.WordWrap
            }
        };
        bubble.StrokeShape = new RoundRectangle
        {
            CornerRadius = isFromCurrentUser ? new CornerRadius(12, 12, 2, 12) : new CornerRadius(12, 12, 12, 2)
        };

        messageStack.Add(bubble);
        messageStack.Add(new Label
        {
            Text = message.CreatedAtLocal.ToString("h:mm tt"),
            FontSize = 11,
            TextColor = Color.Parse("#9CA3AF"),
            HorizontalOptions = isFromCurrentUser ? LayoutOptions.End : LayoutOptions.Start,
            Margin = isFromCurrentUser ? new Thickness(0, 0, 8, 0) : new Thickness(8, 0, 0, 0)
        });

        layout.Add(messageStack);
        return layout;
    }
    
}
