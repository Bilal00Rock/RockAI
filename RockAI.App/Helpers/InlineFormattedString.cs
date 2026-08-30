using System.Collections.Generic;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace RockAI.App.Helpers
{
    public class InlineSpan
    {
        public string Text { get; set; } = string.Empty;
        public FontAttributes FontAttributes { get; set; } = FontAttributes.None;
        public string? FontFamily { get; set; }
        public Color? TextColor { get; set; }
        public Color? BackgroundColor { get; set; }
        public TextDecorations? TextDecorations { get; set; }
    }

    public class InlineFormattedString
    {
        public List<InlineSpan> Spans { get; } = new List<InlineSpan>();

        /// <summary>
        /// Convert to MAUI FormattedString. This will create MAUI UI types and should only be
        /// called in a UI/runtime context where MAUI is available.
        /// </summary>
        public FormattedString ToFormattedString()
        {
            var fs = new FormattedString();
            foreach (var s in Spans)
            {
                var span = new Span { Text = s.Text, FontAttributes = s.FontAttributes };
                if (s.FontFamily is not null)
                    span.FontFamily = s.FontFamily;
                if (s.TextColor is not null)
                    span.TextColor = s.TextColor;
                if (s.BackgroundColor is not null)
                    span.BackgroundColor = s.BackgroundColor;
                if (s.TextDecorations is not null)
                    span.TextDecorations = s.TextDecorations.Value;
                fs.Spans.Add(span);
            }
            return fs;
        }
    }
}
