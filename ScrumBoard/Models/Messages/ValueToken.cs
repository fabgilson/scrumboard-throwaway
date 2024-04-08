using System;
using ScrumBoard.Extensions;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Relationships;
using ScrumBoard.Utils;

namespace ScrumBoard.Models.Messages
{
    public class ValueToken : IMessageToken
    {
        public Type Component => typeof(Shared.Widgets.Messages.ValueToken);
        
        public readonly object Value;
        public string Content { get; private set; }

        public TokenSize TokenSize { get; }
        
        public ValueToken(object value, TokenSize tokenSize)
        {
            TokenSize = tokenSize;
            Value = value;
            SetContent(value);
        }
        
        public ValueToken(object value)
        {
            Value = value;
            SetContent(value);
        }

        private void SetContent(object value)
        {
            Content = value switch
            {
                User userVal => userVal.GetFullName(),
                string stringVal => $"'{stringVal}'",
                TimeSpan durationVal => DurationUtils.DurationStringFrom(durationVal),
                int numVal => numVal.ToString(),
                double numVal => numVal.ToString(),
                long numVal => numVal.ToString(),
                DateOnly dateVal => dateVal.ToString(),
                DateTime dateVal => dateVal.ToString(),
                Enum enumVal => enumVal.ToString(),
                ITag tagVal => tagVal.Name,
                UserStory storyVal => $"'{storyVal.Name}'",
                UserStoryTask taskVal => taskVal.Name,
                TaggedWorkInstance workInstance => $"{workInstance.WorklogTag.Name} ({DurationUtils.DurationStringFrom(workInstance.Duration)})",
                null => "Empty",
                _ => $"'{value}'"
            };
        }

        public override string ToString()
        {
            return Content;
        }
    }
}