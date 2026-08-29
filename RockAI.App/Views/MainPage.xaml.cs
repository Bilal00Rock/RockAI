using Microsoft.Extensions.DependencyInjection;
using RockAI.App.ViewModels;

namespace RockAI.App.Views;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;

    private CancellationTokenSource? _scrollCts;
    private bool _userHasScrolledAway;
    private bool _isAutoScrolling;

    public MainPage()
    {
        InitializeComponent();

        _viewModel = RockAI.App.App.Services.GetRequiredService<MainViewModel>();

        BindingContext = _viewModel;

        _viewModel.MessagesChanged += OnMessagesChanged;
        MessagesCollectionView.Scrolled += OnMessagesScrolled;
    }

    private async void OnPageAppearing(object? sender, EventArgs e)
    {
        await _viewModel.LoadAsync();

        _userHasScrolledAway = false;

        await Task.Delay(100);

        await ForceScrollToBottomAsync();
    }

    private void OnMessagesChanged()
    {
        // If the user deliberately moved away from the bottom,
        // do not fight them.
        //if (_userHasScrolledAway)
        //    return;

        ScheduleScroll();
    }

    private void OnMessagesScrolled(object? sender, ItemsViewScrolledEventArgs e)
    {
        if (_isAutoScrolling)
            return;

        var lastIndex = _viewModel.Messages.Count - 1;

        if (lastIndex < 0)
            return;

        /*
         * We consider the user to be at the bottom if the last
         * message is visible.
         */
        _userHasScrolledAway =
            e.LastVisibleItemIndex < lastIndex;
    }

    private void ScheduleScroll()
    {
        _scrollCts?.Cancel();
        _scrollCts?.Dispose();

        _scrollCts = new CancellationTokenSource();

        var token = _scrollCts.Token;

        Dispatcher.Dispatch(async () =>
        {
            try
            {
                // Wait for CollectionView to process the collection
                // change and perform its layout.
                await Task.Delay(50, token);

                if (token.IsCancellationRequested)
                    return;

                await ForceScrollToBottomAsync(token);
            }
            catch (OperationCanceledException)
            {
            }
        });
    }

    private async Task ForceScrollToBottomAsync(
        CancellationToken cancellationToken = default)
    {
        if (_viewModel.Messages.Count == 0)
            return;

        _isAutoScrolling = true;

        try
        {
            /*
             * We intentionally scroll several times.
             *
             * The assistant message changes height while streaming.
             * A single ScrollTo() can happen before MAUI has finished
             * measuring the new height.
             */
            for (var i = 0; i < 5; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (_viewModel.Messages.Count == 0)
                    return;

                var lastIndex = _viewModel.Messages.Count - 1;

                MessagesCollectionView.ScrollTo(
                    lastIndex,
                    position: ScrollToPosition.End,
                    animate: false);

                await Task.Delay(50, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _isAutoScrolling = false;
        }
    }

    protected override void OnDisappearing()
    {
        _scrollCts?.Cancel();
        _scrollCts?.Dispose();
        _scrollCts = null;

        _viewModel.MessagesChanged -= OnMessagesChanged;
        MessagesCollectionView.Scrolled -= OnMessagesScrolled;

        base.OnDisappearing();
    }
}