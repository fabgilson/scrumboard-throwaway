using System;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharpWrappers;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.Models.Entities;
using ScrumBoard.Services;
using ScrumBoard.Shared.BlackBoxReview;
using ScrumBoard.Tests.Util;
using ScrumBoard.Tests.Util.LiveUpdating;
using Xunit;

namespace ScrumBoard.Tests.Blazor.BlackBoxReview;

public class AcceptanceCriteriaInReviewComponentTest : BaseProjectScopedComponentTestContext<AcceptanceCriteriaInReview>
{
    private AcceptanceCriteria _acceptanceCriteria;

    private IElement PassButton => ComponentUnderTest.Find($"#btn-pass-{_acceptanceCriteria.Id}").Unwrap();
    private IElement FailButton => ComponentUnderTest.Find($"#btn-fail-{_acceptanceCriteria.Id}").Unwrap();
    private IElement ReviewCommentsTextArea => ComponentUnderTest.Find($"#comments-{_acceptanceCriteria.Id}");
    
    private readonly Mock<IAcceptanceCriteriaService> _acceptanceCriteriaServiceMock = new();
    
    private void CreateComponent(
        AcceptanceCriteriaStatus? initialStatus = null, 
        string initialReviewComments = ""
    ) {
        _acceptanceCriteria = FakeDataGenerator.CreateAcceptanceCriteria(
            status: initialStatus, 
            reviewComments: initialReviewComments
        );

        Services.AddScoped(_ => _acceptanceCriteriaServiceMock.Object);
        
        CreateComponentUnderTest(extendParameterBuilder: parameters => parameters
            .Add(x => x.AcceptanceCriteria, _acceptanceCriteria)
        );
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ClickStatusButton_NoStatusAlreadySelected_ServiceMethodCalled(bool isPassButton)
    {
        CreateComponent(initialReviewComments: "Reviewed");
        (isPassButton ? PassButton : FailButton).Click();
        _acceptanceCriteriaServiceMock.Verify(x => 
            x.SetReviewFieldsByIdAsync(
                _acceptanceCriteria.Id, 
                ActingUser.Id, 
                isPassButton ? AcceptanceCriteriaStatus.Pass : AcceptanceCriteriaStatus.Fail, 
                "Reviewed",
                It.IsAny<Guid>()),
            Times.Once
        );
    }
    
    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void ChangeStatus_StatusAlreadySelected_ServiceMethodCalled(bool isPassButton, bool existingStatusIsPass)
    {
        CreateComponent(
            initialStatus: existingStatusIsPass ? AcceptanceCriteriaStatus.Pass : AcceptanceCriteriaStatus.Fail, 
            initialReviewComments: "Reviewed"
        );
        (isPassButton ? PassButton : FailButton).Click();
        _acceptanceCriteriaServiceMock.Verify(x => 
                x.SetReviewFieldsByIdAsync(
                    _acceptanceCriteria.Id, 
                    ActingUser.Id, 
                    isPassButton ? AcceptanceCriteriaStatus.Pass : AcceptanceCriteriaStatus.Fail, 
                    "Reviewed",
                    It.IsAny<Guid>()),
            Times.Once
        );
    }
    
    [Fact]
    public void ClickFailButton_ReviewCommentsEmpty_ServiceMethodNotCalled()
    {
        CreateComponent();
        FailButton.Click();
        _acceptanceCriteriaServiceMock.Verify(x => 
                x.SetReviewFieldsByIdAsync(
                    It.IsAny<long>(), 
                    It.IsAny<long>(), 
                    It.IsAny<AcceptanceCriteriaStatus>(), 
                    It.IsAny<string>(), 
                    It.IsAny<Guid>()
                ),
            Times.Never
        );
    }

    [Fact]
    public void Rendered_ParametersSet_LiveUpdateHandlerRegisteredOnce()
    {
        CreateComponent();
        var handlerRegistrations = GetLiveUpdateEventsForEntity<AcceptanceCriteria>(_acceptanceCriteria.Id, LiveUpdateEventType.EntityUpdated);
        handlerRegistrations.Should().ContainSingle();
    }
    
    [Theory]
    [InlineData(null, null)]
    [InlineData(null, "It is still a fail")]
    [InlineData(AcceptanceCriteriaStatus.Pass, null)]
    [InlineData(AcceptanceCriteriaStatus.Pass, "It passed now!")]
    [InlineData(AcceptanceCriteriaStatus.Fail, null)]
    [InlineData(AcceptanceCriteriaStatus.Fail, "It is still a fail")]
    public async Task LiveUpdateHandlerTriggered_NewValue_ComponentUpdatesCorrectly(AcceptanceCriteriaStatus? newStatus, string newReviewComments)
    {
        CreateComponent(initialStatus: AcceptanceCriteriaStatus.Fail, initialReviewComments: "Failed");
        var liveUpdateHandler = GetMostRecentLiveUpdateHandlerForEntity<AcceptanceCriteria>(_acceptanceCriteria.Id, LiveUpdateEventType.EntityUpdated);

        FailButton.IsChecked().Should().BeTrue();
        PassButton.IsChecked().Should().BeFalse();
        ReviewCommentsTextArea.GetAttribute("value").Should().Be("Failed");
        
        _acceptanceCriteria.Status = newStatus ?? _acceptanceCriteria.Status;
        _acceptanceCriteria.ReviewComments = newReviewComments ?? _acceptanceCriteria.ReviewComments;
        await ComponentUnderTest.InvokeAsync(() => liveUpdateHandler.GetTypedEntityUpdateHandler<AcceptanceCriteria>().Invoke(_acceptanceCriteria, ActingUser.Id));
        
        FailButton.IsChecked().Should().Be(newStatus is AcceptanceCriteriaStatus.Fail or null);
        PassButton.IsChecked().Should().Be(newStatus is AcceptanceCriteriaStatus.Pass);
        ReviewCommentsTextArea.GetAttribute("value").Should().Be(newReviewComments ?? "Failed");
    }
}