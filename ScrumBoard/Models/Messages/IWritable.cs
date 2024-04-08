using System.Collections.Generic;

namespace ScrumBoard.Models.Messages
{
    public interface IWritable
    {
        List<IMessageToken> GenerateMessage();
    }
}