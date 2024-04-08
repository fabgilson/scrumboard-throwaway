using System;

namespace ScrumBoard.Models.Messages
{
    public interface IMessage : IWritable
    {
        DateTime Created { get; }
    }
}