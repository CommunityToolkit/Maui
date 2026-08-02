using System.Reflection;
using Xunit;
using Xunit.Sdk;

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// Runs xunit tests inside the MAUI application and reports results to the console.
/// </summary>
public static class InAppTestRunner
{
	public static async Task<int> RunTestsAsync(CancellationToken cancellationToken = default)
		=> await RunTestsAsync(typeof(InAppTestRunner).Assembly, cancellationToken: cancellationToken);

	public static async Task<int> RunTestsAsync(Assembly assembly, Func<Type, bool>? typeFilter = null, CancellationToken cancellationToken = default)
	{
		var testClasses = assembly.GetTypes()
			.Where(t => t.IsClass && !t.IsAbstract && t.GetMethods().Any(m => m.GetCustomAttribute<FactAttribute>() is not null || m.GetCustomAttribute<TheoryAttribute>() is not null))
			.Where(t => typeFilter?.Invoke(t) ?? true)
			.ToList();

		var totalTests = 0;
		var passedTests = 0;
		var failedTests = 0;
		var skippedTests = 0;

		Console.WriteLine("=== CommunityToolkit.Maui Device Tests ===");
		Console.WriteLine($"Test Classes Found: {testClasses.Count}");
		Console.WriteLine();

		foreach (var testClass in testClasses)
		{
			Console.WriteLine($"--- {testClass.Name} ---");

			object? instance = null;

			try
			{
				instance = Activator.CreateInstance(testClass);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"  [ERROR] Failed to create instance: {ex.Message}");
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
					Console.WriteLine($"  [SKIP] {method.Name}: {factAttribute.Skip}");
					totalTests++;
					skippedTests++;
					continue;
				}

				if (theoryAttribute?.Skip is not null)
				{
					Console.WriteLine($"  [SKIP] {method.Name}: {theoryAttribute.Skip}");
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

								Console.WriteLine($"  [PASS] {method.Name}({string.Join(", ", data)})");
								passedTests++;
							}
							catch (TargetInvocationException tie) when (tie.InnerException is not null)
							{
								Console.WriteLine($"  [FAIL] {method.Name}({string.Join(", ", data)}): {tie.InnerException.Message}");
								failedTests++;
							}
							catch (Exception ex)
							{
								Console.WriteLine($"  [FAIL] {method.Name}({string.Join(", ", data)}): {ex.Message}");
								failedTests++;
							}
						}
					}

					if (!hasInlineData)
					{
						Console.WriteLine($"  [SKIP] {method.Name}: No InlineData found");
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

					Console.WriteLine($"  [PASS] {method.Name}");
					passedTests++;
				}
				catch (TargetInvocationException tie) when (tie.InnerException is not null)
				{
					Console.WriteLine($"  [FAIL] {method.Name}: {tie.InnerException.Message}");
					failedTests++;
				}
				catch (Exception ex)
				{
					Console.WriteLine($"  [FAIL] {method.Name}: {ex.Message}");
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

			Console.WriteLine();
		}

		Console.WriteLine("=== Test Results ===");
		Console.WriteLine($"Total:   {totalTests}");
		Console.WriteLine($"Passed:  {passedTests}");
		Console.WriteLine($"Failed:  {failedTests}");
		Console.WriteLine($"Skipped: {skippedTests}");
		Console.WriteLine();

		if (failedTests > 0)
		{
			Console.WriteLine("RESULT: FAILED");
			return 1;
		}

		Console.WriteLine("RESULT: PASSED");
		return 0;
	}
}
