using Application = Microsoft.Maui.Controls.Application;

namespace CommunityToolkit.Maui.Sample;

public partial class App : Application
{
#if ANDROID
	AppShell? appShell;
#endif

	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
#if ANDROID
		// Android asks for the window again when it creates the Activity again in the same process, e.g. after the app language
		// changed or with "Don't keep activities": reuse the AppShell, so that the app stays on the page it showed.
		return new(appShell ??= new AppShell());
#else
		// A new AppShell for every window, as in the .NET MAUI template: a Shell can be the page of one window only, and the AppShell
		// of a destroyed window keeps that window's services. Mac Catalyst asks for a window for every scene it connects, e.g. after a
		// click on the Dock icon once the last window was closed, or for each window it restores at launch
		// (Platforms/MacCatalyst/SceneDelegate.cs keeps one AppShell window).
		return new(new AppShell());
#endif
	}
}