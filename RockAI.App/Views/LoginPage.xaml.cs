using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using RockAI.App.ViewModels;

namespace RockAI.App.Views;

public partial class LoginPage : ContentPage
{
    private readonly LoginViewModel _viewModel;

    public LoginPage()
    {

        InitializeComponent();
        _viewModel =
           RockAI.App.App.Services
               .GetRequiredService<LoginViewModel>();

        BindingContext = _viewModel;

        _viewModel.LoginSucceeded += OnLoginSucceeded;
        // Resolve the ViewModel from the app's service provider
        //BindingContext = RockAI.App.App.Services.GetRequiredService<LoginViewModel>();
    }
    private async void OnLoginSucceeded(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//MainPage");
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
    }
}
