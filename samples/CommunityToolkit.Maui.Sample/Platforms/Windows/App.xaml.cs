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
		if (!IsSingleInstance())
		{
			Process.GetCurrentProcess().Kill();
		}
		else
		{
			base.OnLaunched(args);
		}
	}


	static bool IsSingleInstance()
	{
		const string applicationId = "1F9C3A44-059B-4FBC-9D92-476E59FB937A";
		mutex = new Mutex(false, applicationId);

		// keep the mutex reference alive until the normal 
		// termination of the program
		GC.KeepAlive(mutex);

		try
		{
			return mutex.WaitOne(0, false);
		}
		catch (AbandonedMutexException)
		{
			// if one thread acquires a Mutex object 
			// that another thread has abandoned 
			// by exiting without releasing it
			mutex.ReleaseMutex();
			return mutex.WaitOne(0, false);
		}
	}
}