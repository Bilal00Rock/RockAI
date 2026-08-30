using RockAI.App.ViewModels;

namespace RockAI.App.Views.Components.Chat;

public partial class MessageActions : ContentView
{
    public static readonly BindableProperty ShowCopyProperty =
        BindableProperty.Create(nameof(ShowCopy), typeof(bool), typeof(MessageActions), true);

    public bool ShowCopy
    {
        get => (bool)GetValue(ShowCopyProperty);
        set => SetValue(ShowCopyProperty, value);
    }

    public MessageActions()
    {
        InitializeComponent();
    }

    private async void OnCopyClicked(object? sender, EventArgs e)
    {
        if (BindingContext is not MessageViewModel msg)
            return;

        if (string.IsNullOrEmpty(msg.Content))
            return;

        try
        {
            await Clipboard.Default.SetTextAsync(msg.Content);
        }
        catch
        {
            // ignore
        }
    }
}
