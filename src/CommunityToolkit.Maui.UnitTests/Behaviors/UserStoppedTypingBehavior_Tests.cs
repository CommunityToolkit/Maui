﻿using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Maui.Behaviors;
using Microsoft.Maui.Controls;
using NUnit.Framework;

namespace CommunityToolkit.Maui.UnitTests.Behaviors
{
    public class UserStoppedTypingBehavior_Tests
	{
		const int defaultTimeThreshold = 1000;
		const int defaultLengthThreshold = 0;
		const int defaultTimeoutThreshold = defaultTimeThreshold * 2;

		[Test]
		public async Task ShouldExecuteCommandWhenTimeThresholdHasExpired()
		{
			// arrange
			var commandHasBeenExecuted = false;
			var entry = CreateEntryWithBehavior(command: new Command<string>(_ => commandHasBeenExecuted = true));

			// act
			entry.Text = "1";
			await Task.Delay(defaultTimeoutThreshold);

			// assert
			Assert.IsTrue(commandHasBeenExecuted);
		}

		[Test]
		public async Task ShouldExecuteCommandWithSpecificParameterWhenSpecified()
		{
			// arrange
			var commandHasBeenExecuted = false;
			var entry = CreateEntryWithBehavior(command: new Command<bool>(_ => commandHasBeenExecuted = true),
												commandParameter: true);

			// act
			entry.Text = "1";
			await Task.Delay(defaultTimeoutThreshold);

			// assert
			Assert.IsTrue(commandHasBeenExecuted);
		}

		[Test]
		public async Task ShouldNotExecuteCommandBeforeTimeThresholdHasExpired()
		{
			// arrange
			var commandHasBeenExecuted = false;
			var entry = CreateEntryWithBehavior(command: new Command<string>(_ => commandHasBeenExecuted = true));

			// act
			entry.Text = "1";
			await Task.Delay(10);

			// assert
			Assert.False(commandHasBeenExecuted);
		}

		[Test]
		public async Task ShouldOnlyExectueCommandOnceWhenTextChangedHasOccurredMultipleTimes()
		{
			// arrange
			var timesExecuted = 0;
			var entry = CreateEntryWithBehavior(command: new Command<string>(_ => timesExecuted++));

			// act
			entry.Text = "1";
			entry.Text = "12";
			entry.Text = "123";
			entry.Text = "1234";
			await Task.Delay(defaultTimeoutThreshold);

			// assert
			Assert.AreEqual(1, timesExecuted);
		}

		[Test]
		public async Task ShouldDismissKeyboardWhenTimeThresholdHasExpired()
		{
			// arrange
			var entry = CreateEntryWithBehavior(shouldDismissKeyboardAutomatically: true);

			// act
			entry.Focus();
			entry.Text = "1";

			await Task.Delay(defaultTimeoutThreshold);

			// assert
			Assert.False(entry.IsFocused);
		}

		[Test]
		public async Task ShouldExecuteCommandWhenMinimumLengthThreholdHasBeenReached()
		{
			// arrange
			var commandHasBeenExecuted = false;
			var entry = CreateEntryWithBehavior(command: new Command<string>(_ => commandHasBeenExecuted = true),
												lengthThreshold: 3);

			// act
			entry.Text = "1";
			entry.Text = "12";
			entry.Text = "123";
			await Task.Delay(defaultTimeoutThreshold);

			// assert
			Assert.IsTrue(commandHasBeenExecuted);
		}

		[Test]
		public async Task ShouldNotExecuteCommandWhenMinimumLengthThreholdHasNotBeenReached()
		{
			// arrange
			var commandHasBeenExecuted = false;
			var entry = CreateEntryWithBehavior(command: new Command<string>(_ => commandHasBeenExecuted = true),
												lengthThreshold: 2);

			// act
			entry.Text = "1";
			await Task.Delay(defaultTimeoutThreshold);

			// assert
			Assert.False(commandHasBeenExecuted);
		}

		[Test]
		public async Task ShouldNotDismissKeyboardWhenMinimumLengthThreholdHasNotBeenReached()
		{
			// arrange
			var entry = CreateEntryWithBehavior(lengthThreshold: 3,
												shouldDismissKeyboardAutomatically: true);

			// act
			entry.Focus();

			entry.Text = "1";
			await Task.Delay(defaultTimeoutThreshold);

			// assert
			Assert.IsTrue(entry.IsFocused);
		}

		[Test]
		public async Task ShouldExecuteCommandImmediatelyWhenMinimumLengthThreholdHasNotBeenSet()
		{
			// arrange
			var commandHasBeenExecuted = false;
			var entry = CreateEntryWithBehavior(command: new Command<string>(_ => commandHasBeenExecuted = true));

			// act
			entry.Text = "1";
			await Task.Delay(defaultTimeoutThreshold);

			// assert
			Assert.IsTrue(commandHasBeenExecuted);
		}

		public Entry CreateEntryWithBehavior(int timeThreshold = defaultTimeThreshold,
											 int lengthThreshold = defaultLengthThreshold,
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
						StoppedTypingTimeThreshold = timeThreshold,
						MinimumLengthThreshold = lengthThreshold,
						ShouldDismissKeyboardAutomatically = shouldDismissKeyboardAutomatically,
						Command = command,
						CommandParameter = commandParameter
					}
				}
			};

			// We simulate Focus/Unfocus behavior ourselves
			// because unit tests doesn't have "platform-specific" part
			// where IsFocused is controlled in the real app
			entry.FocusChangeRequested += (s, e) => entry.SetValue(VisualElement.IsFocusedPropertyKey, e.Focus);

			return entry;
		}
	}
}