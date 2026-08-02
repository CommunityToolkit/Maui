namespace CommunityToolkit.Maui.DeviceTests.Runners;

/// <summary>
/// A visual in-app test runner page that discovers and runs tests, displaying results in the UI.
/// Mirrors the pattern used by the .NET MAUI team's TestUtils.DeviceTests.Runners.
/// </summary>
public sealed partial class VisualRunnerPage : ContentPage
{
	readonly TestOptions testOptions;
	readonly Label statusLabel;
	readonly Label summaryLabel;
	readonly Label outputLabel;

	bool hasRun;

	public VisualRunnerPage(TestOptions testOptions)
	{
		this.testOptions = testOptions;

		Title = "Device Tests";

		statusLabel = new Label
		{
			Text = "CommunityToolkit.Maui Device Tests",
			HorizontalOptions = LayoutOptions.Center,
			FontSize = 20,
			FontAttributes = FontAttributes.Bold
		};

		summaryLabel = new Label
		{
			HorizontalOptions = LayoutOptions.Center,
			FontSize = 16,
			MaxLines = 0
		};

		outputLabel = new Label
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

		Content = new ScrollView { Content = resultsLayout };
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		// Tests must only run once. Restoring window.Page after the run re-fires
		// OnAppearing, so guard against re-running to avoid an infinite loop.
		if (hasRun)
		{
			return;
		}

		hasRun = true;

		// Allow the UI to fully initialize before running tests
		await Task.Delay(1000);

		await RunTestsAsync();
	}

	async Task RunTestsAsync()
	{
		statusLabel.Text = "Running tests...";
		statusLabel.TextColor = Colors.Gray;

		var outputText = new System.Text.StringBuilder();

		var runner = new DeviceRunner(testOptions.Assemblies, testOptions.SkipCategories);

		try
		{
			await Task.Run(runner.RunAll);

			statusLabel.Text = runner.FailedTests > 0 ? "❌ TESTS FAILED" : "✅ ALL TESTS PASSED";
			statusLabel.TextColor = runner.FailedTests > 0 ? Colors.Red : Colors.Green;

			summaryLabel.Text = $"Total: {runner.TotalTests} | Passed: {runner.PassedTests} | Failed: {runner.FailedTests} | Skipped: {runner.SkippedTests}";

			foreach (var failure in runner.FailureMessages)
			{
				outputText.AppendLine(failure);
			}

			outputLabel.Text = outputText.ToString();
		}
		catch (Exception ex)
		{
			statusLabel.Text = "❌ Test run crashed";
			statusLabel.TextColor = Colors.Red;
			outputText.AppendLine(ex.ToString());
			outputLabel.Text = outputText.ToString();
		}
	}
}
