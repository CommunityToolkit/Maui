using Microsoft.Maui.Controls;
using Android.Widget;

namespace CommunityToolkit.Maui.Behaviors;
public partial class SelectAllTextBehavior : BasePlatformBehavior<InputView, EditText>
{
	/// <inheritdoc />

	protected override void OnPlatformAttachedBehavior(InputView view)
	{
		NativeView?.SetSelectAllOnFocus(true);
	}
	/// <inheritdoc />

	protected override void OnPlatformDeattachedBehavior(InputView view)
	{
		NativeView?.SetSelectAllOnFocus(false);
	}
}
