using Android.Widget;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class ValidationBehaviorTests
{
	static partial void ReplaceSoftwareKeyboard(InputView input) => SoftKeyboardHelpers.KeepFocusFromShowingTheKeyboard(input);

	static Color? GetPlatformTextColor(IPlatformViewHandler handler) => Color.FromInt(((EditText)handler.PlatformView!).CurrentTextColor);

	// Safety net: a keyboard that something else still brought up must not outlive the test.
	protected override async ValueTask DisposeAsyncCore()
	{
		try
		{
			await base.DisposeAsyncCore();
		}
		finally
		{
			_ = await SoftKeyboardHelpers.HideAsync();
		}
	}
}