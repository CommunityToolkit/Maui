namespace CommunityToolkit.Maui.Layouts;

/// <summary>
/// The <see cref="StateContainer"/> attached properties enable any <see cref="Layout"/> inheriting element to become state-aware.
/// States are defined in the <see cref="StateViewsProperty"/> with <see cref="StateView"/> attached properties.
/// </summary>
public static class StateContainer
{
	const string stateViewsPropertyName = "StateViews";
	const string currentStatePropertyName = "CurrentState";
	const string canStateChangePropertyName = "CanStateChange";
	const string layoutControllerPropertyName = "LayoutController";
	const string shouldAnimateOnStateChangePropertyName = "ShouldAnimateOnStateChange";

	internal static readonly BindableProperty LayoutControllerProperty
		= BindableProperty.CreateAttached(layoutControllerPropertyName, typeof(StateContainerController), typeof(StateContainer), default(StateContainerController), defaultValueCreator: ContainerControllerCreator);

	/// <summary>
	/// Backing <see cref="BindableProperty"/> for the <see cref="GetStateViews"/> and <see cref="SetStateViews"/> methods.
	/// </summary>
	public static readonly BindableProperty StateViewsProperty
		= BindableProperty.CreateAttached(stateViewsPropertyName, typeof(IList<View>), typeof(StateContainer), default(IList<View>), defaultValueCreator: bindable => new List<View>());

	/// <summary>
	/// Backing <see cref="BindableProperty"/> for the <see cref="GetCurrentState"/> and <see cref="SetCurrentState"/> methods.
	/// When <see cref="ShouldAnimateOnStateChangeProperty"/> is <see cref="true"/>, a <see cref="StateContainerException"/> may be thrown when <see cref="CurrentStateProperty"/> is changed while an animation is in progress
	/// To ensure <see cref="StateContainer"/> does not throw a <see cref="StateContainerException"/> due to active animations, first verify <see cref="CanStateChangeProperty"/> is <see cref="true"/> before changing <see cref="CurrentStateProperty"/>
	/// </summary>
	public static readonly BindableProperty CurrentStateProperty
		= BindableProperty.CreateAttached(currentStatePropertyName, typeof(string), typeof(StateContainer), default(string), propertyChanged: OnCurrentStateChanged);

	/// <summary>
	/// Backing <see cref="BindableProperty"/> for the <see cref="GetShouldAnimateOnStateChange"/> and <see cref="SetShouldAnimateOnStateChange"/> methods.
	/// When <see cref="true"/>, a <see cref="StateContainerException"/> may be thrown when <see cref="CurrentStateProperty"/> is changed while an animation is in progress
	/// To ensure <see cref="StateContainer"/> does not throw a <see cref="StateContainerException"/> due to active animations, first verify <see cref="CanStateChangeProperty"/> is <see cref="true"/> before changing <see cref="CurrentStateProperty"/>
	/// </summary>
	public static readonly BindableProperty ShouldAnimateOnStateChangeProperty
		= BindableProperty.CreateAttached(shouldAnimateOnStateChangePropertyName, typeof(bool), typeof(StateContainer), false, propertyChanged: OnShouldAnimateOnStateChangeChanged);

	/// <summary>
	/// Backing <see cref="BindableProperty"/> for the <see cref="GetCanStateChange"/> method.
	/// </summary>
	public static readonly BindableProperty CanStateChangeProperty
		= BindableProperty.CreateAttached(canStateChangePropertyName, typeof(bool), typeof(StateContainer), true, BindingMode.OneWayToSource);

	/// <summary>
	/// Set the StateViews property
	/// </summary>
	public static void SetStateViews(BindableObject b, IList<View> value)
		=> b.SetValue(StateViewsProperty, value);

	/// <summary>
	/// Get the CanStateChange property
	/// </summary>
	public static bool GetCanStateChange(BindableObject b)
		=> (bool)b.GetValue(CanStateChangeProperty);

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

	static void SetCanStateChange(BindableObject b, bool value)
		=> b.SetValue(CanStateChangeProperty, value);

	static async void OnCurrentStateChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (oldValue == newValue)
		{
			return;
		}

		if(!GetCanStateChange(bindable))
		{
			throw new StateContainerException($"{canStateChangePropertyName} is false. {currentStatePropertyName} cannot be changed while a state change is in progress. To avoid this exception due to active animations, first verify {canStateChangePropertyName} is {true} before changing {currentStatePropertyName}");
		}

		SetCanStateChange(bindable, false);

		var newState = (string)newValue;

		if (string.IsNullOrEmpty(newState))
		{
			await GetContainerController(bindable).SwitchToContent(GetShouldAnimateOnStateChange(bindable));
		}
		else
		{
			await GetContainerController(bindable).SwitchToState(newState, GetShouldAnimateOnStateChange(bindable));
		}

		SetCanStateChange(bindable, true);
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

/// <summary>
/// An <see cref="InvalidOperationException"/> thrown when <see cref="StateContainer"/> enters an invalid state
/// </summary>
public sealed class StateContainerException : InvalidOperationException
{
	/// <summary>
	/// Constructor for <see cref="StateContainerException"/>
	/// </summary>
	/// <param name="message"><see cref="Exception.Message"/></param>
	public StateContainerException(string message) : base(message)
	{

	}
}