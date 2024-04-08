using System;
using System.Collections.Generic;
using ScrumBoard.Models.Entities;

namespace ScrumBoard.Models.Messages
{
    public class CreatedMessage : IMessage
    {
        private DateTime _created;

        private User _creator;

        public string _objectName;

        public CreatedMessage(DateTime created, User creator, string objectName)
        {
            if (creator == null)
                throw new ArgumentNullException(nameof(creator));
            if (objectName == null)
                throw new ArgumentNullException(nameof(objectName));
            _created = created;
            _creator = creator;
            _objectName = objectName;
        }

        public DateTime Created => _created;

        public List<IMessageToken> GenerateMessage()
        {
            return new List<IMessageToken>()
            {
                new TextToken($"{_creator.FirstName} {_creator.LastName} created {_objectName}"),
            };
        }
    }
}