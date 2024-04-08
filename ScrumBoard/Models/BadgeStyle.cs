using System;

namespace ScrumBoard.Models
{
    public enum BadgeStyle
    {
        Disabled,
        Light,
        Primary,
        Success,
        Dark,
        Warning,
        Danger,
        Info,
        
        // Some styles of a neutral palette to not infer any meaning 
        NeutralOne,
        NeutralTwo,
        NeutralThree,
        NeutralFour,
        NeutralFive,
    }

    public static class BadgeStyleExtensions
    {
        public static string GetCss(this BadgeStyle style)
        {
            return style switch
            {
                BadgeStyle.Disabled => "bg-light fst-italic text-dark",
                BadgeStyle.Light   => "text-dark bg-light",
                BadgeStyle.Primary => "bg-primary",
                BadgeStyle.Success => "bg-success",
                BadgeStyle.Dark    => "text-light bg-secondary",
                BadgeStyle.Warning => "text-dark bg-warning",
                BadgeStyle.Danger  => "bg-danger",
                BadgeStyle.Info  => "bg-info",
                
                BadgeStyle.NeutralOne => "neutral-one",
                BadgeStyle.NeutralTwo => "neutral-two",
                BadgeStyle.NeutralThree    => "neutral-three",
                BadgeStyle.NeutralFour => "neutral-four",
                BadgeStyle.NeutralFive  => "neutral-five",
                
                _ => throw new ArgumentException($"Unknown enum value: {style}", nameof(style)),
            };
        }
    }
}
