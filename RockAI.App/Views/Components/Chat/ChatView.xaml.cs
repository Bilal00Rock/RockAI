namespace RockAI.App.Views.Components.Chat;

public partial class ChatView : ContentView
{
    public ChatView()
    {
        InitializeComponent();
    }

    public void ScrollToBottomAfterLoad() => MessageListControl.ScrollToBottomAfterLoad();

    public void Cleanup() => MessageListControl.Cleanup();
}
