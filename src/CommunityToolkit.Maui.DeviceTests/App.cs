namespace CommunityToolkit.Maui.DeviceTests;

public partial class App : Application
{
	protected override Window CreateWindow(IActivationState? activationState)
	{
		var page = new ContentPage
		{
			Content = new Label
			{
				Text = "CommunityToolkit.Maui Device Tests Running...",
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center
			}
		};

		var window = new Window(page);

		window.Created += async (_, _) =>
		{
			// Allow the UI to fully initialize before running tests
			await Task.Delay(1000);
			var exitCode = await InAppTestRunner.RunTestsAsync();
			Console.WriteLine($"Test run completed with exit code: {exitCode}");
		};

		return window;
	}
}
