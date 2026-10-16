using CommunityToolkit.Maui.Behaviors;
using static CommunityToolkit.Maui.DeviceTests.AssertHelpers;

namespace CommunityToolkit.Maui.DeviceTests;

// Device tests of UserStoppedTypingBehavior.ShouldDismissKeyboardAutomatically: a focused Entry in the window gets text that its platform
// view enters, and the user then stops typing. Dismissing the keyboard removes the focus on every platform; on Android it also hides the
// soft keyboard (see the .android.cs partial). The test base is Behaviors/KeyboardDismissalTestBase.cs.
[Category(TestCategory.UserStoppedTypingBehavior)]
[Collection(TestCollections.DisableParallelization)]
public partial class UserStoppedTypingBehaviorTests : KeyboardDismissalTestBase
{
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

	// The Command runs once the user stopped typing (100 ms), after the behavior dismissed the keyboard.
	static (Entry Entry, Task<string> CommandExecuted) CreateEntryWithBehavior(bool shouldDismissKeyboardAutomatically)
	{
		var commandTCS = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
		var entry = new Entry
		{
			WidthRequest = 200,
			Behaviors =
			{
				new UserStoppedTypingBehavior
				{
					StoppedTypingTimeThreshold = 100,
					ShouldDismissKeyboardAutomatically = shouldDismissKeyboardAutomatically,
					Command = new Command<string>(text => commandTCS.TrySetResult(text))
				}
			}
		};

		return (entry, commandTCS.Task);
	}
}