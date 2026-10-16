using CommunityToolkit.Maui.Behaviors;
using static CommunityToolkit.Maui.DeviceTests.AssertHelpers;

namespace CommunityToolkit.Maui.DeviceTests;

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

	// The behavior dismisses the keyboard before it runs the Command, so the tests can assert the focus once the Command has run.
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