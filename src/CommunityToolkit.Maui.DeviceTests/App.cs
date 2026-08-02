using CommunityToolkit.Maui.DeviceTests.Runners;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class App : Application
{
	readonly VisualRunnerPage visualRunnerPage;

	public App(VisualRunnerPage visualRunnerPage)
	{
		this.visualRunnerPage = visualRunnerPage;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(visualRunnerPage);
	}
}
