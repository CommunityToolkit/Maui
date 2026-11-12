using Android.Widget;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class ValidationBehaviorTests
{
	static Color? GetPlatformTextColor(IPlatformViewHandler handler) => Color.FromInt(((EditText)handler.PlatformView!).CurrentTextColor);

	// Focusing an input asks for its soft keyboard, which neither clearing the focus nor ending the test reliably takes back.
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