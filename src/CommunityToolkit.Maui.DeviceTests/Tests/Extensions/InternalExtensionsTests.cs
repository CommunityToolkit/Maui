using CommunityToolkit.Maui.Extensions;
using Xunit;

namespace CommunityToolkit.Maui.DeviceTests.Tests.Extensions;

public class CryptographyExtensionsTests
{
	[Fact]
	public void GetMd5Hash_ReturnsConsistentHash()
	{
		var result1 = "hello world".GetMd5Hash("-");
		var result2 = "hello world".GetMd5Hash("-");

		Assert.NotNull(result1);
		Assert.NotNull(result2);
		Assert.Equal(result1, result2);
	}

	[Fact]
	public void GetMd5Hash_DifferentInputs_DifferentHashes()
	{
		var result1 = "hello".GetMd5Hash("-");
		var result2 = "world".GetMd5Hash("-");

		Assert.NotNull(result1);
		Assert.NotNull(result2);
		Assert.NotEqual(result1, result2);
	}

	[Fact]
	public void GetMd5Hash_DefaultSeparator_UsesDash()
	{
		var hash = "test".GetMd5Hash();

		Assert.NotNull(hash);
		Assert.Contains("-", hash);
	}

	[Fact]
	public void GetMd5Hash_CustomSeparator_UsesCustom()
	{
		var hash = "test".GetMd5Hash(":");

		Assert.NotNull(hash);
		Assert.Contains(":", hash);
		Assert.DoesNotContain("-", hash);
	}

	[Fact]
	public void GetMd5Hash_EmptySeparator_NoSeparator()
	{
		var hash = "test".GetMd5Hash("");

		Assert.NotNull(hash);
		Assert.DoesNotContain("-", hash);
		Assert.DoesNotContain(":", hash);
	}

	[Fact]
	public void GetMd5Hash_ReturnsHexString()
	{
		var hash = "test".GetMd5Hash("");

		Assert.NotNull(hash);

		// MD5 produces 16 bytes = 32 hex characters
		Assert.Equal(32, hash.Length);
		Assert.Matches("^[0-9a-fA-F]+$", hash);
	}
}

public class WeakReferenceExtensionsTests
{
	[Fact]
	public void GetTargetOrDefault_AliveReference_ReturnsTarget()
	{
		var target = new object();
		var weakRef = new WeakReference<object>(target);

		var result = weakRef.GetTargetOrDefault();

		Assert.Same(target, result);
	}

	[Fact]
	public void GetTargetOrDefault_StringReference_ReturnsTarget()
	{
		var target = "hello world";
		var weakRef = new WeakReference<string>(target);

		var result = weakRef.GetTargetOrDefault();

		Assert.Equal("hello world", result);
	}

	[Fact]
	public async Task GetTargetOrDefault_CollectedReference_ReturnsNull()
	{
		// The target is created on another thread so that no stack slot of this test can keep it alive
		var weakRef = await Task.Run(CreateWeakReference);

		for (var attempt = 0; attempt < 10 && weakRef.TryGetTarget(out _); attempt++)
		{
			await Task.Yield();
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}

		Assert.False(weakRef.TryGetTarget(out _), "The target was not collected");
		Assert.Null(weakRef.GetTargetOrDefault());
	}

	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
	static WeakReference<object> CreateWeakReference() => new(new object());
}

public class SafeFireAndForgetExtensionsTests
{
	// Upper bound for the onException callback; the test fails with a TimeoutException if it is never invoked
	static readonly TimeSpan callbackTimeout = TimeSpan.FromSeconds(5);

	[Fact]
	public async Task SafeFireAndForget_CompletedTask_DoesNotThrow()
	{
		var task = Task.CompletedTask;

		// Should not throw
		task.SafeFireAndForget();

		// Give the fire-and-forget a moment to complete
		await Task.Delay(50);
	}

	[Fact]
	public async Task SafeFireAndForget_FaultedTask_CallsOnException()
	{
		var tcs = new TaskCompletionSource<Exception>();

		// Use Task.FromException to create an already-faulted task without throwing
		var faultedTask = Task.FromException(new InvalidOperationException("test"));

		Action<Exception> onException = ex => tcs.TrySetResult(ex);
		bool continueOnCapturedContext = false;

		faultedTask.SafeFireAndForget(in onException, in continueOnCapturedContext);

		Assert.IsType<InvalidOperationException>(await tcs.Task.WaitAsync(callbackTimeout));
	}

	[Fact]
	public async Task SafeFireAndForget_ValueTask_DoesNotThrow()
	{
		var task = new ValueTask(Task.CompletedTask);

		// Should not throw
		task.SafeFireAndForget();

		await Task.Delay(50);
	}

	[Fact]
	public async Task SafeFireAndForget_FaultedValueTask_CallsOnException()
	{
		var tcs = new TaskCompletionSource<Exception>();
		var faultedTask = new ValueTask(Task.FromException(new InvalidOperationException("test")));

		Action<Exception> onException = ex => tcs.TrySetResult(ex);
		bool continueOnCapturedContext = false;

		faultedTask.SafeFireAndForget(in onException, in continueOnCapturedContext);

		Assert.IsType<InvalidOperationException>(await tcs.Task.WaitAsync(callbackTimeout));
	}
}
