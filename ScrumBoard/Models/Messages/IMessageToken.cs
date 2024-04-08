using System;

namespace ScrumBoard.Models.Messages
{
    public interface IMessageToken
    {
        Type Component { get;  }
    }
}