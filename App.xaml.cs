namespace MauiAppIT13
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());
            
            // Set window to maximized state on startup
            window.Created += (s, e) =>
            {
                // Get the display information
                var displayInfo = DeviceDisplay.Current.MainDisplayInfo;
                
                // Set window size to match display dimensions
                window.Width = displayInfo.Width / displayInfo.Density;
                window.Height = displayInfo.Height / displayInfo.Density;
                window.X = 0;
                window.Y = 0;
            };
            
            return window;
        }
    }
}