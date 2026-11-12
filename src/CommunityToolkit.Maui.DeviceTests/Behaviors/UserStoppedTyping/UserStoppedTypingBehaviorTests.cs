using CommunityToolkit.Maui.Behaviors;
using static CommunityToolkit.Maui.DeviceTests.AssertHelpers;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.UserStoppedTypingBehavior)]
[Collection(TestCollections.DisableParallelization)]
public partial class UserStoppedTypingBehaviorTests : KeyboardDismissalTestBase
{
	// Long enough for the test to move the focus to the other Entry before the behavior dismisses the keyboard, also on a slow device.
	const int thresholdThatOutlastsMovingTheFocus = 2000;

	[Fact]
	public async Task AnotherInputKeepsTheFocusWhenTheUserMovesToItBeforeTheyStopTyping()
	{
		SetupBuilder();
		var (entry, commandExecuted) = CreateEntryWithBehavior(shouldDismissKeyboardAutomatically: true, thresholdThatOutlastsMovingTheFocus);
		var otherEntry = new Entry { WidthRequest = 200 };

		await AttachAndRun(new VerticalStackLayout { entry, otherEntry }, async _ =>
		{
			await entry.OnLoadedAsync();
			await otherEntry.OnLoadedAsync();
			var otherPlatformView = GetPlatformView(otherEntry);
			await FocusAsync(entry);

			SetPlatformText(GetPlatformView(entry), "abc");
			await FocusAsync(otherEntry);
			Assert.False(commandExecuted.IsCompleted, "The behavior ran before the focus moved to the other Entry.");

			Assert.Equal("abc", await commandExecuted.WaitAsync(TimeSpan.FromSeconds(5)));
			Assert.True(otherEntry.IsFocused, "The other Entry lost the focus.");
			Assert.True(IsPlatformViewFocused(otherPlatformView), "The platform view of the other Entry lost the focus.");
		});
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task PlatformViewLosesTheFocusWhenTheUserStopsTypingOnlyWhenShouldDismissKeyboardAutomatically(bool shouldDismissKeyboardAutomatically)
	{
		SetupBuilder();
		var (entry, commandExecuted) = CreateEntryWithBehavior(shouldDismissKeyboardAutomatically);

		await AttachAndRun(entry, async handler =>
		{
			await entry.OnLoadedAsync();
			var platformView = handler.PlatformView!;
			await FocusAsync(entry);
			Assert.True(IsPlatformViewFocused(platformView), "The platform view does not have the focus.");

			SetPlatformText(platformView, "abc");

			Assert.Equal("abc", await commandExecuted.WaitAsync(TimeSpan.FromSeconds(5)));
			if (shouldDismissKeyboardAutomatically)
			{
				await AssertEventually(() => !entry.IsFocused && !IsPlatformViewFocused(platformView), timeout: 2000, message: "The Entry kept the focus.");
			}
			else
			{
				Assert.True(entry.IsFocused, "The Entry lost the focus.");
				Assert.True(IsPlatformViewFocused(platformView), "The platform view lost the focus.");
			}
		});
	}

	// The behavior dismisses the keyboard before it runs the Command, so the tests can assert the focus once the Command has run.
	static (Entry Entry, Task<string> CommandExecuted) CreateEntryWithBehavior(bool shouldDismissKeyboardAutomatically, int stoppedTypingTimeThreshold = 100)
	{
		var commandTCS = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
		var entry = new Entry
		{
			WidthRequest = 200,
			Behaviors =
			{
				new UserStoppedTypingBehavior
				{
					StoppedTypingTimeThreshold = stoppedTypingTimeThreshold,
					ShouldDismissKeyboardAutomatically = shouldDismissKeyboardAutomatically,
					Command = new Command<string>(text => commandTCS.TrySetResult(text))
				}
			}
		};

		return (entry, commandTCS.Task);
	}
}