namespace RockAI.App.Views.Components.Chat;

public partial class CodeBlockView : ContentView
{
    public static readonly BindableProperty LanguageProperty =
        BindableProperty.Create(nameof(Language), typeof(string), typeof(CodeBlockView), string.Empty);

    public static readonly BindableProperty CodeProperty =
        BindableProperty.Create(nameof(Code), typeof(string), typeof(CodeBlockView), string.Empty);

    public string Language
    {
        get => (string)GetValue(LanguageProperty);
        set => SetValue(LanguageProperty, value);
    }

    public string Code
    {
        get => (string)GetValue(CodeProperty);
        set => SetValue(CodeProperty, value);
    }

    public CodeBlockView()
    {
        InitializeComponent();
        BindingContext = this;
    }

    private async void OnCopyClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(Code))
            return;

        try
        {
            await Clipboard.Default.SetTextAsync(Code);
        }
        catch
        {
            // Clipboard may fail on some platforms/contexts; ignore.
        }
    }
}
