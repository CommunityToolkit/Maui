using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// A behavior that allows you to tint an icon with a specified <see cref="Color"/>.
/// </summary>
public partial class IconTintColorBehavior
{
	/// <summary>
	/// Attached Bindable Property for the TintColor.
	/// </summary>
	public static readonly BindableProperty TintColorProperty =
		BindableProperty.CreateAttached("TintColor", typeof(Color), typeof(IconTintColorBehavior), default);
	/// <summary>
	/// Method that Gets the TintColor value.
	/// </summary>
	/// <param name="bindable">The <see cref="BindableObject"/> that holds the value for the <see cref="TintColorProperty"/> Bindable Property.</param>
	/// <returns>The <see cref="Color"/> that the icon will be painted.</returns>
	public static Color? GetTintColor(BindableObject bindable) =>
		(Color?)bindable.GetValue(TintColorProperty);

	/// <summary>
	/// Method that sets the <see cref="TintColorProperty"/> Bindable Property.
	/// </summary>
	/// <param name="bindable">The <see cref="BindableObject"/> that holds the value for the <see cref="TintColorProperty"/> Bindable Property.</param>
	/// <param name="value">The <see cref="Color"/> value that will applied to the icon.</param>
	public static void SetTintColor(BindableObject bindable, Color? value) =>
		bindable.SetValue(TintColorProperty,value);
}
