using System;
using System.Collections.Generic;
using Bunit;
using FluentAssertions;
using Moq;
using ScrumBoard.Models.Messages;
using ScrumBoard.Shared.Chart;
using Xunit;

namespace ScrumBoard.Tests.Blazor
{
    public class MessageListTooltipComponentTest : TestContext
    {
        private IRenderedComponent<MessageListTooltip> _component;

        private Mock<IMessage> _mockMessage1 = new();
        private Mock<IMessage> _mockMessage2 = new();

        private readonly string _firstMessage = "First mock message";
        private readonly string _secondMessage = "Second mock message";
        
        private readonly DateTime _moment = new(2012, 12, 21);

        public MessageListTooltipComponentTest()
        {
            _mockMessage1.SetupGet(mock => mock.Created).Returns(_moment);
            _mockMessage2.SetupGet(mock => mock.Created).Returns(_moment);

            _mockMessage1.Setup(mock => mock.GenerateMessage()).Returns(new List<IMessageToken>
                {new TextToken(_firstMessage)});
            _mockMessage2.Setup(mock => mock.GenerateMessage()).Returns(new List<IMessageToken>
                {new TextToken(_secondMessage)});
        }
        
        private void CreateComponent(List<IMessage> messages, int excessMessages)
        {
            _component = RenderComponent<MessageListTooltip>(parameters => parameters
                .Add(cut => cut.Messages, messages)
                .Add(cut => cut.ExcessNumber, excessMessages)
            );
        }
        
        [Fact]
        public void ComponentRendered_SinglMessage_MessageShown() {
            CreateComponent(new List<IMessage> { 
                _mockMessage1.Object,
            }, 0);

            var messages =_component.FindAll(".message");
            messages.Should().HaveCount(1);

            var textContent = _component.Find("div").TextContent;
            textContent.Should().Contain(_firstMessage);
        }

        [Fact]
        public void ComponentRendered_MultipleMessages_MultipleMessagesShown() {
            CreateComponent(new List<IMessage> { 
                _mockMessage1.Object,
                _mockMessage2.Object,
            }, 0);

            var messages =_component.FindAll(".message");
            messages.Should().HaveCount(2);

            var textContent = _component.Find("div").TextContent;
            textContent.Should().Contain(_firstMessage);
            textContent.Should().Contain(_secondMessage);
        }

        [Fact]
        public void ComponentRendered_WithExcessMessages_NumberOfExcessMessagesShown() {
            CreateComponent( new List<IMessage>(), 10);

            var messages = _component.FindAll(".message");
            messages.Should().HaveCount(0);
            
            var textContent = _component.Find("div").TextContent;
            textContent.Should().Contain("+ 10");
        }
    }
}
