#pragma warning disable CA1416
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using MauiAppIT13.Controllers;
using MauiAppIT13.Database;
using MauiAppIT13.Services;
using MauiAppIT13.Utils;
using System.Diagnostics;

namespace MauiAppIT13
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            
            // Load configuration from appsettings.json with fallback
            IConfiguration config;
            try
            {
                config = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load appsettings.json: {ex.Message}. Using empty configuration.");
                config = new ConfigurationBuilder().Build();
            }

            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("Montserrat-Regular.ttf", "MontserratRegular");
                    fonts.AddFont("Montserrat-Bold.ttf", "MontserratBold");
                });

            // Register configuration
            builder.Services.AddSingleton<IConfiguration>(config);

            // Register services - Use SQL Server for persistent storage
            builder.Services.AddSingleton<DbConnection>(sp => 
                new SqlServerDbConnection(sp.GetRequiredService<IConfiguration>()));
            builder.Services.AddSingleton<PasswordHasher>();
            builder.Services.AddSingleton<ActivityLogger>();
            builder.Services.AddSingleton<ValidationHelper>();
            builder.Services.AddSingleton<AuthManager>();
            builder.Services.AddSingleton<AuthService>();
            builder.Services.AddSingleton<AuthController>();
            builder.Services.AddSingleton<UserController>();
            builder.Services.AddSingleton<MessageService>();
            builder.Services.AddSingleton<TicketService>();
            builder.Services.AddSingleton<AnnouncementService>();

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            var app = builder.Build();
            
            // Initialize service provider and seed database in background
            AppServiceProvider.Initialize(app.Services);
            SeedDatabaseInBackground(app.Services);

            return app;
        }

        private static void SeedDatabaseInBackground(IServiceProvider services)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = services.CreateScope();
                    var dbConnection = scope.ServiceProvider.GetRequiredService<DbConnection>();
                    var passwordHasher = scope.ServiceProvider.GetRequiredService<PasswordHasher>();
                    await DbInitializer.EnsureSeedDataAsync(dbConnection, passwordHasher);
                    Debug.WriteLine("Database seeding completed successfully.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Database seed failed: {ex.Message}");
                }
            });
        }
    }
}
