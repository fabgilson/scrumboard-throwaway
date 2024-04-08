using System;
using System.Collections.Generic;
using ScrumBoard.Extensions;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Changelog;

namespace ScrumBoard.Models.Messages
{
    public class BurndownPointMessage : IMessage
    {
        private List<IMessageToken> _tokens = new();

        public BurndownPointMessage(DateTime created, WorklogEntry entry)
        {
            Created = created;
            _tokens.Add(new TextToken($"{entry.User.GetFullName()}"));
            if (entry.PairUser != null)
            {
                _tokens.Add(new TextToken($"and {entry.PairUser.GetFullName()}"));
            }
            _tokens.Add(new TextToken("logged"));
            _tokens.Add(new ValueToken(entry.GetTotalTimeSpent()));
            _tokens.Add(new TextToken("on"));
            _tokens.Add(new ValueToken(entry.Task));
        }

        public BurndownPointMessage(DateTime created, UserStoryTaskChangelogEntry change)
        {
            Created = created;
            if (change.FieldChanged == nameof(UserStoryTask.Estimate))
            {
                _tokens.Add(new TextToken("Estimate of"));
            } else if (change.FieldChanged == nameof(UserStoryTask.Stage))
            {
                _tokens.Add(new TextToken("Stage of"));
            }
            else
            {
                throw new InvalidOperationException($"Unexpected FieldChanged '{change.FieldChanged}'");
            }
            _tokens.Add(new ValueToken(change.UserStoryTaskChanged));
            _tokens.Add(new TextToken("changed"));
            _tokens.Add(new ValueToken(change.FromValueObject));
            _tokens.Add(new ArrowToken());
            _tokens.Add(new ValueToken(change.ToValueObject));
        }

        public BurndownPointMessage(DateTime created, UserStoryTask task)
        {
            Created = created;
            _tokens.Add(new TextToken("Added task"));
            _tokens.Add(new ValueToken(task));
            _tokens.Add(new TextToken("with estimate"));
            _tokens.Add(new ValueToken(task.OriginalEstimate));
        }

        public DateTime Created { get; private set; }

        public List<IMessageToken> GenerateMessage()
        {
            return _tokens;
        }
    }
}