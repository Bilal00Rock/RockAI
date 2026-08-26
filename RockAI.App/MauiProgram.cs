using Microsoft.Extensions.Logging;
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

            // Register infrastructure (EF Core, repositories, unit of work, etc.)
            builder.Services.AddInfrastructure();

            builder.Services.AddTransient<ViewModels.LoginViewModel>();

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
            catch
            {
                // Initialization failed; swallow to avoid crashing app at startup. Log as needed.
            }

            return app;
        }
    }
}
