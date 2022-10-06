using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Layouts;

/// <summary>
/// The <see cref="StateLayout"/> control enables the user to turn any layout element like a <see cref="Grid"/> or <see cref="VerticalStackLayout"/> into an individual state-aware element.
/// Each layout that you make state-aware, using the StateLayout attached properties, contains a collection of <see cref="StateView"/> objects.
/// </summary>
public static class StateLayout
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
	/// Backing BindableProperty for the <see cref="GetShouldAnimateOnStateChange"/> and <see cref="SetShouldAnimateOnStateChange"/> methods.
	/// </summary>
	public static readonly BindableProperty ShouldAnimateOnStateChangeProperty
		= BindableProperty.CreateAttached("ShouldAnimateOnStateChange", typeof(bool), typeof(StateLayout), true, propertyChanged: OnShouldAnimateOnStateChangeChanged);

	/// <summary>
	/// Get the defined StateViews
	/// </summary>
	public static IList<StateView> GetStateViews(BindableObject b)
		=> (IList<StateView>)b.GetValue(StateViewsProperty);

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
	/// Set the ShouldAnimateOnStateChange property
	/// </summary>
	public static void SetShouldAnimateOnStateChange(BindableObject b, bool value)
		=> b.SetValue(ShouldAnimateOnStateChangeProperty, value);

	/// <summary>
	/// Get the ShouldAnimateOnStateChange property
	/// </summary>
	public static bool GetShouldAnimateOnStateChange(BindableObject b)
		=> (bool)b.GetValue(ShouldAnimateOnStateChangeProperty);

	internal static StateLayoutController GetLayoutController(BindableObject b) =>
		(StateLayoutController)b.GetValue(LayoutControllerProperty);

	internal static void SetStateViews(BindableObject b, IEnumerable<StateView> value)
		=> b.SetValue(StateViewsPropertyKey, value.ToList());

	static async void OnCurrentStateChanged(BindableObject bindable, object oldValue, object newValue)
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
				await GetLayoutController(bindable).SwitchToContent(GetShouldAnimateOnStateChange(bindable));
				break;

			case LayoutState.Empty:
			case LayoutState.Error:
			case LayoutState.Loading:
			case LayoutState.Saving:
			case LayoutState.Success:
				await GetLayoutController(bindable).SwitchToTemplate((LayoutState)newValue, null, GetShouldAnimateOnStateChange(bindable));
				break;

			default:
				throw new NotSupportedException($"{nameof(LayoutState)} {newValue} Not Supported");
		}
	}

	static async void OnCurrentCustomStateKeyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (oldValue == newValue)
		{
			return;
		}

		var state = GetCurrentState(bindable);

		// Swap out the current children for the Loading Template.
		switch (state)
		{
			case LayoutState.Empty:
			case LayoutState.Error:
			case LayoutState.Loading:
			case LayoutState.Saving:
			case LayoutState.Success:
				break;

			case LayoutState.None:
				await GetLayoutController(bindable).SwitchToContent(GetShouldAnimateOnStateChange(bindable));
				break;

			case LayoutState.Custom:
				await GetLayoutController(bindable).SwitchToTemplate((string)newValue, GetShouldAnimateOnStateChange(bindable));
				break;

			default:
				throw new NotSupportedException($"{nameof(LayoutState)} {state} Not Supported");
		}
	}

	static void OnShouldAnimateOnStateChangeChanged(BindableObject bindable, object oldValue, object newValue)
		=> bindable.SetValue(ShouldAnimateOnStateChangeProperty, newValue);

	static object LayoutControllerCreator(BindableObject bindable)
	{
		if (bindable is not Layout layoutView)
		{
			throw new InvalidOperationException($"Cannot create the {nameof(StateLayoutController)}. The specified view '{bindable.GetType().FullName}' does not inherit Layout.");
		}

		return new StateLayoutController(layoutView)
		{
			StateViews = GetStateViews(layoutView) ?? new List<StateView>()
		};
	}
}