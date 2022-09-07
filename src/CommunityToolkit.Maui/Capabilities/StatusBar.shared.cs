using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Capabilities;

namespace CommunityToolkit.Maui.Capabilities;

/// <summary>
/// Class that holds customizations for StatusBar
/// </summary>
public class StatusBarProperties
{
	/// <summary>
	/// Attached <see cref="BindableProperty"/> that manages the StatusBarColor property.
	/// </summary>
	public static readonly BindableProperty StatusBarColorProperty =
		BindableProperty.CreateAttached("StatusBarColor", typeof(Color), typeof(StatusBarProperties), null, propertyChanged: OnStatusBarColorPropertyChanged);

	/// <summary>
	/// Method to set the <see cref="StatusBarColorProperty"/> value.
	/// </summary>
	/// <param name="bindable">The <see cref="BindableObject"/> that will be used to set the property.</param>
	/// <param name="value">The <see cref="Color"/> that will tint the Status bar.</param>
	public static void SetStatusBarColor(BindableObject bindable, Color value) =>
		bindable.SetValue(StatusBarColorProperty, value);

	/// <summary>
	/// Method to get the <see cref="StatusBarColorProperty"/> value attached to this <see cref="BindableObject"/>.
	/// </summary>
	/// <param name="bindable">Get the <see cref="Color"/> set for the <see cref="BindableObject"/>.</param>
	/// <returns>The <see cref="Color"/> value, if has one.</returns>
	public static Color GetStatusBarColor(BindableObject bindable) =>
		(Color)bindable.GetValue(StatusBarStyleProperty);

	static void OnStatusBarColorPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (bindable is Element element && element.Handler is null)
		{
			CheckForNull(element, oldValue, newValue, OnStatusBarColorPropertyChanged);
			return;
		}

		StatusBar.SetColor((Color)newValue);
	}

	static void CheckForNull(Element element, object oldValue, object newValue, Action<BindableObject, object, object> action)
	{
		element.HandlerChanged += OnHandlerChanged;

		void OnHandlerChanged(object? sender, EventArgs e)
		{
			element.HandlerChanged -= OnHandlerChanged;
			OnStatusBarColorPropertyChanged(element, oldValue, newValue);
		}
	}

	/// <summary>
	/// Attached <see cref="BindableProperty"/> that manages the StatusBarStyle property.
	/// </summary>
	public static readonly BindableProperty StatusBarStyleProperty =
		BindableProperty.CreateAttached("StatusBarStyle", typeof(StatusBarStyle), typeof(StatusBarProperties), StatusBarStyle.Default, propertyChanged: OnStatusBarStylePropertyChanged);

	/// <summary>
	/// Method to set the <see cref="StatusBarStyleProperty"/> value.
	/// </summary>
	/// <param name="bindable">The <see cref="BindableObject"/> that will be used to set the property.</param>
	/// <param name="value">The <see cref="StatusBarStyle"/> that will be used for the Status bar.</param>
	public static void SetStatusBarStyle(BindableObject bindable, StatusBarStyle value) =>
		bindable.SetValue(StatusBarStyleProperty, value);

	/// <summary>
	/// Method to get the <see cref="StatusBarStyleProperty"/> value attached to this <see cref="BindableObject"/>.
	/// </summary>
	/// <param name="bindable">Get the <see cref="StatusBarStyle"/> set for the <see cref="BindableObject"/>.</param>
	/// <returns>The <see cref="StatusBarStyle"/>.</returns>
	public static StatusBarStyle GetStatusBarStyle(BindableObject bindable) =>
		(StatusBarStyle)bindable.GetValue(StatusBarStyleProperty);

	static void OnStatusBarStylePropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (bindable is Element element && element.Handler is null)
		{
			CheckForNull(element, oldValue, newValue, OnStatusBarStylePropertyChanged);
			return;
		}
		
		StatusBar.SetStyle((StatusBarStyle)newValue);
	}
}
