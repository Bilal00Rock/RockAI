using Microsoft.Extensions.DependencyInjection;
using RockAI.App.ViewModels;

namespace RockAI.App.Views;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;

    public MainPage()
    {
        InitializeComponent();

        _viewModel = RockAI.App.App.Services.GetRequiredService<MainViewModel>();
        BindingContext = _viewModel;
    }

    private async void OnPageAppearing(object? sender, EventArgs e)
    {
        await _viewModel.LoadAsync();
        // After load, ensure we land at the true bottom of the message list.
        ChatViewControl.ScrollToBottomAfterLoad();
    }

    protected override void OnDisappearing()
    {
        ChatViewControl.Cleanup();
        base.OnDisappearing();
    }
}
