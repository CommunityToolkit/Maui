using System.Diagnostics;
using System.Reflection;
using System.Text;

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

		var outputText = new StringBuilder();

		// Log platform diagnostics to help debug platform-specific issues
		outputText.AppendLine($"Platform: {DeviceInfo.Platform} {DeviceInfo.VersionString}");
		outputText.AppendLine($"Runtime: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
		outputText.AppendLine($"Assemblies: {testOptions.Assemblies.Count}");

		foreach (var asm in testOptions.Assemblies)
		{
			outputText.AppendLine($"  - {asm.GetName().Name} (Location: {asm.Location ?? "(empty)"})");
		}

		outputText.AppendLine();

		// Update UI with diagnostics before running
		outputLabel.Text = outputText.ToString();

		var runner = new DeviceRunner(testOptions.Assemblies, testOptions.SkipCategories);

		try
		{
			await Task.Run(runner.RunAll);

			// Use ASCII-safe status text (emoji may not render on all Android devices)
			if (runner.FailedTests > 0)
			{
				statusLabel.Text = "[FAIL] TESTS FAILED";
				statusLabel.TextColor = Colors.Red;
			}
			else if (runner.TotalTests == 0)
			{
				statusLabel.Text = "[WARN] NO TESTS FOUND";
				statusLabel.TextColor = Colors.Orange;
			}
			else
			{
				statusLabel.Text = "[PASS] ALL TESTS PASSED";
				statusLabel.TextColor = Colors.Green;
			}

			summaryLabel.Text = $"Total: {runner.TotalTests} | Passed: {runner.PassedTests} | Failed: {runner.FailedTests} | Skipped: {runner.SkippedTests}";

			foreach (var failure in runner.FailureMessages)
			{
				outputText.AppendLine(failure);
			}

			outputLabel.Text = outputText.ToString();

			Trace.WriteLine($"[DeviceRunner] Results — Total: {runner.TotalTests}, Passed: {runner.PassedTests}, Failed: {runner.FailedTests}, Skipped: {runner.SkippedTests}");
		}
		catch (Exception ex)
		{
			statusLabel.Text = "[CRASH] Test run crashed";
			statusLabel.TextColor = Colors.Red;
			outputText.AppendLine();
			outputText.AppendLine($"EXCEPTION: {ex}");
			outputLabel.Text = outputText.ToString();

			Trace.WriteLine($"[DeviceRunner] Crash: {ex}");
		}
	}
}
