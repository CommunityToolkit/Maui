using FluentAssertions;
using Xunit;
using ToolkitDispatcherExtensions = CommunityToolkit.Maui.Core.Extensions.DispatcherExtensions;

namespace CommunityToolkit.Maui.UnitTests.Extensions;

public class DispatcherExtensionsTests : BaseTest
{
	const string unableToQueueActionExceptionMessage = "The dispatcher was unable to queue the requested action.";

	[Fact]
	public void DispatchIfRequired_NullDispatcher_ThrowsArgumentNullException()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => ToolkitDispatcherExtensions.DispatchIfRequired(null, () => { }));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		exception.ParamName.Should().Be("dispatcher");
	}

	[Fact]
	public void DispatchIfRequired_NullAction_ThrowsArgumentNullException()
	{
		var dispatcher = new ConfigurableMockDispatcher(isDispatchRequired: false);

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => ToolkitDispatcherExtensions.DispatchIfRequired(dispatcher, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		exception.ParamName.Should().Be("action");
	}

	[Fact]
	public void DispatchIfRequired_DispatchNotRequired_ExecutesActionDirectly()
	{
		var dispatcher = new ConfigurableMockDispatcher(isDispatchRequired: false);
		var wasActionExecuted = false;

		ToolkitDispatcherExtensions.DispatchIfRequired(dispatcher, () => wasActionExecuted = true);

		wasActionExecuted.Should().BeTrue();
		dispatcher.DispatchCount.Should().Be(0);
	}

	[Fact]
	public void DispatchIfRequired_DispatchRequired_ExecutesActionUsingDispatcher()
	{
		var dispatcher = new ConfigurableMockDispatcher(isDispatchRequired: true);
		var wasActionExecuted = false;

		ToolkitDispatcherExtensions.DispatchIfRequired(dispatcher, () => wasActionExecuted = true);

		wasActionExecuted.Should().BeTrue();
		dispatcher.DispatchCount.Should().Be(1);
	}

	[Fact]
	public void DispatchIfRequired_DispatchNotRequired_ActionThrows_ExceptionPropagates()
	{
		var dispatcher = new ConfigurableMockDispatcher(isDispatchRequired: false);
		var expectedException = new InvalidDataException("Expected Exception");

		var actualException = Assert.Throws<InvalidDataException>(() => ToolkitDispatcherExtensions.DispatchIfRequired(dispatcher, () => throw expectedException));

		actualException.Should().BeSameAs(expectedException);
		dispatcher.DispatchCount.Should().Be(0);
	}

	[Fact]
	public void DispatchIfRequired_DispatchFails_ThrowsInvalidOperationException()
	{
		var dispatcher = new ConfigurableMockDispatcher(isDispatchRequired: true, canQueueActions: false);
		var wasActionExecuted = false;

		var exception = Assert.Throws<InvalidOperationException>(() => ToolkitDispatcherExtensions.DispatchIfRequired(dispatcher, () => wasActionExecuted = true));

		exception.Message.Should().Be(unableToQueueActionExceptionMessage);
		wasActionExecuted.Should().BeFalse();
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task DispatchIfRequiredAsync_NullDispatcher_ThrowsArgumentNullException()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => ToolkitDispatcherExtensions.DispatchIfRequiredAsync(null, () => { }, TestContext.Current.CancellationToken));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		exception.ParamName.Should().Be("dispatcher");
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task DispatchIfRequiredAsync_NullAction_ThrowsArgumentNullException()
	{
		var dispatcher = new ConfigurableMockDispatcher(isDispatchRequired: false);

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => ToolkitDispatcherExtensions.DispatchIfRequiredAsync(dispatcher, null, TestContext.Current.CancellationToken));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		exception.ParamName.Should().Be("action");
	}

	[Theory(Timeout = (int)TestDuration.Short)]
	[InlineData(true)]
	[InlineData(false)]
	public async Task DispatchIfRequiredAsync_TokenAlreadyCancelled_ThrowsOperationCanceledException(bool isDispatchRequired)
	{
		var dispatcher = new ConfigurableMockDispatcher(isDispatchRequired);
		var wasActionExecuted = false;
		using var cts = new CancellationTokenSource();
		await cts.CancelAsync();

		await Assert.ThrowsAsync<OperationCanceledException>(() => ToolkitDispatcherExtensions.DispatchIfRequiredAsync(dispatcher, () => wasActionExecuted = true, cts.Token));

		wasActionExecuted.Should().BeFalse();
		dispatcher.DispatchCount.Should().Be(0);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task DispatchIfRequiredAsync_DispatchNotRequired_ExecutesActionDirectly()
	{
		var dispatcher = new ConfigurableMockDispatcher(isDispatchRequired: false);
		var wasActionExecuted = false;

		await ToolkitDispatcherExtensions.DispatchIfRequiredAsync(dispatcher, () => wasActionExecuted = true, TestContext.Current.CancellationToken);

		wasActionExecuted.Should().BeTrue();
		dispatcher.DispatchCount.Should().Be(0);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task DispatchIfRequiredAsync_DispatchNotRequired_ActionThrows_ExceptionPropagates()
	{
		var dispatcher = new ConfigurableMockDispatcher(isDispatchRequired: false);
		var expectedException = new InvalidDataException("Expected Exception");

		var actualException = await Assert.ThrowsAsync<InvalidDataException>(() => ToolkitDispatcherExtensions.DispatchIfRequiredAsync(dispatcher, () => throw expectedException, TestContext.Current.CancellationToken));

		actualException.Should().BeSameAs(expectedException);
		dispatcher.DispatchCount.Should().Be(0);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task DispatchIfRequiredAsync_DispatchRequired_ExecutesActionUsingDispatcher()
	{
		var dispatcher = new ConfigurableMockDispatcher(isDispatchRequired: true);
		var wasActionExecuted = false;

		await ToolkitDispatcherExtensions.DispatchIfRequiredAsync(dispatcher, () => wasActionExecuted = true, TestContext.Current.CancellationToken);

		wasActionExecuted.Should().BeTrue();
		dispatcher.DispatchCount.Should().Be(1);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task DispatchIfRequiredAsync_DispatchRequired_ActionThrows_TaskFaultsWithOriginalException()
	{
		var dispatcher = new ConfigurableMockDispatcher(isDispatchRequired: true);
		var expectedException = new InvalidDataException("Expected Exception");

		var actualException = await Assert.ThrowsAsync<InvalidDataException>(() => ToolkitDispatcherExtensions.DispatchIfRequiredAsync(dispatcher, () => throw expectedException, TestContext.Current.CancellationToken));

		actualException.Should().BeSameAs(expectedException);
		dispatcher.DispatchCount.Should().Be(1);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task DispatchIfRequiredAsync_DispatchFails_ThrowsInvalidOperationException()
	{
		var dispatcher = new ConfigurableMockDispatcher(isDispatchRequired: true, canQueueActions: false);
		var wasActionExecuted = false;

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => ToolkitDispatcherExtensions.DispatchIfRequiredAsync(dispatcher, () => wasActionExecuted = true, TestContext.Current.CancellationToken));

		exception.Message.Should().Be(unableToQueueActionExceptionMessage);
		wasActionExecuted.Should().BeFalse();
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task DispatchIfRequiredAsync_DispatchRequired_DoesNotCompleteUntilQueuedActionExecutes()
	{
		var dispatcher = new ConfigurableMockDispatcher(isDispatchRequired: true, executeDispatchedActionsImmediately: false);
		var wasActionExecuted = false;

		var dispatchTask = ToolkitDispatcherExtensions.DispatchIfRequiredAsync(dispatcher, () => wasActionExecuted = true, TestContext.Current.CancellationToken);

		dispatchTask.IsCompleted.Should().BeFalse();
		wasActionExecuted.Should().BeFalse();

		dispatcher.ExecuteQueuedActions();
		await dispatchTask;

		wasActionExecuted.Should().BeTrue();
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task DispatchIfRequiredAsync_TokenCancelledBeforeQueuedActionExecutes_ThrowsOperationCanceledException()
	{
		var dispatcher = new ConfigurableMockDispatcher(isDispatchRequired: true, executeDispatchedActionsImmediately: false);
		var wasActionExecuted = false;
		using var cts = new CancellationTokenSource();

		var dispatchTask = ToolkitDispatcherExtensions.DispatchIfRequiredAsync(dispatcher, () => wasActionExecuted = true, cts.Token);

		await cts.CancelAsync();
		dispatcher.ExecuteQueuedActions();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => dispatchTask);
		wasActionExecuted.Should().BeFalse();
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task DispatchIfRequiredAsync_TokenCancelledWhileActionQueued_CancelsTaskWithoutExecutingAction()
	{
		var dispatcher = new ConfigurableMockDispatcher(isDispatchRequired: true, executeDispatchedActionsImmediately: false);
		var wasActionExecuted = false;
		using var cts = new CancellationTokenSource();

		var dispatchTask = ToolkitDispatcherExtensions.DispatchIfRequiredAsync(dispatcher, () => wasActionExecuted = true, cts.Token);

		await cts.CancelAsync();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => dispatchTask);
		wasActionExecuted.Should().BeFalse();
		dispatcher.DispatchCount.Should().Be(1);
	}

	sealed class ConfigurableMockDispatcher(bool isDispatchRequired, bool canQueueActions = true, bool executeDispatchedActionsImmediately = true) : IDispatcher
	{
		readonly List<Action> queuedActions = [];

		public bool IsDispatchRequired { get; } = isDispatchRequired;

		public int DispatchCount { get; private set; }

		public bool Dispatch(Action action)
		{
			DispatchCount++;

			if (!canQueueActions)
			{
				return false;
			}

			if (executeDispatchedActionsImmediately)
			{
				action();
			}
			else
			{
				queuedActions.Add(action);
			}

			return true;
		}

		public bool DispatchDelayed(TimeSpan delay, Action action) => throw new NotSupportedException();

		public IDispatcherTimer CreateTimer() => throw new NotSupportedException();

		public void ExecuteQueuedActions()
		{
			foreach (var queuedAction in queuedActions)
			{
				queuedAction();
			}

			queuedActions.Clear();
		}
	}
}
