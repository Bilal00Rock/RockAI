using Microsoft.Extensions.DependencyInjection;
using RockAI.Application.Common.Interfaces;

namespace RockAI.App
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
        }

        public async Task InitializeAsync()
        {
            try
            {
                var session = App.Services.GetRequiredService<IUserSession>();
                await session.LoadAsync();

                await GoToAsync(session.IsAuthenticated ? "//MainPage" : "//LoginPage");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Session initialization failed: {ex}");
                throw;
            }
        }
    }
}
