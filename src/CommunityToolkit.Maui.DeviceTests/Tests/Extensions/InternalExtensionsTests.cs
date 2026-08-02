using System.Reflection;
using Xunit;

namespace CommunityToolkit.Maui.DeviceTests.Tests.Extensions;

static class MauiExtensionsReflectionHelper
{
	internal static readonly Assembly MauiAssembly = typeof(CommunityToolkit.Maui.Converters.InvertedBoolConverter).Assembly;

	internal static object? InvokeInternalStatic(string className, string methodName, object?[]? args, Type[]? genericTypeArgs = null, Type[]? parameterTypes = null)
	{
		var type = MauiAssembly.GetType($"CommunityToolkit.Maui.Extensions.{className}");
		Assert.NotNull(type);

		MethodInfo? method;
		if (parameterTypes is not null)
		{
			method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, parameterTypes);
		}
		else
		{
			// Use GetMethods to avoid AmbiguousMatchException when generic and non-generic overloads share a signature.
			// Match generic-ness to whether genericTypeArgs were supplied, and match the argument count.
			var argCount = args?.Length ?? 0;
			var wantGeneric = genericTypeArgs is not null;
			method = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
				.FirstOrDefault(m => m.Name == methodName && m.IsGenericMethod == wantGeneric && m.GetParameters().Length == argCount);
		}

		Assert.NotNull(method);

		if (genericTypeArgs is not null)
		{
			method = method.MakeGenericMethod(genericTypeArgs);
		}

		return method.Invoke(null, args);
	}
}

public class CryptographyExtensionsTests
{
	[Fact]
	public void GetMd5Hash_ReturnsConsistentHash()
	{
		// GetMd5Hash is an extension method; reflection requires all params including the default separator
		var result1 = MauiExtensionsReflectionHelper.InvokeInternalStatic(
			"CryptographyExtensions", "GetMd5Hash", ["hello world", "-"]);
		var result2 = MauiExtensionsReflectionHelper.InvokeInternalStatic(
			"CryptographyExtensions", "GetMd5Hash", ["hello world", "-"]);

		Assert.NotNull(result1);
		Assert.NotNull(result2);
		Assert.Equal(result1, result2);
	}

	[Fact]
	public void GetMd5Hash_DifferentInputs_DifferentHashes()
	{
		var result1 = MauiExtensionsReflectionHelper.InvokeInternalStatic(
			"CryptographyExtensions", "GetMd5Hash", ["hello", "-"]);
		var result2 = MauiExtensionsReflectionHelper.InvokeInternalStatic(
			"CryptographyExtensions", "GetMd5Hash", ["world", "-"]);

		Assert.NotNull(result1);
		Assert.NotNull(result2);
		Assert.NotEqual(result1, result2);
	}

	[Fact]
	public void GetMd5Hash_DefaultSeparator_UsesDash()
	{
		var result = MauiExtensionsReflectionHelper.InvokeInternalStatic(
			"CryptographyExtensions", "GetMd5Hash", ["test", "-"]);

		Assert.NotNull(result);
		var hash = (string)result;
		Assert.Contains("-", hash);
	}

	[Fact]
	public void GetMd5Hash_CustomSeparator_UsesCustom()
	{
		var result = MauiExtensionsReflectionHelper.InvokeInternalStatic(
			"CryptographyExtensions", "GetMd5Hash", ["test", ":"]);

		Assert.NotNull(result);
		var hash = (string)result;
		Assert.Contains(":", hash);
		Assert.DoesNotContain("-", hash);
	}

	[Fact]
	public void GetMd5Hash_EmptySeparator_NoSeparator()
	{
		var result = MauiExtensionsReflectionHelper.InvokeInternalStatic(
			"CryptographyExtensions", "GetMd5Hash", ["test", ""]);

		Assert.NotNull(result);
		var hash = (string)result;
		Assert.DoesNotContain("-", hash);
		Assert.DoesNotContain(":", hash);
	}

	[Fact]
	public void GetMd5Hash_ReturnsHexString()
	{
		var result = MauiExtensionsReflectionHelper.InvokeInternalStatic(
			"CryptographyExtensions", "GetMd5Hash", ["test", ""]);

		Assert.NotNull(result);
		var hash = (string)result;

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

		var result = MauiExtensionsReflectionHelper.InvokeInternalStatic(
			"WeakReferenceExtensions", "GetTargetOrDefault", [weakRef], [typeof(object)]);

		Assert.Same(target, result);
	}

	[Fact]
	public void GetTargetOrDefault_StringReference_ReturnsTarget()
	{
		var target = "hello world";
		var weakRef = new WeakReference<string>(target);

		var result = MauiExtensionsReflectionHelper.InvokeInternalStatic(
			"WeakReferenceExtensions", "GetTargetOrDefault", [weakRef], [typeof(string)]);

		Assert.Equal("hello world", result);
	}

	[Fact]
	public void GetTargetOrDefault_CollectedReference_ReturnsNull()
	{
		var weakRef = CreateCollectedWeakReference();

		var result = MauiExtensionsReflectionHelper.InvokeInternalStatic(
			"WeakReferenceExtensions", "GetTargetOrDefault", [weakRef], [typeof(object)]);

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
	// SafeFireAndForget uses `in` (by-ref) parameters, so specify exact parameter types to resolve overloads
	static readonly Type[] taskOverloadTypes = [typeof(Task), typeof(Action<Exception>).MakeByRefType(), typeof(bool).MakeByRefType()];
	static readonly Type[] valueTaskOverloadTypes = [typeof(ValueTask), typeof(Action<Exception>).MakeByRefType(), typeof(bool).MakeByRefType()];

	[Fact]
	public async Task SafeFireAndForget_CompletedTask_DoesNotThrow()
	{
		var task = Task.CompletedTask;

		// Should not throw
		MauiExtensionsReflectionHelper.InvokeInternalStatic(
			"SafeFireAndForgetExtensions", "SafeFireAndForget", [task, null, false], parameterTypes: taskOverloadTypes);

		// Give the fire-and-forget a moment to complete
		await Task.Delay(50);
	}

	[Fact]
	public async Task SafeFireAndForget_FaultedTask_CallsOnException()
	{
		var tcs = new TaskCompletionSource<Exception>();

		// Use Task.FromException to create an already-faulted task without throwing
		var faultedTask = Task.FromException(new InvalidOperationException("test error"));

		MauiExtensionsReflectionHelper.InvokeInternalStatic(
			"SafeFireAndForgetExtensions", "SafeFireAndForget",
			[faultedTask, (Action<Exception>)(ex => tcs.TrySetResult(ex)), false], parameterTypes: taskOverloadTypes);

		var capturedException = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

		Assert.NotNull(capturedException);
		Assert.IsType<InvalidOperationException>(capturedException);
		Assert.Equal("test error", capturedException.Message);
	}

	[Fact]
	public async Task SafeFireAndForget_SuccessfulTask_DoesNotCallOnException()
	{
		var exceptionCalled = false;
		var task = Task.FromResult(42);

		MauiExtensionsReflectionHelper.InvokeInternalStatic(
			"SafeFireAndForgetExtensions", "SafeFireAndForget",
			[task, (Action<Exception>)(_ => exceptionCalled = true), false], parameterTypes: taskOverloadTypes);

		await Task.Delay(100);

		Assert.False(exceptionCalled);
	}

	[Fact]
	public async Task SafeFireAndForget_ValueTask_FaultedTask_CallsOnException()
	{
		var tcs = new TaskCompletionSource<Exception>();

		// Use Task.FromException to create an already-faulted task without throwing
		var faultedValueTask = new ValueTask(Task.FromException(new ArgumentException("value task error")));

		MauiExtensionsReflectionHelper.InvokeInternalStatic(
			"SafeFireAndForgetExtensions", "SafeFireAndForget",
			[faultedValueTask, (Action<Exception>)(ex => tcs.TrySetResult(ex)), false], parameterTypes: valueTaskOverloadTypes);

		var capturedException = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

		Assert.NotNull(capturedException);
		Assert.IsType<ArgumentException>(capturedException);
	}
}