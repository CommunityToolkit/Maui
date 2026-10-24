using static CommunityToolkit.Maui.DeviceTests.AssertHelpers;

namespace CommunityToolkit.Maui.DeviceTests;

// The tests enter the text through the platform view, as typing does.
public abstract partial class KeyboardDismissalTestBase : CommunityToolkitHandlerTestBase
{
	protected static async Task FocusAsync(InputView input)
	{
		Assert.True(input.Focus(), "The view did not accept the focus.");
		await AssertEventually(() => input.IsFocused, timeout: 3000, message: "The view never got the focus.");
	}
}