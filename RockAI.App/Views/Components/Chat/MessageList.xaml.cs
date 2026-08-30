using System.Collections.Specialized;
using RockAI.App.ViewModels;

namespace RockAI.App.Views.Components.Chat;

/// <summary>
/// Owns the message CollectionView and all presentation-level scrolling state.
/// Auto-scrolls only when the user is near the bottom (or on explicit load/send).
/// Does not force-scroll when the user has scrolled up to read older messages.
/// </summary>
public partial class MessageList : ContentView
{
    private MainViewModel? _viewModel;
    private CancellationTokenSource? _scrollCts;
    private bool _userNearBottom = true;
    private bool _isProgrammaticScroll;
    private int _lastKnownCount;

    // Consider "near bottom" when the last visible index is within this many items of the end.
    private const int NearBottomThreshold = 2;

    public MessageList()
    {
        InitializeComponent();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (_viewModel is not null)
        {
            _viewModel.MessagesChanged -= OnMessagesChanged;
            _viewModel.Messages.CollectionChanged -= OnCollectionChanged;
        }

        _viewModel = BindingContext as MainViewModel;

        if (_viewModel is not null)
        {
            _viewModel.MessagesChanged += OnMessagesChanged;
            _viewModel.Messages.CollectionChanged += OnCollectionChanged;
            _lastKnownCount = _viewModel.Messages.Count;
        }
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // New messages added (send / load) → prefer following bottom.
        if (e.Action == NotifyCollectionChangedAction.Add ||
            e.Action == NotifyCollectionChangedAction.Reset)
        {
            _userNearBottom = true;
        }
    }

    private void OnMessagesChanged()
    {
        if (_viewModel is null)
            return;

        var count = _viewModel.Messages.Count;
        var countIncreased = count > _lastKnownCount;
        _lastKnownCount = count;

        // Only auto-scroll when the user is already near the bottom
        // (or we just added items and haven't detected a user scroll-away yet).
        if (!_userNearBottom)
        {
            UpdateScrollButtonVisibility();
            return;
        }

        ScheduleScrollToBottom();
    }

    private void OnMessagesScrolled(object? sender, ItemsViewScrolledEventArgs e)
    {
        if (_isProgrammaticScroll || _viewModel is null)
            return;

        var lastIndex = _viewModel.Messages.Count - 1;
        if (lastIndex < 0)
        {
            _userNearBottom = true;
            UpdateScrollButtonVisibility();
            return;
        }

        // User is near bottom if the last visible item is close to the end.
        _userNearBottom = e.LastVisibleItemIndex >= lastIndex - NearBottomThreshold;
        UpdateScrollButtonVisibility();
    }

    private void UpdateScrollButtonVisibility()
    {
        ScrollToBottomButton.IsVisible = !_userNearBottom && (_viewModel?.Messages.Count ?? 0) > 0;
    }

    private void OnScrollToBottomClicked(object? sender, EventArgs e)
    {
        _userNearBottom = true;
        UpdateScrollButtonVisibility();
        ScheduleScrollToBottom(force: true);
    }

    /// <summary>
    /// Called by parent when a conversation is loaded / switched so we land at the true bottom.
    /// </summary>
    public void ScrollToBottomAfterLoad()
    {
        _userNearBottom = true;
        UpdateScrollButtonVisibility();
        ScheduleScrollToBottom(force: true);
    }

    private void ScheduleScrollToBottom(bool force = false)
    {
        if (!force && !_userNearBottom)
            return;

        _scrollCts?.Cancel();
        _scrollCts?.Dispose();
        _scrollCts = new CancellationTokenSource();
        var token = _scrollCts.Token;

        // Dispatch after the current layout pass so measurement has a chance to complete.
        Dispatcher.Dispatch(async () =>
        {
            try
            {
                // One short yield for CollectionView to process the collection change.
                // Not a magic multi-second delay — a single frame-scale wait.
                await Task.Delay(16, token);
                if (token.IsCancellationRequested)
                    return;

                await ScrollToBottomAsync(token);
            }
            catch (OperationCanceledException)
            {
            }
        });
    }

    private async Task ScrollToBottomAsync(CancellationToken cancellationToken)
    {
        if (_viewModel is null || _viewModel.Messages.Count == 0)
            return;

        _isProgrammaticScroll = true;
        try
        {
            var lastIndex = _viewModel.Messages.Count - 1;

            // Two attempts: first immediately, second after a layout tick for streaming height growth.
            MessagesCollectionView.ScrollTo(lastIndex, position: ScrollToPosition.End, animate: false);
            await Task.Delay(32, cancellationToken);
            if (cancellationToken.IsCancellationRequested || _viewModel.Messages.Count == 0)
                return;

            lastIndex = _viewModel.Messages.Count - 1;
            MessagesCollectionView.ScrollTo(lastIndex, position: ScrollToPosition.End, animate: false);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _isProgrammaticScroll = false;
        }
    }

    public void Cleanup()
    {
        _scrollCts?.Cancel();
        _scrollCts?.Dispose();
        _scrollCts = null;

        if (_viewModel is not null)
        {
            _viewModel.MessagesChanged -= OnMessagesChanged;
            _viewModel.Messages.CollectionChanged -= OnCollectionChanged;
        }
    }
}
