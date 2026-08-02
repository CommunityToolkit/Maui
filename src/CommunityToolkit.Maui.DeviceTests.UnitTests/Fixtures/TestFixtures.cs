using Xunit;

// Suppress xUnit1004 (skipped tests) and xUnit1013 (public non-test methods)
// because these fixture classes intentionally use Skip and helper methods
// to exercise InAppTestRunner behavior.
// Suppress IDE0040 because fixture methods must be explicitly public for
// InAppTestRunner's reflection-based discovery (BindingFlags.Public).
// Suppress xUnit1000 because these classes are intentionally internal to
// prevent xunit from discovering and running them directly.
#pragma warning disable xUnit1000
#pragma warning disable xUnit1004
#pragma warning disable xUnit1013
#pragma warning disable IDE0040

namespace CommunityToolkit.Maui.DeviceTests.UnitTests.Fixtures;

/// <summary>
/// Fixture class with a single passing test for InAppTestRunner discovery tests.
/// </summary>
internal class PassingTestFixture
{
	[Fact]
	public void PassingTest()
	{
		Assert.True(true);
	}
}

/// <summary>
/// Fixture class with a single failing test.
/// </summary>
internal class FailingTestFixture
{
	[Fact]
	public void FailingTest()
	{
		Assert.Fail("Intentional failure for testing");
	}
}

/// <summary>
/// Fixture class with a skipped test.
/// </summary>
internal class SkippedTestFixture
{
	[Fact(Skip = "Intentionally skipped for testing")]
	public void SkippedTest()
	{
		Assert.Fail("This should never execute");
	}
}

/// <summary>
/// Fixture class with a skipped theory.
/// </summary>
internal class SkippedTheoryFixture
{
	[Theory(Skip = "Theory intentionally skipped")]
	[InlineData(1)]
	public void SkippedTheoryTest(int value)
	{
		Assert.Equal(1, value);
	}
}

/// <summary>
/// Fixture class with a test that throws an exception.
/// </summary>
internal class ThrowingTestFixture
{
	[Fact]
	public void ThrowingTest()
	{
		throw new InvalidOperationException("Intentional exception for testing");
	}
}

/// <summary>
/// Fixture class with an async passing test.
/// </summary>
internal class AsyncPassingTestFixture
{
	[Fact]
	public async Task AsyncPassingTest()
	{
		await Task.Delay(1, TestContext.Current.CancellationToken);
		Assert.True(true);
	}
}

/// <summary>
/// Fixture class with an async failing test.
/// </summary>
internal class AsyncFailingTestFixture
{
	[Fact]
	public async Task AsyncFailingTest()
	{
		await Task.Delay(1, TestContext.Current.CancellationToken);
		Assert.Fail("Intentional async failure for testing");
	}
}

/// <summary>
/// Fixture class with a ValueTask test.
/// </summary>
internal class ValueTaskTestFixture
{
	[Fact]
	public async ValueTask ValueTaskPassingTest()
	{
		await Task.Delay(1, TestContext.Current.CancellationToken);
		Assert.True(true);
	}
}

/// <summary>
/// Fixture class with multiple test methods.
/// </summary>
internal class MultipleTestsFixture
{
	[Fact]
	public void FirstPassingTest()
	{
		Assert.True(true);
	}

	[Fact]
	public void SecondPassingTest()
	{
		Assert.Equal(2, 1 + 1);
	}

	[Fact]
	public void ThirdFailingTest()
	{
		Assert.Fail("Intentional failure in multi-test fixture");
	}
}

/// <summary>
/// Fixture class that implements IDisposable to test disposal behavior.
/// </summary>
internal sealed class DisposableTestFixture : IDisposable
{
	public static bool WasDisposed { get; private set; }

	[Fact]
	public void DisposableTest()
	{
		Assert.True(true);
	}

	public void Dispose()
	{
		WasDisposed = true;
		GC.SuppressFinalize(this);
	}

	public static void Reset() => WasDisposed = false;
}

/// <summary>
/// Fixture class that implements IAsyncDisposable to test async disposal behavior.
/// </summary>
internal sealed class AsyncDisposableTestFixture : IAsyncDisposable
{
	public static bool WasDisposedAsync { get; private set; }

	[Fact]
	public void AsyncDisposableTest()
	{
		Assert.True(true);
	}

	public ValueTask DisposeAsync()
	{
		WasDisposedAsync = true;
		GC.SuppressFinalize(this);
		return ValueTask.CompletedTask;
	}

	public static void Reset() => WasDisposedAsync = false;
}

/// <summary>
/// Fixture class with a theory test method.
/// </summary>
internal class TheoryTestFixture
{
	[Theory]
	[InlineData(1, 1, 2)]
	[InlineData(2, 3, 5)]
	public void TheoryTest(int a, int b, int expected)
	{
		Assert.Equal(expected, a + b);
	}
}

/// <summary>
/// Abstract class with test attributes — should NOT be discovered.
/// </summary>
internal abstract class AbstractTestFixture
{
	[Fact]
	public void ShouldNotBeDiscovered()
	{
		Assert.Fail("Abstract class tests should not run");
	}
}

/// <summary>
/// Class without any test attributes — should NOT be discovered.
/// </summary>
internal class NoTestsFixture
{
	public void NotATest()
	{
	}
}

/// <summary>
/// Fixture class with a constructor that throws — tests instance creation failure.
/// </summary>
internal class ThrowingConstructorFixture
{
	public ThrowingConstructorFixture()
	{
		throw new InvalidOperationException("Intentional constructor failure");
	}

	[Fact]
	public void ShouldNotRun()
	{
		Assert.Fail("This should never execute due to constructor failure");
	}
}

/// <summary>
/// Fixture class with mixed sync and async tests.
/// </summary>
internal class MixedSyncAsyncFixture
{
	[Fact]
	public void SyncPassingTest()
	{
		Assert.True(true);
	}

	[Fact]
	public async Task AsyncPassingTest()
	{
		await Task.Delay(1, TestContext.Current.CancellationToken);
		Assert.True(true);
	}

	[Fact]
	public void SyncFailingTest()
	{
		Assert.Fail("Intentional sync failure in mixed fixture");
	}
}
