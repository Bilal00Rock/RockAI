using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using RockAI.App.ViewModels;

namespace RockAI.App.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();

        // Resolve the ViewModel from the app's service provider
        BindingContext = RockAI.App.App.Services.GetRequiredService<LoginViewModel>();
    }
}
