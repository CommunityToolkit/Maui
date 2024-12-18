using Application = Microsoft.Maui.Controls.Application;

namespace CommunityToolkit.Maui.Sample;

public partial class App : Application
{
	readonly AppShell appShell;

	public App(AppShell appShell)
	{
		InitializeComponent();
		this.appShell = appShell;
	}

#if MACCATALYST
	protected override Window CreateWindow(IActivationState? activationState)
    {
        Window window = base.CreateWindow(activationState);
        window.Destroying += (object? sender, EventArgs args) =>
        {
            if (Current?.Windows?.Count - 1 == 0) 
			{
				// Exit the app when the last window closes
				// This ensures app closes when last window closes on Mac
				Environment.Exit(0);
			}
        };
        return window;
    }

#else
	protected override Window CreateWindow(IActivationState? activationState) => new(appShell);
#endif
}