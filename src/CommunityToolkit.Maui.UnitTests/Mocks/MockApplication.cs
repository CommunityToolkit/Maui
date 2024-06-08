using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

class MockApplication(IServiceProvider serviceProvider) : Application, IPlatformApplication
{
	public new Application? Current = null;
	public IServiceProvider Services { get; } = serviceProvider;
	public IApplication Application => this;
}

// Inspired by https://github.com/dotnet/maui/blob/main/src/Controls/tests/Core.UnitTests/TestClasses/ApplicationHandlerStub.cs
class ApplicationHandlerStub : ElementHandler<IApplication, object>
{
	public ApplicationHandlerStub() : base(Mapper)
	{
	}

	public static IPropertyMapper<IApplication, ApplicationHandlerStub> Mapper = new PropertyMapper<IApplication, ApplicationHandlerStub>(ElementMapper);

	protected override object CreatePlatformElement()
	{
		return new object();
	}
}