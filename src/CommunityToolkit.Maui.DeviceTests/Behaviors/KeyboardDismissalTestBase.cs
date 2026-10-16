using static CommunityToolkit.Maui.DeviceTests.AssertHelpers;

namespace CommunityToolkit.Maui.DeviceTests;

// Base class of the device tests of ShouldDismissKeyboardAutomatically, which MaxLengthReachedBehavior and UserStoppedTypingBehavior share.
// The tests host an Entry in the window and enter its text through the platform view, as typing does. The platform partials write the
// text of the platform view and read whether it has the focus; the Android partial also shows the soft keyboard and reads whether it shows.
public abstract partial class KeyboardDismissalTestBase : CommunityToolkitHandlerTestBase
{
	protected static async Task FocusAsync(InputView input)
	{
		Assert.True(input.Focus(), "The view did not accept the focus.");
		await AssertEventually(() => input.IsFocused, timeout: 3000, message: "The view never got the focus.");
	}
}