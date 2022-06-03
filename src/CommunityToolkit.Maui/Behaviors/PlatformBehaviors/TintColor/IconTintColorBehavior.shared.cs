using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// A behavior that allows you to tint an icon with a specified <see cref="Color"/>.
/// </summary>
[UnsupportedOSPlatform("Windows")]
public partial class IconTintColorBehavior
#if WINDOWS
 : PlatformBehavior<Image>
#endif
{
	/// <summary>
	/// Attached Bindable Property for the TintColor.
	/// </summary>

	public static readonly BindableProperty TintColorProperty =
		BindableProperty.Create(nameof(TintColor), typeof(Color), typeof(IconTintColorBehavior), default);

	/// <summary>
	/// 
	/// </summary>
	public Color TintColor
	{
		get => (Color)GetValue(TintColorProperty);
		set => SetValue(TintColorProperty, value);
	}
}
