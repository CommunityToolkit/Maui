using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core.Platform;
using static CommunityToolkit.Maui.DeviceTests.AssertHelpers;
using AView = Android.Views.View;
using SearchView = AndroidX.AppCompat.Widget.SearchView;

namespace CommunityToolkit.Maui.DeviceTests;

// Clearing the focus of an EditText leaves its soft keyboard open, so the behavior also hides the soft keyboard.
public partial class UserStoppedTypingBehaviorTests
{
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task SoftKeyboardIsHiddenWhenTheUserStopsTypingOnlyWhenShouldDismissKeyboardAutomatically(bool shouldDismissKeyboardAutomatically)
	{
		SetupBuilder();
		SkipWhenTheSoftKeyboardCannotShow();
		await EnsureSoftKeyboardHiddenAsync();
		var (entry, commandExecuted) = CreateEntryWithBehavior(shouldDismissKeyboardAutomatically);

		await AttachAndRun(entry, async handler =>
		{
			await entry.OnLoadedAsync();
			var editText = (AView)handler.PlatformView!;
			var frame = await ShowSoftKeyboardAsync(entry, editText);

			SetPlatformText(editText, "abc");

			Assert.Equal("abc", await commandExecuted.WaitAsync(TimeSpan.FromSeconds(5)));
			await AssertSoftKeyboardIsDismissedAsync(frame, shouldDismissKeyboardAutomatically);
		});
	}

	// Android hides the soft keyboard of a window, not of a view, so a behavior that hid it after its Entry lost the focus would hide the keyboard
	// of the Entry the user moved to.
	[Fact]
	public async Task SoftKeyboardOfAnotherInputStaysWhenTheUserMovesToItBeforeTheyStopTyping()
	{
		SetupBuilder();
		SkipWhenTheSoftKeyboardCannotShow();
		await EnsureSoftKeyboardHiddenAsync();
		var (entry, commandExecuted) = CreateEntryWithBehavior(shouldDismissKeyboardAutomatically: true, thresholdThatOutlastsMovingTheFocus);
		var otherEntry = new Entry { WidthRequest = 200 };

		await AttachAndRun(new VerticalStackLayout { entry, otherEntry }, async _ =>
		{
			await entry.OnLoadedAsync();
			await otherEntry.OnLoadedAsync();
			var editText = GetPlatformView(entry);
			var frame = await ShowSoftKeyboardAsync(entry, editText);

			SetPlatformText(editText, "abc");
			Assert.True(await otherEntry.ShowKeyboardAsync(), "ShowKeyboardAsync of the other Entry returned false.");
			await AssertEventually(() => otherEntry.IsFocused, timeout: 3000, message: "The other Entry never got the focus.");
			Assert.False(commandExecuted.IsCompleted, "The behavior ran before the focus moved to the other Entry.");

			Assert.Equal("abc", await commandExecuted.WaitAsync(TimeSpan.FromSeconds(5)));
			Assert.True(otherEntry.IsFocused, "The other Entry lost the focus.");
			await AssertSoftKeyboardIsDismissedAsync(frame, isDismissed: false);
		});
	}

	// MAUI keeps the IsFocused of a SearchBar from the focus of its query editor, not of its SearchView, and the behavior only dismisses the keyboard
	// of an InputView that has the focus.
	[Fact]
	public async Task SoftKeyboardIsHiddenWhenTheUserStopsTypingInASearchBar()
	{
		SetupBuilder();
		SkipWhenTheSoftKeyboardCannotShow();
		await EnsureSoftKeyboardHiddenAsync();
		var commandTCS = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
		var searchBar = new SearchBar
		{
			WidthRequest = 200,
			Behaviors =
			{
				new UserStoppedTypingBehavior
				{
					StoppedTypingTimeThreshold = 100,
					ShouldDismissKeyboardAutomatically = true,
					Command = new Command<string>(text => commandTCS.TrySetResult(text))
				}
			}
		};

		await AttachAndRun(searchBar, async _ =>
		{
			await searchBar.OnLoadedAsync();
			var searchView = (SearchView)GetPlatformView(searchBar);
			var frame = await ShowSoftKeyboardAsync(searchBar, searchView);

			searchView.SetQuery("abc", false);

			Assert.Equal("abc", await commandTCS.Task.WaitAsync(TimeSpan.FromSeconds(5)));
			await AssertSoftKeyboardIsDismissedAsync(frame, isDismissed: true);
			Assert.False(searchBar.IsFocused, "The SearchBar kept the focus.");
		});
	}
}