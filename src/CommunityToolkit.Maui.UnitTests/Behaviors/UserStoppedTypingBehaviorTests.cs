using System.Windows.Input;
using CommunityToolkit.Maui.Behaviors;
using Nito.AsyncEx;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class UserStoppedTypingBehaviorTests() : BaseBehaviorTest<UserStoppedTypingBehavior, InputView>(new UserStoppedTypingBehavior(), new Entry())
{
	const int textChangesStoppedTypingTimeThreshold = 100;

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
		await commandTCS.Task.WaitAsync(TestContext.Current.CancellationToken);

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
		await commandTCS.Task.WaitAsync(TestContext.Current.CancellationToken);

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
		await commandTCS.Task.WaitAsync(TestContext.Current.CancellationToken);

		Assert.True(commandHasBeenExecuted);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShouldOnlyExecuteCommandOnceWhenTextChangedHasOccurredMultipleTimes()
	{
		// arrange
		var commandTCS = new TaskCompletionSource();
		var timesExecuted = 0;
		var entry = CreateEntryWithBehavior(
			minimumLengthThreshold: 4,
			command: new Command<string>(_ =>
		{
			timesExecuted++;
			commandTCS.SetResult();
		}));

		// act
		entry.Text = "1";
		entry.Text = "12";
		entry.Text = "123";
		entry.Text = "1234";
		await commandTCS.Task.WaitAsync(TestContext.Current.CancellationToken);

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

		await unfocusTCS.Task.WaitAsync(TestContext.Current.CancellationToken);

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
		await commandTCS.Task.WaitAsync(TestContext.Current.CancellationToken);

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
		await focusTCS.Task.WaitAsync(TestContext.Current.CancellationToken);

		// assert
		Assert.False(commandHasBeenExecuted);

		// act
		entry.Text = "123";
		await commandTCS.Task.WaitAsync(TestContext.Current.CancellationToken);

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
		await focusTCS.Task.WaitAsync(TestContext.Current.CancellationToken);

		// assert
		Assert.True(entry.IsFocused);

		// act
		entry.Text = "123";
		await unfocusTCS.Task.WaitAsync(TestContext.Current.CancellationToken);

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
		await commandTCS.Task.WaitAsync(TestContext.Current.CancellationToken);

		// assert
		Assert.True(commandHasBeenExecuted);
	}

	[Theory]
	[InlineData(-1)]
	[InlineData(-2)]
	[InlineData(-5)]
	[InlineData(int.MinValue)]
	public void ShouldKeepPreviousStoppedTypingTimeThresholdWhenNegativeValueIsSet(int negativeThreshold)
	{
		var behavior = new UserStoppedTypingBehavior
		{
			StoppedTypingTimeThreshold = 250
		};

		behavior.StoppedTypingTimeThreshold = negativeThreshold;

		// .NET MAUI ignores a value rejected by the validation of the property (logging a warning) instead of throwing.
		Assert.Equal(250, behavior.StoppedTypingTimeThreshold);
	}

	[Fact]
	public void ShouldKeepPreviousStoppedTypingTimeThresholdWhenBindingProvidesNegativeValue()
	{
		// The threshold is bound to the text of an Entry, as in the Sample app.
		var thresholdEntry = new Entry
		{
			Text = "250"
		};
		var behavior = new UserStoppedTypingBehavior();
		behavior.SetBinding(UserStoppedTypingBehavior.StoppedTypingTimeThresholdProperty, new Binding(nameof(Entry.Text), source: thresholdEntry));
		Assert.Equal(250, behavior.StoppedTypingTimeThreshold);

		thresholdEntry.Text = "-5";

		Assert.Equal(250, behavior.StoppedTypingTimeThreshold);
	}

	[Theory(Timeout = (int)TestDuration.Medium)]
	[InlineData(-1)]
	[InlineData(-5)]
	public async Task ShouldExecuteCommandAfterPreviousStoppedTypingTimeThresholdWhenNegativeValueIsSet(int negativeThreshold)
	{
		var executedText = await RunOnSingleThreadedSynchronizationContext(async () =>
		{
			var commandTCS = new TaskCompletionSource<string>();
			var entry = CreateEntryWithBehavior(stoppedTypingTimeThreshold: 100, command: new Command<string>(commandTCS.SetResult));
			var behavior = entry.Behaviors.OfType<UserStoppedTypingBehavior>().Single();

			behavior.StoppedTypingTimeThreshold = negativeThreshold;
			entry.Text = "1";

			return await commandTCS.Task.WaitAsync(TestContext.Current.CancellationToken);
		}, TestContext.Current.CancellationToken);

		Assert.Equal("1", executedText);
	}

	[Fact(Timeout = (int)TestDuration.Medium)]
	public async Task ShouldExecuteCommandOnceWithLatestTextWhenTextChangesSeveralTimesInOneMessageOfTheUIThread()
	{
		var executedTexts = await GetCommandExecutionsAfterTextChanges(entry =>
		{
			entry.Text = "a";
			entry.Text = "ab";
			entry.Text = "abc";
		}, TestContext.Current.CancellationToken);

		var executedText = Assert.Single(executedTexts);
		Assert.Equal("abc", executedText);
	}

	[Fact(Timeout = (int)TestDuration.Medium)]
	public async Task ShouldExecuteCommandOnceWithLatestTextWhenKeystrokesAreQueuedOnTheUIThread()
	{
		const string typedText = "abcdefghijklmnopqrstuvwxyz0123";

		// Every keystroke is its own message of the UI thread, and all of them are queued before the behavior resumes, as in a typing burst.
		var executedTexts = await GetCommandExecutionsAfterTextChanges(entry =>
		{
			var uiThread = SynchronizationContext.Current ?? throw new InvalidOperationException("The Text changes must run on a SynchronizationContext");
			for (var length = 1; length <= typedText.Length; length++)
			{
				var text = typedText[..length];
				uiThread.Post(_ => entry.Text = text, null);
			}
		}, TestContext.Current.CancellationToken);

		var executedText = Assert.Single(executedTexts);
		Assert.Equal(typedText, executedText);
	}

	[Fact(Timeout = (int)TestDuration.Medium)]
	public async Task ShouldExecuteCommandOnceWithLatestTextWhenTextChangesWhileTheElapsedWaitIsQueuedOnTheUIThread()
	{
		// The UI thread is busy until the wait for "a" has elapsed, so the change to "ab" is handled before the continuation of that wait.
		var executedTexts = await GetCommandExecutionsAfterTextChanges(entry =>
		{
			entry.Text = "a";
			Thread.Sleep(5 * textChangesStoppedTypingTimeThreshold);
			entry.Text = "ab";
		}, TestContext.Current.CancellationToken);

		var executedText = Assert.Single(executedTexts);
		Assert.Equal("ab", executedText);
	}

	// Waits five thresholds after the first execution, so that an outdated execution is recorded as well as the one with the latest text.
	static Task<List<string>> GetCommandExecutionsAfterTextChanges(Action<Entry> changeText, CancellationToken token) => RunOnSingleThreadedSynchronizationContext(async () =>
	{
		var executedTexts = new List<string>();
		var firstExecutionTCS = new TaskCompletionSource();
		var entry = CreateEntryWithBehavior(textChangesStoppedTypingTimeThreshold, command: new Command<string>(text =>
		{
			executedTexts.Add(text);
			firstExecutionTCS.TrySetResult();
		}));

		changeText(entry);

		await firstExecutionTCS.Task.WaitAsync(token);
		await Task.Delay(5 * textChangesStoppedTypingTimeThreshold, token);

		return executedTexts;
	}, token);

	// The continuations of the behavior come back to one thread, like on the UI thread of an app, and an exception thrown by the async void
	// handler of the behavior fails the test instead of terminating the test process.
	static Task<T> RunOnSingleThreadedSynchronizationContext<T>(Func<Task<T>> test, CancellationToken token) =>
		Task.Run(() => AsyncContext.Run(test), token);

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
		entry.Focused += (s, e) => entry.SetValue(VisualElement.IsFocusedPropertyKey, true);
		entry.Unfocused += (s, e) => entry.SetValue(VisualElement.IsFocusedPropertyKey, false);
		entry.FocusChangeRequested += (s, e) => entry.SetValue(VisualElement.IsFocusedPropertyKey, e.Focus);

		return entry;
	}
}