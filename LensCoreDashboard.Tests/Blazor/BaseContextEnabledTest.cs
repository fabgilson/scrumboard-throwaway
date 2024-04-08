using Blazorise;
using Blazorise.Bootstrap5;
using Blazorise.Icons.FontAwesome;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using SharedLensResources;

namespace LensCoreDashboard.Tests.Blazor;

public abstract class BaseContextEnabledTest : TestContext
{
    protected Mock<LensAuthenticationService.LensAuthenticationServiceClient> LensAuthClientMock { get; private set; } = new(MockBehavior.Loose);
    protected Mock<LensUserService.LensUserServiceClient> LensUserClientMock { get; private set; } = new(MockBehavior.Loose);
    
    protected virtual IRenderedComponent<T> CreateComponent<T>(Action<ComponentParameterCollectionBuilder<T>>? parameterBuilder=null) where T : ComponentBase
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        
        // Per Blazorise docs, mock an internal transient service
        Services.AddBlazorise(o => o.Immediate = true)
            .Replace(ServiceDescriptor.Transient<IComponentActivator, ComponentActivator>())
            .AddBootstrap5Providers()
            .AddFontAwesomeIcons();

        Services.AddTransient<LensAuthenticationService.LensAuthenticationServiceClient>(_ => LensAuthClientMock.Object);
        Services.AddTransient<LensUserService.LensUserServiceClient>(_ => LensUserClientMock.Object);
        var cut = RenderComponent<T>(parameterBuilder);
        return cut;
    }
}