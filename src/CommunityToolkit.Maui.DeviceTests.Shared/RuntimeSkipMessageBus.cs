using System;
using System.Linq;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace CommunityToolkit.Maui.DeviceTests;

// Turns the TestFailed message of a test that threw SkipException into TestSkipped, the technique of Xunit.SkippableFact.
// DeviceTestCase moves the count from Failed to Skipped in the returned RunSummary, which feeds the assembly totals.
sealed class RuntimeSkipMessageBus(IMessageBus inner) : IMessageBus
{
	static readonly string skipExceptionTypeName = typeof(SkipException).FullName ?? nameof(SkipException);
	static readonly string aggregateExceptionTypeName = typeof(AggregateException).FullName ?? nameof(AggregateException);

	public int SkippedCount { get; private set; }

	public bool QueueMessage(IMessageSinkMessage message)
	{
		if (message is ITestFailed failed && TryGetSkipReason(failed, out var reason))
		{
			SkippedCount++;
			return inner.QueueMessage(new TestSkipped(failed.Test, reason));
		}

		return inner.QueueMessage(message);
	}

	// The wrapped bus belongs to the xUnit runner, which disposes it.
	public void Dispose()
	{
	}

	static bool TryGetSkipReason(ITestFailed failed, out string reason)
	{
		var exceptionTypes = failed.ExceptionTypes ?? [];
		var messages = failed.Messages ?? [];

		// A blocking wait on a task that threw SkipException wraps it in an AggregateException.
		var index = exceptionTypes.Length switch
		{
			> 0 when exceptionTypes[0] == skipExceptionTypeName => 0,
			2 when exceptionTypes[0] == aggregateExceptionTypeName && exceptionTypes[1] == skipExceptionTypeName => 1,
			_ => -1
		};

		reason = index >= 0 ? messages.ElementAtOrDefault(index) ?? string.Empty : string.Empty;
		return index >= 0;
	}
}