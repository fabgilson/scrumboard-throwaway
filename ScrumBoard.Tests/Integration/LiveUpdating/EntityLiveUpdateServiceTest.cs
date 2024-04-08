using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using ScrumBoard.LiveUpdating;
using ScrumBoard.Models.Entities;
using ScrumBoard.Tests.Integration.Infrastructure;
using ScrumBoard.Tests.Util.LiveUpdating;
using Xunit;
using Xunit.Abstractions;

namespace ScrumBoard.Tests.Integration.LiveUpdating;

[Collection(LiveUpdateIsolationCollection.CollectionName)]
public class EntityLiveUpdateServiceTest : MultipleHubConnectionBaseIntegrationTextFixture
{
    private readonly IEntityLiveUpdateService _entityLiveUpdateService;
    
    public EntityLiveUpdateServiceTest(TestWebApplicationFactory factory, ITestOutputHelper outputHelper) : base(factory, outputHelper)
    {
        _entityLiveUpdateService = ServiceProvider.GetRequiredService<IEntityLiveUpdateService>();
    }
    
    [Fact]
    public async Task BroadcastNewValueForEntityToProjectAsync_OnlyOneUserInProject_OnlyOneUserReceivesBroadcast()
    {
        await CreateAndStartHubConnections();
        
        await _entityLiveUpdateService.BroadcastNewValueForEntityToProjectAsync(100, ProjectA.Id, new AcceptanceCriteria(), UserInProjectA.Id);
        await WaitForLiveUpdateInvocationsToNotBeEmpty(LiveUpdateEventType.EntityUpdated);
        
        InvocationsReceivedByUserInProjectA.Where(x => x.EventType is LiveUpdateEventType.EntityUpdated).Should().ContainSingle();
        InvocationsReceivedByUser1InProjectB.Where(x => x.EventType is LiveUpdateEventType.EntityUpdated).Should().BeEmpty();
    }
    
    [Fact]
    public async Task BroadcastChangeHasOccuredForEntityToProjectAsync_OnlyOneUserInProject_OnlyOneUserReceivesBroadcast()
    {
        await CreateAndStartHubConnections();
        
        await _entityLiveUpdateService.BroadcastChangeHasOccuredForEntityToProjectAsync<AcceptanceCriteria>(100, ProjectA.Id);
        await WaitForLiveUpdateInvocationsToNotBeEmpty(LiveUpdateEventType.EntityHasChanged);
        
        InvocationsReceivedByUserInProjectA.Where(x => x.EventType is LiveUpdateEventType.EntityHasChanged).Should().ContainSingle();
        InvocationsReceivedByUser1InProjectB.Where(x => x.EventType is LiveUpdateEventType.EntityHasChanged).Should().BeEmpty();
    }

    [Fact]
    public async Task BroadcastUpdateStartedOnEntityToProject_OnlyOneUserInProject_OnlyOneUserReceivesBroadcast()
    {
        await CreateAndStartHubConnections();
        
        await _entityLiveUpdateService.BroadcastUpdateStartedOnEntityToProject<AcceptanceCriteria>(100, ProjectA.Id, UserInProjectA.Id);
        await WaitForLiveUpdateInvocationsToNotBeEmpty(LiveUpdateEventType.EditingBegunOnEntity);
        
        InvocationsReceivedByUserInProjectA.Where(x => x.EventType is LiveUpdateEventType.EditingBegunOnEntity).Should().ContainSingle();
        InvocationsReceivedByUser1InProjectB.Where(x => x.EventType is LiveUpdateEventType.EditingBegunOnEntity).Should().BeEmpty();
    }

    [Fact]
    public async Task BroadcastUpdateEndedOnEntityToProject_OnlyOneUserInProject_OnlyOneUserReceivesBroadcast()
    {
        await CreateAndStartHubConnections();
        
        await _entityLiveUpdateService.BroadcastUpdateEndedOnEntityToProject<AcceptanceCriteria>(100, ProjectA.Id, UserInProjectA.Id);
        await WaitForLiveUpdateInvocationsToNotBeEmpty(LiveUpdateEventType.EditingEndedOnEntity);
        
        InvocationsReceivedByUserInProjectA.Where(x => x.EventType is LiveUpdateEventType.EditingEndedOnEntity).Should().ContainSingle();
        InvocationsReceivedByUser1InProjectB.Where(x => x.EventType is LiveUpdateEventType.EditingEndedOnEntity).Should().BeEmpty();
    }
    
    [Fact]
    public async Task BroadcastNewValueForEntityToUserAsync_OnlyOneUser_OnlyCorrectUserReceivesBroadcast()
    {
        await CreateAndStartHubConnections();
        
        await _entityLiveUpdateService.BroadcastNewValueForEntityToUserAsync(100, UserInProjectA.Id, new AcceptanceCriteria(), UserInProjectA.Id);
        await WaitForLiveUpdateInvocationsToNotBeEmpty(LiveUpdateEventType.EntityUpdated);
        
        InvocationsReceivedByUserInProjectA.Where(x => x.EventType is LiveUpdateEventType.EntityUpdated).Should().ContainSingle();
        InvocationsReceivedByUser1InProjectB.Where(x => x.EventType is LiveUpdateEventType.EntityUpdated).Should().BeEmpty();
    }

    [Fact]
    public async Task BroadcastChangeHasOccuredForEntityToUserAsync_OnlyOneUser_OnlyCorrectUserReceivesBroadcast()
    {
        await CreateAndStartHubConnections();
        
        await _entityLiveUpdateService.BroadcastChangeHasOccuredForEntityToUserAsync<AcceptanceCriteria>(100, UserInProjectA.Id);
        await WaitForLiveUpdateInvocationsToNotBeEmpty(LiveUpdateEventType.EntityHasChanged);
        
        InvocationsReceivedByUserInProjectA.Where(x => x.EventType is LiveUpdateEventType.EntityHasChanged).Should().ContainSingle();
        InvocationsReceivedByUser1InProjectB.Where(x => x.EventType is LiveUpdateEventType.EntityHasChanged).Should().BeEmpty();
    }

    [Fact]
    public async Task BroadcastUpdateStartedOnEntityToUser_OnlyOneUser_OnlyCorrectUserReceivesBroadcast()
    {
        await CreateAndStartHubConnections();
        
        await _entityLiveUpdateService.BroadcastUpdateStartedOnEntityToUser<AcceptanceCriteria>(100, UserInProjectA.Id, UserInProjectA.Id);
        await WaitForLiveUpdateInvocationsToNotBeEmpty(LiveUpdateEventType.EditingBegunOnEntity);
        
        InvocationsReceivedByUserInProjectA.Where(x => x.EventType is LiveUpdateEventType.EditingBegunOnEntity).Should().ContainSingle();
        InvocationsReceivedByUser1InProjectB.Where(x => x.EventType is LiveUpdateEventType.EditingBegunOnEntity).Should().BeEmpty();
    }

    [Fact]
    public async Task BroadcastUpdateEndedOnEntityToUser_OnlyOneUser_OnlyCorrectUserReceivesBroadcast()
    {
        await CreateAndStartHubConnections();
        
        await _entityLiveUpdateService.BroadcastUpdateEndedOnEntityToUser<AcceptanceCriteria>(100, UserInProjectA.Id, UserInProjectA.Id);
        await WaitForLiveUpdateInvocationsToNotBeEmpty(LiveUpdateEventType.EditingEndedOnEntity);
        
        InvocationsReceivedByUserInProjectA.Where(x => x.EventType is LiveUpdateEventType.EditingEndedOnEntity).Should().ContainSingle();
        InvocationsReceivedByUser1InProjectB.Where(x => x.EventType is LiveUpdateEventType.EditingEndedOnEntity).Should().BeEmpty();
    }
}