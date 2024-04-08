using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Diffing.Extensions;
using Bunit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.LiveUpdating;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.FeatureFlags;
using ScrumBoard.Repositories;
using ScrumBoard.Services;
using ScrumBoard.Services.UsageData;
using ScrumBoard.Shared;
using ScrumBoard.Tests.Util;
using ScrumBoard.Tests.Util.LiveUpdating;
using ScrumBoard.Utils;
using FakeNavigationManager = Bunit.TestDoubles.FakeNavigationManager;

namespace ScrumBoard.Tests.Blazor;

public abstract class BaseProjectScopedComponentTestContext<TComponentUnderTest> : TestContext where TComponentUnderTest : BaseProjectScopedComponent
{
    protected Mock<IJsInteropService> JsInteropServiceMock { get; }
    protected Mock<IProjectRepository> ProjectRepositoryMock { get; }
    protected Mock<IUserRepository> UserRepositoryMock { get; }
    protected Mock<IClock> ClockMock { get; }
    protected Mock<IUsageDataService> UsageDataServiceMock { get; }
    protected Mock<IProjectFeatureFlagService> ProjectFeatureFlagServiceMock { get; }
    
    protected Mock<IHubContext<EntityUpdateHub>> EntityUpdateHubMock { get; }
    private Mock<IEntityLiveUpdateConnectionBuilder> LiveUpdateConnectionBuilderMock { get; }
    protected Mock<EntityUpdateHubConnectionWrapper> LiveUpdateConnectionWrapperMock { get; }

    private readonly List<LiveUpdateHandlerRegistration> _registeredLiveUpdateEventHandlers = [];

    protected User ActingUser { get; private set; }
    protected Project CurrentProject { get; private set; }
    protected ProjectRole CurrentProjectRole { get; private set; }
    protected ProjectState ProjectState { get; private set; }
    protected FakeNavigationManager FakeNavigationManager => Services.GetRequiredService<FakeNavigationManager>();

    protected IRenderedComponent<TComponentUnderTest> ComponentUnderTest { get; private set; }

    protected BaseProjectScopedComponentTestContext()
    {
        JsInteropServiceMock = new Mock<IJsInteropService>();
        ProjectRepositoryMock = new Mock<IProjectRepository>();
        UserRepositoryMock = new Mock<IUserRepository>();
        ClockMock = new Mock<IClock>();
        UsageDataServiceMock = new Mock<IUsageDataService>();
        ProjectFeatureFlagServiceMock = new Mock<IProjectFeatureFlagService>();

        LiveUpdateConnectionBuilderMock = new Mock<IEntityLiveUpdateConnectionBuilder>();
        LiveUpdateConnectionWrapperMock = new Mock<EntityUpdateHubConnectionWrapper>();
        ConfigureEntityHubConnectionWrapperMock();

        EntityUpdateHubMock = new Mock<IHubContext<EntityUpdateHub>>();
        var clientsMock = new Mock<IHubClients>();
        var clientsProxyMock = new Mock<IClientProxy>();
        clientsMock.Setup(x => x.Group(It.IsAny<string>())).Returns(clientsProxyMock.Object);
        EntityUpdateHubMock.Setup(x => x.Clients).Returns(() => clientsMock.Object);
        
        Services.AddScoped(_ => JsInteropServiceMock.Object);
        Services.AddScoped(_ => ProjectRepositoryMock.Object);
        Services.AddScoped(_ => UserRepositoryMock.Object);
        Services.AddScoped(_ => ClockMock.Object);
        Services.AddScoped(_ => UsageDataServiceMock.Object);
        Services.AddScoped(_ => ProjectFeatureFlagServiceMock.Object);

        Services.AddScoped<IEntityLiveUpdateService>(_ => new EntityLiveUpdateService(EntityUpdateHubMock.Object));
        Services.AddScoped(_ => LiveUpdateConnectionBuilderMock.Object);
        
        ActingUser = FakeDataGenerator.CreateFakeUser();
        CurrentProject = FakeDataGenerator.CreateFakeProject();
    }

    /// <summary>
    /// Configures the EntityHubConnectionWrapper mock to capture all live update handlers registered from the component.
    /// The <see cref="BaseProjectScopedComponent"/> base methods for registering live update handlers are configured to
    /// pass the type of the calling component to this wrapper method as an argument so that we can capture it here,
    /// even though its value is never used in the actual code.
    /// </summary>
    private void ConfigureEntityHubConnectionWrapperMock()
    {
        // Entity update handlers
        LiveUpdateConnectionWrapperMock
            .Setup(x => x.OnUpdateReceivedForEntityWithId(It.IsAny<long>(), It.IsAny<Func<IdTypeMatcher<IId>, long, Task>>(), It.IsAny<Type>()))
            .Callback(new InvocationAction(invocation => _registeredLiveUpdateEventHandlers.Add(
                LiveUpdateHandlerRegistration.FromInvocation(invocation, LiveUpdateEventType.EntityUpdated))
            ));

        // Editing started on entity handlers
        LiveUpdateConnectionWrapperMock
            .Setup(x => x.OnUpdateBegunForEntityByUser<IdTypeMatcher<IId>>(It.IsAny<long>(), It.IsAny<Func<long, Task>>(), It.IsAny<Type>()))
            .Callback(new InvocationAction(invocation => _registeredLiveUpdateEventHandlers.Add(
                LiveUpdateHandlerRegistration.FromInvocation(invocation, LiveUpdateEventType.EditingBegunOnEntity))
            ));

        // Editing ended on entity handlers
        LiveUpdateConnectionWrapperMock
            .Setup(x => x.OnUpdateEndedForEntityByUser<IdTypeMatcher<IId>>(It.IsAny<long>(), It.IsAny<Func<long, Task>>(), It.IsAny<Type>()))
            .Callback(new InvocationAction(invocation => _registeredLiveUpdateEventHandlers.Add(
                    LiveUpdateHandlerRegistration.FromInvocation(invocation, LiveUpdateEventType.EditingEndedOnEntity))
                ));
        
        // Entity has changed handlers
        LiveUpdateConnectionWrapperMock
            .Setup(x => x.OnEntityWithIdHasChanged<IdTypeMatcher<IId>>(It.IsAny<long>(), It.IsAny<Func<Task>>(), It.IsAny<Type>()))
            .Callback(new InvocationAction(invocation => _registeredLiveUpdateEventHandlers.Add(
                LiveUpdateHandlerRegistration.FromInvocation(invocation, LiveUpdateEventType.EntityHasChanged))
            ));
    }
    
    /// <summary>
    /// Get all live updated handlers that have been registered since the components creation.
    /// </summary>
    /// <param name="callingClassType">The type of class which performed the registration, defaults to <see cref="TComponentUnderTest"/>.</param>
    /// <returns>All registered live update handlers</returns>
    protected IReadOnlyCollection<LiveUpdateHandlerRegistration> GetAllRegisteredLiveUpdateEventHandlers(
        Type callingClassType = null
    ) {
        return _registeredLiveUpdateEventHandlers
            .Where(x => x.CallingClassType == (callingClassType ?? typeof(TComponentUnderTest)))
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Retrieves the most recent live update handler registration for a specific entity that has occurred since this component was created.
    /// By default, this method returns the most recent registration performed by a component of the same type as the one currently
    /// under test.
    ///
    /// E.g., if the component under test is of type A, and handlers are registered by both the component under test and its children
    /// of type B, by default only the most recent handler registered by a component of type A is returned. This behavior can be overridden
    /// by passing a value to the <see cref="callingClassType"/> parameter.
    /// </summary>
    /// <param name="entityId">ID of the entity for which a live update handler has been registered.</param>
    /// <param name="registrationType">The type of registered event to fetch, if null will return all registered handlers.</param>
    /// <param name="callingClassType">The type of class which performed the registration, defaults to <see cref="TComponentUnderTest"/>.</param>
    /// <typeparam name="TEntity">The type of entity for which a live update handler has been registered.</typeparam>
    /// <returns>
    /// The most recent live update handler registration for the specified entity matching the given parameters. If no matching handler
    /// registration is found, returns null.
    /// </returns>
    protected LiveUpdateHandlerRegistration GetMostRecentLiveUpdateHandlerForEntity<TEntity>(
        long entityId,
        LiveUpdateEventType? registrationType,
        Type callingClassType = null
    ) where TEntity : IId
    {
        return GetLiveUpdateEventsForEntity<TEntity>(entityId, registrationType, callingClassType).LastOrDefault();
    }

    /// <summary>
    /// Retrieves all live update handlers registrations that have occurred since this component was created.
    /// By default, this method only returns registrations performed by a component of the same type as the one currently
    /// under test.
    /// 
    /// E.g. if my component under test has some type A, and has children with some type B, and handlers are
    /// registered by both the component under test and its children, by default only the handlers registered by components
    /// with type A are returned. This behaviour can be overriden by passing a value to the <see cref="callingClassType"/>
    /// parameter. 
    /// </summary>
    /// <param name="entityId">ID of entity for which a live update handler has been registered.</param>
    /// <param name="registrationType">The type of registered events to fetch, if null will return all registered handlers.</param>
    /// <param name="callingClassType">The type of class which performed the registration, defaults to <see cref="TComponentUnderTest"/>.</param>
    /// <typeparam name="TEntity">The type of entity for which a live update handler has been registered.</typeparam>
    /// <returns>
    /// All live update handler registrations matching the given parameters that have occurred since this component was created.
    /// Handlers are ordered from oldest (first) to most recent (last).
    /// </returns>
    protected IEnumerable<LiveUpdateHandlerRegistration> GetLiveUpdateEventsForEntity<TEntity>(
        long entityId,
        LiveUpdateEventType? registrationType,
        Type callingClassType = null
    ) where TEntity : IId
    {
        return _registeredLiveUpdateEventHandlers
            .Where(x => x.EntityType == typeof(TEntity) && x.EntityId == entityId)
            .Where(x => x.CallingClassType == (callingClassType ?? typeof(TComponentUnderTest)))
            .Where(x => registrationType == null || x.EventType == registrationType)
            .ToList();
    }

    /// <summary>
    /// Creates an instance of the component under test. Default values will be supplied to meet the minimum requirements
    /// for a <see cref="BaseProjectScopedComponent"/>, though these values can be overriden by supplying non-null values
    /// to this method's parameters.
    /// For non-default parameters to pass to the component, pass in an action for configuring these through the
    /// <see cref="extendParameterBuilder"/> parameter. Do not use this builder to override the default values, instead
    /// provide the default values as parameters here instead.
    /// </summary>
    /// <param name="actingUser">
    /// Override for value of current acting user. Will automatically be added as a member of <see cref="CurrentProject"/>
    /// with role <see cref="actingUserRoleInProject"/>.
    /// </param>
    /// <param name="currentProject">
    /// Override for value of current project in scope. This project must have a non-default ID provided, and will be returned
    /// by the mocked project repository when requests are made with its ID - such as during base authentication and state
    /// checks in <see cref="BaseProjectScopedComponent.RefreshProjectAndEnforcePermissionsAsync"/>
    /// </param>
    /// <param name="isReadOnly">Whether the acting user should be in read only mode for the current project</param>
    /// <param name="actingUserRoleInProject">The current user's role in the current project in scope.</param>
    /// <param name="extendParameterBuilder">
    /// Builder for passing in additional parameters to component not already covered by default parameters.
    /// </param>
    /// <param name="otherDevelopersOnTeam">Fake developer users to add to team</param>
    /// <param name="featureFlagsEnabledOnProject">Feature flags that should be treated as set for the current project.</param>
    protected void CreateComponentUnderTest(
        User actingUser = null,
        Project currentProject = null,
        bool? isReadOnly = null,
        ProjectRole actingUserRoleInProject = ProjectRole.Developer,
        Action<ComponentParameterCollectionBuilder<TComponentUnderTest>> extendParameterBuilder = null,
        IEnumerable<User> otherDevelopersOnTeam = null,
        ICollection<FeatureFlagDefinition> featureFlagsEnabledOnProject=null
    ) {
        JSInterop.Mode = JSRuntimeMode.Loose;

        ActingUser = actingUser ?? ActingUser;
        CurrentProject = currentProject ?? CurrentProject;
        CurrentProjectRole = actingUserRoleInProject;

        ProjectState = new ProjectState
        {
            ProjectId = CurrentProject.Id,
            ProjectRole = CurrentProjectRole,
            Project = CurrentProject,
            IsReadOnly = isReadOnly ?? false
        };

        CurrentProject.MemberAssociations.Add(new ProjectUserMembership
        {
            Project = CurrentProject,
            ProjectId = CurrentProject.Id,
            Role = CurrentProjectRole,
            User = ActingUser,
            UserId = ActingUser.Id
        });

        otherDevelopersOnTeam ??= new List<User>();
        CurrentProject.MemberAssociations.AddRange(otherDevelopersOnTeam.Select(x => new ProjectUserMembership
            {
                Project = CurrentProject,
                ProjectId = CurrentProject.Id,
                Role = ProjectRole.Developer,
                User = x,
                UserId = x.Id
            })
        );

        ProjectRepositoryMock
            .Setup(x => x.GetByIdAsync(CurrentProject.Id, It.IsAny<Func<IQueryable<Project>, IQueryable<Project>>[]>()))
            .ReturnsAsync(CurrentProject);

        featureFlagsEnabledOnProject ??= [];
        ProjectFeatureFlagServiceMock.Setup(x => x.ProjectHasFeatureFlagAsync(
            It.Is<Project>(p => p.Id == CurrentProject.Id),
            It.IsAny<FeatureFlagDefinition>()
        )).ReturnsAsync((Project _, FeatureFlagDefinition flag) => featureFlagsEnabledOnProject.Contains(flag));

        ComponentUnderTest = RenderComponent<TComponentUnderTest>(parameterBuilder =>
        {
            parameterBuilder
                .AddCascadingValue("Self", ActingUser) // Use the passed actingUser or a default new User.
                .AddCascadingValue("ProjectState", ProjectState)
                .AddCascadingValue("LiveUpdateHubConnectionWrapper", LiveUpdateConnectionWrapperMock.Object);

            // Allow the extendParameterBuilder action to add or override parameters.
            extendParameterBuilder?.Invoke(parameterBuilder);
        });
    }
}