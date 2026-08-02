namespace CommunityToolkit.Maui.DeviceTests;

public partial class App : Application
{
	protected override Window CreateWindow(IActivationState? activationState)
	{
		var statusLabel = new Label
		{
			Text = "CommunityToolkit.Maui Device Tests Running...",
			HorizontalOptions = LayoutOptions.Center,
			FontSize = 20,
			FontAttributes = FontAttributes.Bold
		};

		var summaryLabel = new Label
		{
			HorizontalOptions = LayoutOptions.Center,
			FontSize = 16,
			MaxLines = 0
		};

		var outputLabel = new Label
		{
			HorizontalOptions = LayoutOptions.Start,
			FontSize = 12,
			MaxLines = 0,
			FontFamily = "Monospace"
		};

		var resultsLayout = new VerticalStackLayout
		{
			Children = { statusLabel, summaryLabel, outputLabel },
			HorizontalOptions = LayoutOptions.Fill,
			Spacing = 12,
			Padding = 16
		};

		var resultsView = new ScrollView { Content = resultsLayout };

		var page = new ContentPage { Content = resultsView };

		var window = new Window(page);

		window.Created += async (_, _) =>
		{
			// Allow the UI to fully initialize before running tests
			await Task.Delay(1000);

			// Stream each line of output into the UI as it is written, matching the Trace output
			var outputText = new System.Text.StringBuilder();
			EventHandler<string> onOutputWritten = (_, line) =>
			{
				outputText.AppendLine(line);
				outputLabel.Text = outputText.ToString();
			};

			InAppTestRunner.OutputWritten += onOutputWritten;

			try
			{
				var exitCode = await InAppTestRunner.RunTestsAsync();

				// Some tests (e.g. HandlerTests) replace the window's page with their own,
				// so restore the results page to keep it clean for displaying the outcome.
				window.Page = page;

				// Update the UI with results and keep the app alive so results stay visible
				statusLabel.Text = exitCode == 0 ? "✅ ALL TESTS PASSED" : "❌ TESTS FAILED";
				statusLabel.TextColor = exitCode == 0 ? Colors.Green : Colors.Red;
				summaryLabel.Text = InAppTestRunner.LastRunSummary;
			}
			finally
			{
				InAppTestRunner.OutputWritten -= onOutputWritten;
			}
		};

		return window;
	}
}
