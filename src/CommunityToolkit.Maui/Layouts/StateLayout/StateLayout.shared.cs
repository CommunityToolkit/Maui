using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Layouts;

/// <summary>
/// The <see cref="StateLayout"/> control enables the user to turn any layout element like a <see cref="Grid"/> or <see cref="VerticalStackLayout"/> into an individual state-aware element.
/// Each layout that you make state-aware, using the StateLayout attached properties, contains a collection of <see cref="StateView"/> objects.
/// </summary>
public class StateLayout
{
	internal static readonly BindableProperty LayoutControllerProperty
		= BindableProperty.CreateAttached("LayoutController", typeof(StateLayoutController), typeof(StateLayout), default(StateLayoutController), defaultValueCreator: LayoutControllerCreator);

	internal static readonly BindablePropertyKey StateViewsPropertyKey
		= BindableProperty.CreateReadOnly("StateViews", typeof(IList<StateView>), typeof(StateLayout), default(IList<StateView>), defaultValueCreator: bindable => new List<StateView>());

	/// <summary>
	/// Backing BindableProperty for the <see cref="GetStateViews"/> method.
	/// </summary>
	public static readonly BindableProperty StateViewsProperty = StateViewsPropertyKey.BindableProperty;

	/// <summary>
	/// Backing BindableProperty for the <see cref="GetCurrentState"/> and <see cref="SetCurrentState"/> methods.
	/// </summary>
	public static readonly BindableProperty CurrentStateProperty
		= BindableProperty.CreateAttached("CurrentState", typeof(LayoutState), typeof(StateLayout), default(LayoutState), propertyChanged: OnCurrentStateChanged);

	/// <summary>
	/// Backing BindableProperty for the <see cref="GetCurrentCustomStateKey"/> and <see cref="SetCurrentCustomStateKey"/> methods.
	/// </summary>
	public static readonly BindableProperty CurrentCustomStateKeyProperty
		= BindableProperty.CreateAttached("CurrentCustomStateKey", typeof(string), typeof(StateLayout), default(string), propertyChanged: OnCurrentCustomStateKeyChanged);

	/// <summary>
	/// Backing BindableProperty for the <see cref="GetAnimateStateChanges"/> and <see cref="SetAnimateStateChanges"/> methods.
	/// </summary>
	public static readonly BindableProperty AnimateStateChangesProperty
		= BindableProperty.CreateAttached("AnimateStateChanges", typeof(bool), typeof(StateLayout), true, propertyChanged: OnAnimateStateChangesChanged);

	/// <summary>
	/// Get the defined StateViews
	/// </summary>
	public static IList<StateView>? GetStateViews(BindableObject b)
		=> (IList<StateView>?)b.GetValue(StateViewsProperty);

	/// <summary>
	/// Set the active LayoutState
	/// </summary>
	public static void SetCurrentState(BindableObject b, LayoutState value)
		=> b.SetValue(CurrentStateProperty, value);

	/// <summary>
	/// Get the active LayoutState
	/// </summary>
	public static LayoutState GetCurrentState(BindableObject b)
		=> (LayoutState)b.GetValue(CurrentStateProperty);

	/// <summary>
	/// Get the active LayoutState custom key
	/// </summary>
	public static void SetCurrentCustomStateKey(BindableObject b, string value)
		=> b.SetValue(CurrentCustomStateKeyProperty, value);

	/// <summary>
	/// Set the active LayoutState custom key
	/// </summary>
	public static string? GetCurrentCustomStateKey(BindableObject b)
		=> (string?)b.GetValue(CurrentCustomStateKeyProperty);

	/// <summary>
	/// Set the AnimateStateChanges property
	/// </summary>
	public static void SetAnimateStateChanges(BindableObject b, bool value)
		=> b.SetValue(AnimateStateChangesProperty, value);

	/// <summary>
	/// Get the AnimateStateChanges property
	/// </summary>
	public static bool GetAnimateStateChanges(BindableObject b)
		=> (bool?)b.GetValue(AnimateStateChangesProperty) ?? false;

	static void OnCurrentStateChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (oldValue == newValue)
		{
			return;
		}

		// Swap out the current children for the Loading Template.
		switch (newValue)
		{
			case LayoutState.Custom:
				break;
			case LayoutState.None:
				GetLayoutController(bindable)?.SwitchToContent(GetAnimateStateChanges(bindable));
				break;
			default:
				GetLayoutController(bindable)?.SwitchToTemplate((LayoutState)newValue, null, GetAnimateStateChanges(bindable));
				break;
		}
	}

	static void OnCurrentCustomStateKeyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (oldValue == newValue)
		{
			return;
		}

		var state = GetCurrentState(bindable);

		// Swap out the current children for the Loading Template.
		switch (state)
		{
			case LayoutState.None:
				GetLayoutController(bindable)?.SwitchToContent(GetAnimateStateChanges(bindable));
				break;
			case LayoutState.Custom:
				GetLayoutController(bindable)?.SwitchToTemplate((string)newValue, GetAnimateStateChanges(bindable));
				break;
			default:
				break;
		}
	}

	static void OnAnimateStateChangesChanged(BindableObject bindable, object oldValue, object newValue)
		=> bindable.SetValue(AnimateStateChangesProperty, newValue);

	internal static StateLayoutController? GetLayoutController(BindableObject b) => (StateLayoutController?)b.GetValue(LayoutControllerProperty);

	static object LayoutControllerCreator(BindableObject bindable)
	{
		if (bindable is Layout layoutView)
		{
			return new StateLayoutController(layoutView)
			{
				StateViews = GetStateViews(layoutView) ?? new List<StateView>()
			};
		}

		throw new InvalidOperationException($"Cannot create the StateLayoutController. The specified view '{bindable.GetType().FullName}' does not inherit Layout.");
	}
}