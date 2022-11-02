namespace CommunityToolkit.Maui.Layouts;

/// <summary>
/// The <see cref="StateView"/> attached properties can be used on any <see cref="IView"/> inheriting element to represent a state inside a <see cref="StateContainer.StateViewsProperty"/>.
/// </summary>
public static class StateView
{
	/// <summary>
	/// Backing BindableProperty for the <see cref="GetStateKey(BindableObject)"/> and <see cref="SetStateKey(BindableObject, string)"/> methods.
	/// </summary>
	public static readonly BindableProperty StateKeyProperty
		= BindableProperty.CreateAttached("StateKey", typeof(string), typeof(IView), string.Empty);

	/// <summary>
	/// Set the StateKey property
	/// </summary>
	/// <param name="b">The <see cref="VisualElement"/> that will set the property.</param>
	/// <param name="value">The StateKey value for this <see cref="VisualElement"/>.</param>
	public static void SetStateKey(BindableObject b, string value)
		=> b.SetValue(StateKeyProperty, value);

	/// <summary>
	/// Get the StateKey property
	/// </summary>
	/// <param name="b"></param>
	public static string GetStateKey(BindableObject b)
		=> (string)b.GetValue(StateKeyProperty);
}