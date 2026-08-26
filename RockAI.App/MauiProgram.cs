using Microsoft.Extensions.Logging;
using RockAI.Infrastructure;
using RockAI.App.Services.Authentication;

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

            // Token storage for attaching JWT to requests
            builder.Services.AddSingleton<ITokenStorage, SecureTokenStorage>();
            // Authentication service and view models
            builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();
            builder.Services.AddTransient<ViewModels.LoginViewModel>();
            // HttpClient configured to attach Authorization header when a token exists.
            builder.Services.AddSingleton(sp =>
            {
                var tokenStorage = sp.GetRequiredService<ITokenStorage>();
                var handler = new AuthenticatedHttpClientHandler(tokenStorage)
                {
                    InnerHandler = new HttpClientHandler()
                };

                // Replace the BaseAddress with your API base URL or configure it externally.
                return new HttpClient(handler)
                {
                    BaseAddress = new Uri("https://api.example.com/")
                };
            });

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
