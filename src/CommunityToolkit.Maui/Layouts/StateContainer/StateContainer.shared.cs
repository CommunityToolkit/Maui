using System.Linq.Expressions;

namespace CommunityToolkit.Maui.Layouts;

/// <summary>
/// The <see cref="StateContainer"/> attached properties enable any <see cref="Layout"/> inheriting element to become state-aware.
/// States are defined in the <see cref="StateViewsProperty"/> with <see cref="StateView"/> attached properties.
/// </summary>
public static class StateContainer
{
	internal static readonly BindableProperty LayoutControllerProperty
		= BindableProperty.CreateAttached("LayoutController", typeof(StateContainerController), typeof(StateContainer), default(StateContainerController), defaultValueCreator: ContainerControllerCreator);

	/// <summary>
	/// Backing BindableProperty for the <see cref="GetStateViews"/> and <see cref="SetStateViews"/> methods.
	/// </summary>
	public static readonly BindableProperty StateViewsProperty
		= BindableProperty.CreateAttached("StateViews", typeof(IList<View>), typeof(StateContainer), default(IList<View>), defaultValueCreator: bindable => new List<View>());

	/// <summary>
	/// Backing BindableProperty for the <see cref="GetCurrentState"/> and <see cref="SetCurrentState"/> methods.
	/// </summary>
	public static readonly BindableProperty CurrentStateProperty
		= BindableProperty.CreateAttached("CurrentState", typeof(string), typeof(StateContainer), default(string), propertyChanged: OnCurrentStateChanged);

	/// <summary>
	/// Backing BindableProperty for the <see cref="GetShouldAnimateOnStateChange"/> and <see cref="SetShouldAnimateOnStateChange"/> methods.
	/// </summary>
	public static readonly BindableProperty ShouldAnimateOnStateChangeProperty
		= BindableProperty.CreateAttached("ShouldAnimateOnStateChange", typeof(bool), typeof(StateContainer), true, propertyChanged: OnShouldAnimateOnStateChangeChanged);

	/// <summary>
	/// Set the StateViews property
	/// </summary>
	public static void SetStateViews(BindableObject b, IList<View> value)
		=> b.SetValue(StateViewsProperty, value);

	/// <summary>
	/// Get the StateViews property
	/// </summary>
	public static IList<View> GetStateViews(BindableObject b)
		=> (IList<View>)b.GetValue(StateViewsProperty);

	/// <summary>
	/// Set the CurrentState property
	/// </summary>
	public static void SetCurrentState(BindableObject b, string value)
		=> b.SetValue(CurrentStateProperty, value);

	/// <summary>
	/// Get the CurrentState property
	/// </summary>
	public static string GetCurrentState(BindableObject b)
		=> (string)b.GetValue(CurrentStateProperty);

	/// <summary>
	/// Set the ShouldAnimateOnStateChange property
	/// </summary>
	public static void SetShouldAnimateOnStateChange(BindableObject b, bool value)
		=> b.SetValue(ShouldAnimateOnStateChangeProperty, value);

	/// <summary>
	/// Get the ShouldAnimateOnStateChange property
	/// </summary>
	public static bool GetShouldAnimateOnStateChange(BindableObject b)
		=> (bool)b.GetValue(ShouldAnimateOnStateChangeProperty);

	internal static StateContainerController GetContainerController(BindableObject b) =>
		(StateContainerController)b.GetValue(LayoutControllerProperty);

	static async void OnCurrentStateChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (oldValue == newValue)
		{
			return;
		}

		var newState = (string)newValue;

		if (string.IsNullOrEmpty(newState))
		{
			await GetContainerController(bindable).SwitchToContent(GetShouldAnimateOnStateChange(bindable));
		}
		else
		{
			await GetContainerController(bindable).SwitchToState(newState, GetShouldAnimateOnStateChange(bindable));
		}
	}

	static void OnShouldAnimateOnStateChangeChanged(BindableObject bindable, object oldValue, object newValue)
		=> bindable.SetValue(ShouldAnimateOnStateChangeProperty, newValue);

	static object ContainerControllerCreator(BindableObject bindable)
	{
		if (bindable is not Layout layoutView)
		{
			throw new StateContainerException($"Cannot create the {nameof(StateContainerController)}. The specified view '{bindable.GetType().FullName}' does not inherit Layout.");
		}

		return new StateContainerController(layoutView)
		{
			StateViews = GetStateViews(layoutView)
		};
	}
}

sealed class StateContainerException : InvalidOperationException
{
	public StateContainerException(string message) : base(message)
	{

	}
}