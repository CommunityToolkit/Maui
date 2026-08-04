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
		var hash = "test".GetMd5Hash("-");

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
	public void GetTargetOrDefault_CollectedReference_ReturnsNull()
	{
		var weakRef = CreateCollectedWeakReference();

		var result = weakRef.GetTargetOrDefault();

		// GC collection is non-deterministic. Only assert null if the target was actually collected.
		if (!weakRef.TryGetTarget(out _))
		{
			Assert.Null(result);
		}
	}

	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
	static WeakReference<object> CreateCollectedWeakReference()
	{
		var target = new object();
		var weakRef = new WeakReference<object>(target);
		target = null;
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
		return weakRef;
	}
}

public class SafeFireAndForgetExtensionsTests
{
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

		// Give the fire-and-forget a moment to process
		await Task.Delay(100);

		Assert.True(tcs.Task.IsCompleted);
		Assert.IsType<InvalidOperationException>(await tcs.Task);
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

		await Task.Delay(100);

		Assert.True(tcs.Task.IsCompleted);
		Assert.IsType<InvalidOperationException>(await tcs.Task);
	}
}
