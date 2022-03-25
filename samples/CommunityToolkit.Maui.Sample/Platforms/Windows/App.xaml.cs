using System.Diagnostics;
using Microsoft.UI.Xaml;

namespace CommunityToolkit.Maui.Sample.Windows;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : MauiWinUIApplication
{
	/// <summary>
	/// Initializes the singleton application object.  This is the first line of authored code
	/// executed, and as such is the logical equivalent of main() or WinMain().
	/// </summary>
	public App()
	{
		this.InitializeComponent();
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

	static Mutex? mutex;

	protected override void OnLaunched(LaunchActivatedEventArgs args)
	{
		const string applicationId = "1F9C3A44-059B-4FBC-9D92-476E59FB937A";

		mutex = new Mutex(true, applicationId, out var createdNew);

		if (!createdNew)
		{
			Process.GetCurrentProcess().Kill();
		}
		else
		{
			base.OnLaunched(args);
		}
	}
}