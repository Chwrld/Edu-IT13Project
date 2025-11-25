namespace MauiAppIT13.Pages.Student;

public partial class TicketsPage : ContentPage
    {
        public TicketsPage()
        {
            InitializeComponent();
        }

        private async void OnProfileTapped(object? sender, EventArgs e)
        {
            await Navigation.PushAsync(new ProfilePage());
        }

        private async void OnHomeTapped(object? sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async void OnMessagesTapped(object? sender, EventArgs e)
        {
            await Navigation.PushAsync(new MessagesPage());
        }

        private async void OnAnnouncementsTapped(object? sender, EventArgs e)
        {
            await Navigation.PushAsync(new AnnouncementsPage());
        }

        private void OnNewTicketClicked(object? sender, EventArgs e)
        {
            // Show modal
            ModalOverlay.IsVisible = true;
        }

        private void OnCloseModalTapped(object? sender, EventArgs e)
        {
            // Hide modal
            ModalOverlay.IsVisible = false;
            ClearForm();
        }

        private void OnOverlayTapped(object? sender, EventArgs e)
        {
            // Close modal when clicking outside
            ModalOverlay.IsVisible = false;
            ClearForm();
        }

        private void OnCancelTicketClicked(object? sender, EventArgs e)
        {
            // Hide modal and clear form
            ModalOverlay.IsVisible = false;
            ClearForm();
        }

        private async void OnSubmitTicketClicked(object? sender, EventArgs e)
        {
            string title = TitleEntry.Text?.Trim() ?? string.Empty;
            string category = CategoryPicker.SelectedIndex > 0 ? CategoryPicker.Items[CategoryPicker.SelectedIndex] : string.Empty;
            string priority = PriorityPicker.SelectedIndex > 0 ? PriorityPicker.Items[PriorityPicker.SelectedIndex] : string.Empty;
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

            // TODO: Implement actual ticket submission logic
            await DisplayAlert("Success", $"Ticket submitted successfully!\n\nTitle: {title}\nCategory: {category}\nPriority: {priority}", "OK");
            
            // Hide modal and clear form
            ModalOverlay.IsVisible = false;
            ClearForm();
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
            // Navigate back to login page by popping to root
            await Navigation.PopToRootAsync();
        }
    }
