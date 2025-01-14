using System.Windows.Input;
using CommunityToolkit.Maui.Behaviors;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class MaxLengthReachedBehaviorTests() : BaseBehaviorTest<MaxLengthReachedBehavior, InputView>(new MaxLengthReachedBehavior(), new Entry())
{
	[Fact]
	[Obsolete]
	public void ShouldExecuteCommandWhenMaxLengthHasBeenReached()
	{
		// arrange
		var commandHasBeenExecuted = false;
		var entry = CreateEntry(command: new Command<string>((s) => commandHasBeenExecuted = true));

		// act
		entry.Text = "1";
		entry.Text += "2";

		// assert
		Assert.True(commandHasBeenExecuted);
	}

	[Fact]
	[Obsolete]
	public void ShouldInvokeEventHandlerWhenMaxLengthHasBeenReached()
	{
		// arrange
		var eventHandlerHasBeenInvoked = false;
		var entry = CreateEntry(eventHandler: (sender, text) => eventHandlerHasBeenInvoked = true);

		// act
		entry.Text = "1";
		entry.Text += "2";

		// assert
		Assert.True(eventHandlerHasBeenInvoked);
	}

	[Fact]
	[Obsolete]
	public void ShouldExecuteCommandWithTextValueNoLargerThenMaxLength()
	{
		// arrange
		var expectedLength = 6;
		var actualLength = int.MaxValue;
		var entry = CreateEntry(maxLength: 6,
								command: new Command<string>((text) => actualLength = text.Length));

		// act
		entry.Text = "123456789";

		// assert
		Assert.Equal(expectedLength, actualLength);
	}

	[Fact]
	[Obsolete]
	public void ShouldInvokeEventHandlerWithTextValueNoLargerThenMaxLength()
	{
		// arrange
		var expectedLength = 6;
		var actualLength = int.MaxValue;
		var entry = CreateEntry(maxLength: 6,
								eventHandler: (sender, text) => actualLength = text.Text.Length);

		// act
		entry.Text = "123456789";

		// assert
		Assert.Equal(expectedLength, actualLength);
	}

	[Fact]
	[Obsolete]
	public void ShouldNotExecuteCommandBeforeMaxLengthHasBeenReached()
	{
		// arrange
		var commandHasBeenExecuted = false;
		var entry = CreateEntry(command: new Command<string>((s) => commandHasBeenExecuted = true));

		// act
		entry.Text = "1";

		// assert
		Assert.False(commandHasBeenExecuted);
	}

	[Fact]
	[Obsolete]
	public void ShouldNotInvokeEventHandlerBeforeMaxLengthHasBeenReached()
	{
		// arrange
		var eventHandlerHasBeenInvoked = false;
		var entry = CreateEntry(eventHandler: (sender, text) => eventHandlerHasBeenInvoked = true);

		// act
		entry.Text = "1";

		// assert
		Assert.False(eventHandlerHasBeenInvoked);
	}

	[Fact]
	[Obsolete]
	public void ShouldDismissKeyboardWhenMaxLengthHasBeenReached()
	{
		// arrange
		var entry = CreateEntry(shouldDismissKeyboardAutomatically: true);

		// act
		entry.Focus();
		entry.Text = "1";
		entry.Text += "2";

		// assert
		Assert.False(entry.IsFocused);
	}

	[Fact]
	[Obsolete]
	public void ShouldNotDismissKeyboardBeforeMaxLengthHasBeenReached()
	{
		// arrange
		var entry = CreateEntry(shouldDismissKeyboardAutomatically: true);

		// act
		entry.Focus();
		entry.Text = "1";

		// assert
		Assert.True(entry.IsFocused);
	}

	[Fact]
	[Obsolete]
	public void ShouldNotDismissKeyboardWhenOptionSetToFalse()
	{
		// arrange
		var entry = CreateEntry();

		// act
		entry.Focus();
		entry.Text = "1";
		entry.Text += "1";

		// assert
		Assert.True(entry.IsFocused);
	}

	[Obsolete]
	static Entry CreateEntry(int? maxLength = 2,
							  bool shouldDismissKeyboardAutomatically = false,
							  ICommand? command = null,
							  EventHandler<MaxLengthReachedEventArgs>? eventHandler = null)
	{
		var behavior = new MaxLengthReachedBehavior
		{
			ShouldDismissKeyboardAutomatically = shouldDismissKeyboardAutomatically,
			Command = command
		};

		if (eventHandler != null)
		{
			behavior.MaxLengthReached += eventHandler;
		}

		var entry = new Entry
		{
			MaxLength = maxLength ?? int.MaxValue,
			Behaviors =
			{
				behavior
			}
		};

		// We simulate Focus/Unfocus behavior ourselves
		// because unit tests doesn't have "platform-specific" part
		// where IsFocused is controlled in the real app
		entry.FocusChangeRequested += (s, e) => entry.SetValue(VisualElement.IsFocusedPropertyKey, e.Focus);

		return entry;
	}
}