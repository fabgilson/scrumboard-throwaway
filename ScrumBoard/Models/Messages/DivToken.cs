using System;
using System.Collections.Generic;

namespace ScrumBoard.Models.Messages
{
    public class DivToken : IMessageToken
    {
        public Type Component => typeof(Shared.Widgets.Messages.DivToken);
        
        public List<IMessageToken> Tokens { get; }

        public DivToken(List<IMessageToken> tokens)
        {
            Tokens = tokens;
        }
    }
}