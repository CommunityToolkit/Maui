using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

class MockApplication : Application, IPlatformApplication
{
	public MockApplication(IServiceProvider serviceProvider)
	{
		Services = serviceProvider;
#pragma warning disable CS0612 // Type or member is obsolete
		DependencyService.Register<ISystemResourcesProvider, MockResourcesProvider>();
#pragma warning restore CS0612 // Type or member is obsolete
	}

	public IApplication Application => this;
	public IServiceProvider Services { get; }
}

// Inspired by https://github.com/dotnet/maui/blob/main/src/Controls/tests/Core.UnitTests/TestClasses/ApplicationHandlerStub.cs
class ApplicationHandlerStub() : ElementHandler<IApplication, object>(Mapper)
{
	public static IPropertyMapper<IApplication, ApplicationHandlerStub> Mapper = new PropertyMapper<IApplication, ApplicationHandlerStub>(ElementMapper);

	protected override object CreatePlatformElement()
	{
		return new object();
	}
}