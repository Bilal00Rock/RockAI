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

        _viewModel.MessagesChanged += ScrollToBottom;

    }

    private async void OnPageAppearing(object? sender, EventArgs e)
    {
        await _viewModel.LoadAsync();
    }
    private void ScrollToBottom()
    {
        if (_viewModel.Messages.Count == 0)
            return;

        MessagesCollectionView.ScrollTo(
            _viewModel.Messages.Count - 1,
            position: ScrollToPosition.End,
            animate: false);
    }
}