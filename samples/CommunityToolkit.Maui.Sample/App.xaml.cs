using Application = Microsoft.Maui.Controls.Application;

namespace CommunityToolkit.Maui.Sample;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
		MainPage = new AppShell();
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

#endif
}