using Microsoft.Extensions.Logging;
using RockAI.App.Services.Authentication;
using RockAI.App.ViewModels;
using RockAI.App.Views;
using RockAI.Application;
using RockAI.Application.Authentication;
using RockAI.Application.Common.Interfaces;
using RockAI.Application.Conversations;
using RockAI.Application.Messages;
using RockAI.Infrastructure;

namespace RockAI.App
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });
            builder.Services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(ApplicationAssemblyMarker).Assembly);
            });
            // Register infrastructure (EF Core, repositories, unit of work, etc.)
            var databasePath = Path.Combine(
    FileSystem.AppDataDirectory,
    "RockAI.db");

            builder.Services.AddInfrastructure(databasePath);
            builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
            builder.Services.AddScoped<IConversationService, ConversationService>();
            builder.Services.AddScoped<IMessageService, MessageService>();
            builder.Services.AddSingleton<IUserSession, UserSession>();
            //builder.Services.AddTransient<ViewModels.LoginViewModel>();
            //builder.Services.AddTransient<ViewModels.MainViewModel>();

            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<MainViewModel>();

            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<MainPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            // Expose the DI container to the app for page/viewmodel resolution
            RockAI.App.App.Services = app.Services;

            // Initialize database and seed default data (if needed)
            try
            {
                using var scope = app.Services.CreateScope();
                var initializer = scope.ServiceProvider.GetService<RockAI.Infrastructure.Common.Persistence.DatabaseInitializer>();
                if (initializer is not null)
                {
                    initializer.InitializeAsync().GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(
                    $"Database initialization failed: {ex}");
#endif
                throw;
            }

            return app;
        }
    }
}
