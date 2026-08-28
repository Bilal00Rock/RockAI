using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;

namespace RockAI.App
{
    public partial class App : Microsoft.Maui.Controls.Application
    {
        // Expose the application's IServiceProvider so pages can resolve view models/services.
        public static IServiceProvider Services { get; set; } = null!;

        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var shell = new AppShell();
            var window = new Window(shell);
            _ = shell.InitializeAsync();
            return window;
        }
    }
}