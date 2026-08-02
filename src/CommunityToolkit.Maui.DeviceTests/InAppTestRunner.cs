using System.Diagnostics;
using System.Reflection;
using System.Text;
using Xunit;
using Xunit.Sdk;

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// Runs xunit tests inside the MAUI application and reports results to the console.
/// </summary>
public static class InAppTestRunner
{
	static readonly StringBuilder outputBuilder = new();

	/// <summary>Gets the summary text from the most recent test run.</summary>
	public static string LastRunSummary { get; private set; } = string.Empty;

	/// <summary>Gets the full captured output from the most recent test run.</summary>
	public static string LastRunOutput { get; private set; } = string.Empty;

	/// <summary>Raised for each line of output as it is written, allowing live UI updates.</summary>
	public static event EventHandler<string>? OutputWritten;

	public static async Task<int> RunTestsAsync(CancellationToken cancellationToken = default)
		=> await RunTestsAsync(typeof(InAppTestRunner).Assembly, cancellationToken: cancellationToken);

	public static async Task<int> RunTestsAsync(Assembly assembly, Func<Type, bool>? typeFilter = null, CancellationToken cancellationToken = default)
	{
		outputBuilder.Clear();

		var testClasses = assembly.GetTypes()
			.Where(t => t.IsClass && !t.IsAbstract && t.GetMethods().Any(m => m.GetCustomAttribute<FactAttribute>() is not null || m.GetCustomAttribute<TheoryAttribute>() is not null))
			.Where(t => typeFilter?.Invoke(t) ?? true)
			.ToList();

		var totalTests = 0;
		var passedTests = 0;
		var failedTests = 0;
		var skippedTests = 0;

		Log("=== CommunityToolkit.Maui Device Tests ===");
		Log($"Test Classes Found: {testClasses.Count}");
		Log(string.Empty);

		foreach (var testClass in testClasses)
		{
			Log($"--- {testClass.Name} ---");

			object? instance = null;

			try
			{
				instance = Activator.CreateInstance(testClass);
			}
			catch (Exception ex)
			{
				Log($"  [ERROR] Failed to create instance: {ex.Message}");
				failedTests++;
				totalTests++;
				continue;
			}

			var testMethods = testClass.GetMethods(BindingFlags.Public | BindingFlags.Instance)
				.Where(m => m.GetCustomAttribute<FactAttribute>() is not null || m.GetCustomAttribute<TheoryAttribute>() is not null)
				.ToList();

			foreach (var method in testMethods)
			{
				var factAttribute = method.GetCustomAttribute<FactAttribute>();
				var theoryAttribute = method.GetCustomAttribute<TheoryAttribute>();

				if (factAttribute?.Skip is not null)
				{
					Log($"  [SKIP] {method.Name}: {factAttribute.Skip}");
					totalTests++;
					skippedTests++;
					continue;
				}

				if (theoryAttribute?.Skip is not null)
				{
					Log($"  [SKIP] {method.Name}: {theoryAttribute.Skip}");
					totalTests++;
					skippedTests++;
					continue;
				}

				cancellationToken.ThrowIfCancellationRequested();

				if (theoryAttribute is not null)
				{
					var inlineDataAttributes = method.GetCustomAttributes<InlineDataAttribute>();
					var hasInlineData = false;
					await using var disposalTracker = new DisposalTracker();

					foreach (var inlineData in inlineDataAttributes)
					{
						var theoryDataRows = await inlineData.GetData(method, disposalTracker);

						foreach (var row in theoryDataRows)
						{
							hasInlineData = true;
							totalTests++;
							var data = row.GetData();

							try
							{
								var result = method.Invoke(instance, data);

								if (result is Task task)
								{
									await task;
								}
								else if (result is ValueTask valueTask)
								{
									await valueTask;
								}

								Log($"  [PASS] {method.Name}({string.Join(", ", data)})");
								passedTests++;
							}
							catch (TargetInvocationException tie) when (tie.InnerException is not null)
							{
								Log($"  [FAIL] {method.Name}({string.Join(", ", data)}): {tie.InnerException.Message}");
								failedTests++;
							}
							catch (Exception ex)
							{
								Log($"  [FAIL] {method.Name}({string.Join(", ", data)}): {ex.Message}");
								failedTests++;
							}
						}
					}

					if (!hasInlineData)
					{
						Log($"  [SKIP] {method.Name}: No InlineData found");
						totalTests++;
						skippedTests++;
					}

					continue;
				}

				totalTests++;

				try
				{
					var result = method.Invoke(instance, null);

					if (result is Task task)
					{
						await task;
					}
					else if (result is ValueTask valueTask)
					{
						await valueTask;
					}

					Log($"  [PASS] {method.Name}");
					passedTests++;
				}
				catch (TargetInvocationException tie) when (tie.InnerException is not null)
				{
					Log($"  [FAIL] {method.Name}: {tie.InnerException.Message}");
					failedTests++;
				}
				catch (Exception ex)
				{
					Log($"  [FAIL] {method.Name}: {ex.Message}");
					failedTests++;
				}
			}

			if (instance is IDisposable disposable)
			{
				disposable.Dispose();
			}

			if (instance is IAsyncDisposable asyncDisposable)
			{
				await asyncDisposable.DisposeAsync();
			}

			Log(string.Empty);
		}

		var resultText = failedTests > 0 ? "RESULT: FAILED" : "RESULT: PASSED";
		var summary = $"Total:   {totalTests}\nPassed:  {passedTests}\nFailed:  {failedTests}\nSkipped: {skippedTests}\n\n{resultText}";

		Log("=== Test Results ===");
		Log(summary);
		Log(string.Empty);

		LastRunSummary = summary;
		LastRunOutput = outputBuilder.ToString();

		return failedTests > 0 ? 1 : 0;
	}

	static void Log(string message)
	{
		Trace.WriteLine(message);
		outputBuilder.AppendLine(message);
		OutputWritten?.Invoke(null, message);
	}
}
