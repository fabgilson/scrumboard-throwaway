using System;
using System.Diagnostics;

namespace ScrumBoard.Models.Messages
{
    public class TextToken : IMessageToken
    {
        public Type Component => typeof(Shared.Widgets.Messages.TextToken);
        
        public string Content { get; private set; }
        
        public FontStyle FontStyle { get; private set; }

        public TextToken(string content, FontStyle fontStyle = FontStyle.Normal)
        {
            Content = content;
            FontStyle = fontStyle;
        }

        public override string ToString()
        {
            return Content;
        }
    }

    public enum FontStyle
    {
        Normal,
        Bold,
    }

    public static class FontStyleExtensions
    {
        public static string GetCss(this FontStyle fontStyle)
        {
            return fontStyle switch
            {
                FontStyle.Normal => "",
                FontStyle.Bold => "fw-bold",
                _ => throw new ArgumentException($"Unknown {nameof(fontStyle)} value {fontStyle}", nameof(fontStyle)),
            };
        }
    }
}