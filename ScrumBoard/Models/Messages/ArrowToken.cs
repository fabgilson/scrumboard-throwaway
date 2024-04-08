using System;

namespace ScrumBoard.Models.Messages
{
    public class ArrowToken : IMessageToken
    {
        public Type Component => typeof(Shared.Widgets.Messages.ArrowToken);
        
        public override string ToString()
        {
            return "->";
        }
    }
}