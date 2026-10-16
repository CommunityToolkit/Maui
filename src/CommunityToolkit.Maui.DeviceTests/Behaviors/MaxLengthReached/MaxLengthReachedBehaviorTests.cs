using CommunityToolkit.Maui.Behaviors;
using static CommunityToolkit.Maui.DeviceTests.AssertHelpers;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.MaxLengthReachedBehavior)]
[Collection(TestCollections.DisableParallelization)]
public partial class MaxLengthReachedBehaviorTests : KeyboardDismissalTestBase
{
	const int maxLength = 3;

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task PlatformViewLosesTheFocusWhenMaxLengthIsReachedOnlyWhenShouldDismissKeyboardAutomatically(bool shouldDismissKeyboardAutomatically)
	{
		SetupBuilder();
		var (entry, maxLengthReached) = CreateEntryWithBehavior(shouldDismissKeyboardAutomatically);

		await AttachAndRun(entry, async handler =>
		{
			await entry.OnLoadedAsync();
			var platformView = handler.PlatformView!;
			await FocusAsync(entry);
			Assert.True(IsPlatformViewFocused(platformView), "The platform view does not have the focus.");

			SetPlatformText(platformView, "abc");

			Assert.Equal("abc", await maxLengthReached.WaitAsync(TimeSpan.FromSeconds(2)));
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

	// The Command runs after the behavior dismissed the keyboard. A handler of the MaxLengthReached event would be held weakly, so a lambda
	// that nothing else references could be collected before the event.
	static (Entry Entry, Task<string> MaxLengthReached) CreateEntryWithBehavior(bool shouldDismissKeyboardAutomatically)
	{
		var maxLengthReachedTCS = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
		var entry = new Entry
		{
			MaxLength = maxLength,
			WidthRequest = 200,
			Behaviors =
			{
				new MaxLengthReachedBehavior
				{
					ShouldDismissKeyboardAutomatically = shouldDismissKeyboardAutomatically,
					Command = new Command<string>(text => maxLengthReachedTCS.TrySetResult(text))
				}
			}
		};

		return (entry, maxLengthReachedTCS.Task);
	}
}