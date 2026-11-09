using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

class MockApplication : Application, IPlatformApplication
{
	Window? windowToOpen;

	public MockApplication(IServiceProvider serviceProvider)
	{
		Services = serviceProvider;
	}

	public IApplication Application => this;
	public IServiceProvider Services { get; }

	public override void OpenWindow(Window window)
	{
		windowToOpen = window;
		try
		{
			_ = ((IApplication)this).CreateWindow(null);
		}
		finally
		{
			windowToOpen = null;
		}
	}

	protected override Window CreateWindow(IActivationState? activationState) =>
		windowToOpen ?? base.CreateWindow(activationState);
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