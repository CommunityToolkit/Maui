namespace CommunityToolkit.Maui.DeviceTests.WinUI;

// Same as dotnet/maui's device-test apps.
public partial class App : MauiWinUIApplication
{
	public App() => InitializeComponent();

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}