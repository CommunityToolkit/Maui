using Application = Microsoft.Maui.Controls.Application;
#if ANDROID
using CommunityToolkit.Maui.Sample.Pages.Behaviors;
#endif

namespace CommunityToolkit.Maui.Sample;

public partial class App : Application
{
	readonly AppShell appShell;

	public App(AppShell appShell)
	{
		InitializeComponent();

		this.appShell = appShell;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
#if ANDROID
		// Every Activity needs its own Window and appShell can only belong to one, so the Shell demo's Activity gets a Shell of its own.
		// This is used to regression test the StatusBarBehavior
		if (activationState?.Context.Services.GetService(typeof(global::Android.App.Activity)) is EdgeToEdgeShellActivity)
		{
			return new Window(new EdgeToEdgeTitleViewShell());
		}
#endif

		return new(appShell);
	}
}