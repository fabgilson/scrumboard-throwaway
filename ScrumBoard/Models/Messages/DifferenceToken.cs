using System;

namespace ScrumBoard.Models.Messages
{
    public class DifferenceToken : IMessageToken
    {
        public Type Component => typeof(Shared.Widgets.Messages.DifferenceToken);
        
        public object From { get; }
        public object To { get; }

        public DifferenceToken(object from, object to)
        {
            From = from;
            To = to;
        }
        
        public override string ToString()
        {
            return $"{From} -> {To}";
        }
    }
}