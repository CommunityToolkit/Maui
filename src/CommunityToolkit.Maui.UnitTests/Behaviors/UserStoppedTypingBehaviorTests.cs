using System.Windows.Input;
using CommunityToolkit.Maui.Behaviors;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class UserStoppedTypingBehaviorTests() : BaseBehaviorTest<UserStoppedTypingBehavior, InputView>(new UserStoppedTypingBehavior(), new Entry())
{
	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShouldExecuteCommandWhenTimeThresholdHasExpired()
	{
		// arrange
		var commandTCS = new TaskCompletionSource();
		var commandHasBeenExecuted = false;

		var entry = CreateEntryWithBehavior(command: new Command<string>(_ =>
		{
			commandHasBeenExecuted = true;
			commandTCS.SetResult();
		}));

		// act
		entry.Text = "1";
		await commandTCS.Task;

		// assert
		Assert.True(commandHasBeenExecuted);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShouldExecuteCommandWithSpecificParameterWhenSpecified()
	{
		// arrange
		var commandTCS = new TaskCompletionSource();
		var commandHasBeenExecuted = false;
		var entry = CreateEntryWithBehavior(command: new Command<bool>(_ =>
		{
			commandHasBeenExecuted = true;
			commandTCS.SetResult();
		}), commandParameter: true);

		// act
		entry.Text = "1";
		await commandTCS.Task;

		// assert
		Assert.True(commandHasBeenExecuted);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShouldNotExecuteCommandBeforeTimeThresholdHasExpired()
	{
		// arrange
		var commandTCS = new TaskCompletionSource();
		var commandHasBeenExecuted = false;
		var entry = CreateEntryWithBehavior(command: new Command<string>(_ =>
		{
			commandHasBeenExecuted = true;
			commandTCS.SetResult();
		}));

		// act
		entry.Text = "1";

		// assert
		Assert.False(commandHasBeenExecuted);

		// act
		await commandTCS.Task;

		Assert.True(commandHasBeenExecuted);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShouldOnlyExecuteCommandOnceWhenTextChangedHasOccurredMultipleTimes()
	{
		// arrange
		var commandTCS = new TaskCompletionSource();
		var timesExecuted = 0;
		var entry = CreateEntryWithBehavior(command: new Command<string>(_ =>
		{
			timesExecuted++;
			commandTCS.SetResult();
		}));

		// act
		entry.Text = "1";
		entry.Text = "12";
		entry.Text = "123";
		entry.Text = "1234";
		await commandTCS.Task;

		// assert
		Assert.Equal(1, timesExecuted);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShouldDismissKeyboardWhenTimeThresholdHasExpired()
	{
		// arrange
		var unfocusTCS = new TaskCompletionSource();
		var entry = CreateEntryWithBehavior(shouldDismissKeyboardAutomatically: true);
		entry.Unfocused += HandleEntryUnfocused;

		// act
		entry.Focus();
		entry.Text = "1";

		await unfocusTCS.Task;

		// assert
		Assert.False(entry.IsFocused);

		void HandleEntryUnfocused(object? sender, FocusEventArgs e)
		{
			entry.Unfocused -= HandleEntryUnfocused;
			unfocusTCS.SetResult();
		}
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShouldExecuteCommandWhenMinimumLengthThresholdHasBeenReached()
	{
		// arrange
		var commandTCS = new TaskCompletionSource();
		var commandHasBeenExecuted = false;
		var entry = CreateEntryWithBehavior(command: new Command<string>(_ =>
		{
			commandHasBeenExecuted = true;
			commandTCS.SetResult();
		}),
		minimumLengthThreshold: 3);

		// act
		entry.Text = "1";
		entry.Text = "12";
		entry.Text = "123";
		await commandTCS.Task;

		// assert
		Assert.True(commandHasBeenExecuted);
	}

	[Fact(Timeout = (int)TestDuration.Long)]
	public async Task ShouldNotExecuteCommandWhenMinimumLengthThresholdHasNotBeenReached()
	{
		// arrange
		var focusTCS = new TaskCompletionSource();
		var commandTCS = new TaskCompletionSource();
		var commandHasBeenExecuted = false;
		var entry = CreateEntryWithBehavior(command: new Command<string>(_ =>
		{
			commandHasBeenExecuted = true;
			commandTCS.SetResult();
		}),
		minimumLengthThreshold: 2);

		entry.Focused += HandleFocused;

		// act
		entry.Focus();
		entry.Text = "1";
		await focusTCS.Task;

		// assert
		Assert.False(commandHasBeenExecuted);

		// act
		entry.Text = "123";
		await commandTCS.Task;

		// assert
		Assert.True(commandHasBeenExecuted);

		void HandleFocused(object? sender, FocusEventArgs e)
		{
			entry.Focused -= HandleFocused;
			focusTCS.SetResult();
		}
	}

	[Fact(Timeout = (int)TestDuration.Long)]
	public async Task ShouldNotDismissKeyboardWhenMinimumLengthThresholdHasNotBeenReached()
	{
		// arrange
		var focusTCS = new TaskCompletionSource();
		var unfocusTCS = new TaskCompletionSource();
		var entry = CreateEntryWithBehavior(minimumLengthThreshold: 3,
			shouldDismissKeyboardAutomatically: true);
		entry.Focused += HandleFocused;
		entry.Unfocused += HandleUnfocused;

		// act
		entry.Focus();

		entry.Text = "1";
		await focusTCS.Task;

		// assert
		Assert.True(entry.IsFocused);

		// act
		entry.Text = "123";
		await unfocusTCS.Task;

		// assert
		Assert.False(entry.IsFocused);

		void HandleFocused(object? sender, FocusEventArgs e)
		{
			entry.Focused -= HandleFocused;
			focusTCS.SetResult();
		}

		void HandleUnfocused(object? sender, FocusEventArgs e)
		{
			entry.Unfocused -= HandleUnfocused;
			unfocusTCS.SetResult();
		}
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShouldExecuteCommandImmediatelyWhenMinimumLengthThresholdHasNotBeenSet()
	{
		// arrange
		var commandTCS = new TaskCompletionSource();
		var commandHasBeenExecuted = false;
		var entry = CreateEntryWithBehavior(command: new Command<string>(_ =>
		{
			commandHasBeenExecuted = true;
			commandTCS.SetResult();
		}));

		// act
		entry.Text = "1";
		await commandTCS.Task;

		// assert
		Assert.True(commandHasBeenExecuted);
	}

	static Entry CreateEntryWithBehavior(int stoppedTypingTimeThreshold = 500,
		int minimumLengthThreshold = 0,
		bool shouldDismissKeyboardAutomatically = false,
		ICommand? command = null,
		object? commandParameter = null)
	{
		var entry = new Entry
		{
			Behaviors =
			{
				new UserStoppedTypingBehavior
				{
					StoppedTypingTimeThreshold = stoppedTypingTimeThreshold,
					MinimumLengthThreshold = minimumLengthThreshold,
					ShouldDismissKeyboardAutomatically = shouldDismissKeyboardAutomatically,
					Command = command,
					CommandParameter = commandParameter
				}
			}
		};

		// We simulate Focus/Unfocus behavior ourselves
		// because unit tests doesn't have "platform-specific" part
		// where IsFocused is controlled in the real app
		entry.FocusChangeRequested += HandleEntryFocusChangeRequested;

		return entry;

		static void HandleEntryFocusChangeRequested(object? sender, VisualElement.FocusRequestArgs e)
		{
			ArgumentNullException.ThrowIfNull(sender);

			var entry = (Entry)sender;
			entry.SetValue(VisualElement.IsFocusedPropertyKey, e.Focus);
		}
	}
}