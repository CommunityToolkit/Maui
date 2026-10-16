namespace CommunityToolkit.Maui.DeviceTests.WinUI;

// Same as dotnet/maui's device-test apps. The visual runner's home page implements the headless contract:
// "<App>.exe <results.xml>" runs every test and writes xUnit v2 XML to that path; "<App>.exe <results.xml> -1" writes the
// test categories to devicetestcategories.txt next to it, and "<App>.exe <results.xml> <index>" runs only that category.
public partial class App : MauiWinUIApplication
{
	public App() => InitializeComponent();

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}