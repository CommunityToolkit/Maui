using CommunityToolkit.Maui.DeviceTests.UnitTests.Fixtures;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.DeviceTests.UnitTests;

public sealed class InAppTestRunnerTests
{
	[Fact]
	public async Task RunTestsAsync_WithPassingFixture_ReturnsZero()
	{
		var exitCode = await InAppTestRunner.RunTestsAsync(
			typeof(PassingTestFixture).Assembly,
			t => t == typeof(PassingTestFixture),
			TestContext.Current.CancellationToken);

		exitCode.Should().Be(0);
	}

	[Fact]
	public async Task RunTestsAsync_WithFailingFixture_ReturnsOne()
	{
		var exitCode = await InAppTestRunner.RunTestsAsync(
			typeof(FailingTestFixture).Assembly,
			t => t == typeof(FailingTestFixture),
			TestContext.Current.CancellationToken);

		exitCode.Should().Be(1);
	}

	[Fact]
	public async Task RunTestsAsync_WithSkippedFact_ReturnsZero()
	{
		var exitCode = await InAppTestRunner.RunTestsAsync(
			typeof(SkippedTestFixture).Assembly,
			t => t == typeof(SkippedTestFixture),
			TestContext.Current.CancellationToken);

		exitCode.Should().Be(0);
	}

	[Fact]
	public async Task RunTestsAsync_WithSkippedTheory_ReturnsZero()
	{
		var exitCode = await InAppTestRunner.RunTestsAsync(
			typeof(SkippedTheoryFixture).Assembly,
			t => t == typeof(SkippedTheoryFixture),
			TestContext.Current.CancellationToken);

		exitCode.Should().Be(0);
	}

	[Fact]
	public async Task RunTestsAsync_WithThrowingTest_ReturnsOne()
	{
		var exitCode = await InAppTestRunner.RunTestsAsync(
			typeof(ThrowingTestFixture).Assembly,
			t => t == typeof(ThrowingTestFixture),
			TestContext.Current.CancellationToken);

		exitCode.Should().Be(1);
	}

	[Fact]
	public async Task RunTestsAsync_WithAsyncPassingTest_ReturnsZero()
	{
		var exitCode = await InAppTestRunner.RunTestsAsync(
			typeof(AsyncPassingTestFixture).Assembly,
			t => t == typeof(AsyncPassingTestFixture),
			TestContext.Current.CancellationToken);

		exitCode.Should().Be(0);
	}

	[Fact]
	public async Task RunTestsAsync_WithAsyncFailingTest_ReturnsOne()
	{
		var exitCode = await InAppTestRunner.RunTestsAsync(
			typeof(AsyncFailingTestFixture).Assembly,
			t => t == typeof(AsyncFailingTestFixture),
			TestContext.Current.CancellationToken);

		exitCode.Should().Be(1);
	}

	[Fact]
	public async Task RunTestsAsync_WithValueTaskTest_ReturnsZero()
	{
		var exitCode = await InAppTestRunner.RunTestsAsync(
			typeof(ValueTaskTestFixture).Assembly,
			t => t == typeof(ValueTaskTestFixture),
			TestContext.Current.CancellationToken);

		exitCode.Should().Be(0);
	}

	[Fact]
	public async Task RunTestsAsync_WithMultipleTests_ReturnsOneWhenAnyFail()
	{
		var exitCode = await InAppTestRunner.RunTestsAsync(
			typeof(MultipleTestsFixture).Assembly,
			t => t == typeof(MultipleTestsFixture),
			TestContext.Current.CancellationToken);

		exitCode.Should().Be(1);
	}

	[Fact]
	public async Task RunTestsAsync_WithAllPassingMultipleFixtures_ReturnsZero()
	{
		var exitCode = await InAppTestRunner.RunTestsAsync(
			typeof(PassingTestFixture).Assembly,
			t => t == typeof(PassingTestFixture) || t == typeof(AsyncPassingTestFixture),
			TestContext.Current.CancellationToken);

		exitCode.Should().Be(0);
	}

	[Fact]
	public async Task RunTestsAsync_WithThrowingConstructor_ReturnsOne()
	{
		var exitCode = await InAppTestRunner.RunTestsAsync(
			typeof(ThrowingConstructorFixture).Assembly,
			t => t == typeof(ThrowingConstructorFixture),
			TestContext.Current.CancellationToken);

		exitCode.Should().Be(1);
	}

	[Fact]
	public async Task RunTestsAsync_WithDisposableFixture_DisposesInstance()
	{
		DisposableTestFixture.Reset();

		await InAppTestRunner.RunTestsAsync(
			typeof(DisposableTestFixture).Assembly,
			t => t == typeof(DisposableTestFixture),
			TestContext.Current.CancellationToken);

		DisposableTestFixture.WasDisposed.Should().BeTrue();
	}

	[Fact]
	public async Task RunTestsAsync_WithAsyncDisposableFixture_DisposesAsync()
	{
		AsyncDisposableTestFixture.Reset();

		await InAppTestRunner.RunTestsAsync(
			typeof(AsyncDisposableTestFixture).Assembly,
			t => t == typeof(AsyncDisposableTestFixture),
			TestContext.Current.CancellationToken);

		AsyncDisposableTestFixture.WasDisposedAsync.Should().BeTrue();
	}

	[Fact]
	public async Task RunTestsAsync_WithTheoryFixture_ReturnsZero()
	{
		var exitCode = await InAppTestRunner.RunTestsAsync(
			typeof(TheoryTestFixture).Assembly,
			t => t == typeof(TheoryTestFixture),
			TestContext.Current.CancellationToken);

		exitCode.Should().Be(0);
	}

	[Fact]
	public async Task RunTestsAsync_WithNoMatchingTypes_ReturnsZero()
	{
		var exitCode = await InAppTestRunner.RunTestsAsync(
			typeof(PassingTestFixture).Assembly,
			_ => false,
			TestContext.Current.CancellationToken);

		exitCode.Should().Be(0);
	}

	[Fact]
	public async Task RunTestsAsync_WithNullFilter_DiscoversAllTestClasses()
	{
		// When no filter is provided, all test classes in the assembly are discovered.
		// We exclude InAppTestRunnerTests itself to prevent infinite recursion
		// (the runner would otherwise discover and re-execute this test class).
		var exitCode = await InAppTestRunner.RunTestsAsync(
			typeof(PassingTestFixture).Assembly,
			t => t != typeof(InAppTestRunnerTests),
			TestContext.Current.CancellationToken);

		exitCode.Should().Be(1);
	}

	[Fact]
	public async Task RunTestsAsync_WithMixedSyncAsyncFixture_ReturnsOne()
	{
		var exitCode = await InAppTestRunner.RunTestsAsync(
			typeof(MixedSyncAsyncFixture).Assembly,
			t => t == typeof(MixedSyncAsyncFixture),
			TestContext.Current.CancellationToken);

		exitCode.Should().Be(1);
	}

	[Fact]
	public async Task RunTestsAsync_WithCancelledToken_ThrowsOperationCanceledException()
	{
		using var cts = new CancellationTokenSource();
		cts.Cancel();

		var act = () => InAppTestRunner.RunTestsAsync(
			typeof(PassingTestFixture).Assembly,
			t => t == typeof(PassingTestFixture),
			cancellationToken: cts.Token);

		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	[Fact]
	public async Task RunTestsAsync_OutputsPassResult()
	{
		var originalOut = Console.Out;
		using var writer = new StringWriter();
		Console.SetOut(writer);

		try
		{
			await InAppTestRunner.RunTestsAsync(
				typeof(PassingTestFixture).Assembly,
				t => t == typeof(PassingTestFixture),
				TestContext.Current.CancellationToken);
		}
		finally
		{
			Console.SetOut(originalOut);
		}

		var output = writer.ToString();
		output.Should().Contain("[PASS]");
		output.Should().Contain("RESULT: PASSED");
		output.Should().Contain("Passed:  1");
		output.Should().Contain("Failed:  0");
	}

	[Fact]
	public async Task RunTestsAsync_OutputsFailResult()
	{
		var originalOut = Console.Out;
		using var writer = new StringWriter();
		Console.SetOut(writer);

		try
		{
			await InAppTestRunner.RunTestsAsync(
				typeof(FailingTestFixture).Assembly,
				t => t == typeof(FailingTestFixture),
				TestContext.Current.CancellationToken);
		}
		finally
		{
			Console.SetOut(originalOut);
		}

		var output = writer.ToString();
		output.Should().Contain("[FAIL]");
		output.Should().Contain("RESULT: FAILED");
		output.Should().Contain("Failed:  1");
	}

	[Fact]
	public async Task RunTestsAsync_OutputsSkipResult()
	{
		var originalOut = Console.Out;
		using var writer = new StringWriter();
		Console.SetOut(writer);

		try
		{
			await InAppTestRunner.RunTestsAsync(
				typeof(SkippedTestFixture).Assembly,
				t => t == typeof(SkippedTestFixture),
				TestContext.Current.CancellationToken);
		}
		finally
		{
			Console.SetOut(originalOut);
		}

		var output = writer.ToString();
		output.Should().Contain("[SKIP]");
		output.Should().Contain("Skipped: 1");
		output.Should().Contain("RESULT: PASSED");
	}

	[Fact]
	public async Task RunTestsAsync_OutputsErrorForConstructorFailure()
	{
		var originalOut = Console.Out;
		using var writer = new StringWriter();
		Console.SetOut(writer);

		try
		{
			await InAppTestRunner.RunTestsAsync(
				typeof(ThrowingConstructorFixture).Assembly,
				t => t == typeof(ThrowingConstructorFixture),
				TestContext.Current.CancellationToken);
		}
		finally
		{
			Console.SetOut(originalOut);
		}

		var output = writer.ToString();
		output.Should().Contain("[ERROR]");
		output.Should().Contain("Failed to create instance");
	}

	[Fact]
	public async Task RunTestsAsync_OutputsTestClassCount()
	{
		var originalOut = Console.Out;
		using var writer = new StringWriter();
		Console.SetOut(writer);

		try
		{
			await InAppTestRunner.RunTestsAsync(
				typeof(PassingTestFixture).Assembly,
				t => t == typeof(PassingTestFixture) || t == typeof(FailingTestFixture),
				TestContext.Current.CancellationToken);
		}
		finally
		{
			Console.SetOut(originalOut);
		}

		var output = writer.ToString();
		output.Should().Contain("Test Classes Found: 2");
	}

	[Fact]
	public async Task RunTestsAsync_DoesNotDiscoverAbstractClasses()
	{
		var originalOut = Console.Out;
		using var writer = new StringWriter();
		Console.SetOut(writer);

		try
		{
			await InAppTestRunner.RunTestsAsync(
				typeof(AbstractTestFixture).Assembly,
				t => t.Namespace?.EndsWith(".Fixtures", StringComparison.Ordinal) is true,
				TestContext.Current.CancellationToken);
		}
		finally
		{
			Console.SetOut(originalOut);
		}

		var output = writer.ToString();
		output.Should().NotContain("AbstractTestFixture");
	}

	[Fact]
	public async Task RunTestsAsync_DoesNotDiscoverClassesWithoutTests()
	{
		var originalOut = Console.Out;
		using var writer = new StringWriter();
		Console.SetOut(writer);

		try
		{
			await InAppTestRunner.RunTestsAsync(
				typeof(NoTestsFixture).Assembly,
				t => t == typeof(NoTestsFixture),
				TestContext.Current.CancellationToken);
		}
		finally
		{
			Console.SetOut(originalOut);
		}

		var output = writer.ToString();
		output.Should().Contain("Test Classes Found: 0");
	}

	[Fact]
	public async Task RunTestsAsync_WithMultipleFixtureTypes_CountsCorrectly()
	{
		var originalOut = Console.Out;
		using var writer = new StringWriter();
		Console.SetOut(writer);

		try
		{
			await InAppTestRunner.RunTestsAsync(
				typeof(PassingTestFixture).Assembly,
				t => t == typeof(PassingTestFixture) || t == typeof(FailingTestFixture) || t == typeof(SkippedTestFixture),
				TestContext.Current.CancellationToken);
		}
		finally
		{
			Console.SetOut(originalOut);
		}

		var output = writer.ToString();
		output.Should().Contain("Total:   3");
		output.Should().Contain("Passed:  1");
		output.Should().Contain("Failed:  1");
		output.Should().Contain("Skipped: 1");
	}
}
