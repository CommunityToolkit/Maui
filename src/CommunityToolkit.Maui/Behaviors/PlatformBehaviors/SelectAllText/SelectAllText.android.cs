using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Widget;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// 
/// </summary>
public class SelectAllText : PlatformBehavior<InputView, EditText>
{
	/// <inheritdoc/>
	protected override void OnAttachedTo(InputView bindable, EditText platformView) => platformView.SetSelectAllOnFocus(true);

	/// <inheritdoc/>
	protected override void OnDetachedFrom(InputView bindable, EditText platformView) => platformView.SetSelectAllOnFocus(true);
}
