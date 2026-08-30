using Microsoft.Maui.Controls.Shapes;
using RockAI.App.Helpers;

namespace RockAI.App.Views.Components.Chat;

/// <summary>
/// Renders Markdown text into a VerticalStackLayout of Labels / CodeBlockViews.
/// Rebuilds on every Text change so streaming incomplete Markdown stays safe.
/// </summary>
public partial class MarkdownView : ContentView
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(
            nameof(Text),
            typeof(string),
            typeof(MarkdownView),
            string.Empty,
            propertyChanged: OnTextChanged);

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public MarkdownView()
    {
        InitializeComponent();
    }

    private static void OnTextChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is MarkdownView view)
            view.Rebuild();
    }

    private void Rebuild()
    {
        Root.Children.Clear();

        var blocks = MarkdownParser.Parse(Text);
        foreach (var block in blocks)
        {
            View? child = block switch
            {
                HeadingBlock h => CreateHeading(h),
                ParagraphBlock p => CreateParagraph(p.Text),
                ListBlock l => CreateList(l),
                BlockquoteBlock q => CreateBlockquote(q.Text),
                CodeBlock c => CreateCodeBlock(c),
                HorizontalRuleBlock => CreateHr(),
                _ => null
            };

            if (child is not null)
                Root.Children.Add(child);
        }
    }

    private static View CreateHeading(HeadingBlock h)
    {
        var size = h.Level switch
        {
            1 => 22.0,
            2 => 20.0,
            3 => 18.0,
            4 => 16.0,
            _ => 15.0
        };

        return new Label
        {
            FormattedText = MarkdownParser.ParseInline(h.Text),
            FontSize = size,
            FontAttributes = FontAttributes.Bold,
            LineBreakMode = LineBreakMode.WordWrap
        };
    }

    private static View CreateParagraph(string text)
    {
        return new Label
        {
            FormattedText = MarkdownParser.ParseInline(text),
            FontSize = 14,
            LineBreakMode = LineBreakMode.WordWrap
        };
    }

    private static View CreateList(ListBlock list)
    {
        var stack = new VerticalStackLayout { Spacing = 2 };
        for (var i = 0; i < list.Items.Count; i++)
        {
            var prefix = list.Ordered ? $"{i + 1}. " : "• ";
            var row = new HorizontalStackLayout { Spacing = 4 };
            row.Children.Add(new Label
            {
                Text = prefix,
                FontSize = 14,
                VerticalOptions = LayoutOptions.Start
            });
            row.Children.Add(new Label
            {
                FormattedText = MarkdownParser.ParseInline(list.Items[i]),
                FontSize = 14,
                LineBreakMode = LineBreakMode.WordWrap,
                HorizontalOptions = LayoutOptions.FillAndExpand
            });
            stack.Children.Add(row);
        }
        return stack;
    }

    private static View CreateBlockquote(string text)
    {
        return new Border
        {
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 4 },
            BackgroundColor = Color.FromArgb("#20FFFFFF"),
            Padding = new Thickness(10, 6),
            Content = new Label
            {
                FormattedText = MarkdownParser.ParseInline(text),
                FontSize = 14,
                FontAttributes = FontAttributes.Italic,
                Opacity = 0.9,
                LineBreakMode = LineBreakMode.WordWrap
            }
        };
    }

    private static View CreateCodeBlock(CodeBlock block)
    {
        return new CodeBlockView
        {
            Language = string.IsNullOrWhiteSpace(block.Language) ? "code" : block.Language,
            Code = block.Code
        };
    }

    private static View CreateHr()
    {
        return new BoxView
        {
            HeightRequest = 1,
            Color = Color.FromArgb("#666666"),
            Margin = new Thickness(0, 6)
        };
    }
}
