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

	internal static readonly BindableProperty LayoutControllerProperty
		= BindableProperty.CreateAttached(layoutControllerPropertyName, typeof(StateContainerController), typeof(StateContainer), default(StateContainerController), defaultValueCreator: ContainerControllerCreator);

	/// <summary>
	/// Backing <see cref="BindableProperty"/> for the <see cref="GetStateViews"/> and <see cref="SetStateViews"/> methods.
	/// </summary>
	public static readonly BindableProperty StateViewsProperty
		= BindableProperty.CreateAttached(stateViewsPropertyName, typeof(IList<View>), typeof(StateContainer), default(IList<View>), defaultValueCreator: _ => new List<View>());

	/// <summary>
	/// Backing <see cref="BindableProperty"/> for the <see cref="GetCurrentState"/> and <see cref="SetCurrentState"/> methods.
	/// To ensure <see cref="StateContainer"/> does not throw a <see cref="StateContainerException"/> due to active animations, first verify <see cref="CanStateChangeProperty"/> is <see langword="true"/> before changing <see cref="CurrentStateProperty"/>
	/// </summary>
	public static readonly BindableProperty CurrentStateProperty
		= BindableProperty.CreateAttached(currentStatePropertyName, typeof(string), typeof(StateContainer), default(string), propertyChanging: OnCurrentStateChanging);

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
	public static void SetCurrentState(BindableObject b, string? value)
		=> b.SetValue(CurrentStateProperty, value);

	/// <summary>
	/// Get the CurrentState property
	/// </summary>
	public static string GetCurrentState(BindableObject b)
		=> (string)b.GetValue(CurrentStateProperty);

	/// <summary>
	/// Change state with custom animation.
	/// </summary>
	public static async Task ChangeStateWithAnimation(
		BindableObject bindable,
		string? state,
		Animation? beforeStateChange,
		Animation? afterStateChange,
		CancellationToken token)
	{
		if (beforeStateChange is null && afterStateChange is null)
		{
			throw new ArgumentException($"Anmiation required. Parameters {nameof(beforeStateChange)} and {nameof(afterStateChange)} cannot both be null");
		}

		ValidateCanStateChange(bindable);
		SetCanStateChange(bindable, false);

		var layout = GetContainerController(bindable).GetLayout();

		try
		{
			if (layout.Children.Count > 0 && beforeStateChange is not null)
			{
				var beforeAnimationTCS = new TaskCompletionSource<bool>();
				layout.Children.OfType<View>().ForEach(view => view.Animate(nameof(beforeStateChange), beforeStateChange, finished: (_, result) => beforeAnimationTCS.SetResult(result)));

				await beforeAnimationTCS.Task.WaitAsync(token);
			}

			ChangeState(bindable, state);

			if (layout.Children.Count > 0 && afterStateChange is not null)
			{
				var animationAnimationTCS = new TaskCompletionSource<bool>();
				layout.Children.OfType<View>().ForEach(view => view.Animate(nameof(afterStateChange), afterStateChange, finished: (_, result) => animationAnimationTCS.SetResult(result)));

				await animationAnimationTCS.Task.WaitAsync(token);
			}
		}
		finally
		{
			SetCanStateChange(bindable, true);
			SetCurrentState(bindable, state);
		}
	}

	/// <summary>
	/// Change state with custom animation.
	/// </summary>
	public static async Task ChangeStateWithAnimation(
		BindableObject bindable,
		string? state,
		Func<VisualElement, CancellationToken, Task>? beforeStateChange,
		Func<VisualElement, CancellationToken, Task>? afterStateChange,
		CancellationToken cancellationToken)
	{
		if (beforeStateChange is null && afterStateChange is null)
		{
			throw new ArgumentException($"Anmiation required. Parameters {nameof(beforeStateChange)} and {nameof(afterStateChange)} cannot both be null");
		}

		ValidateCanStateChange(bindable);
		SetCanStateChange(bindable, false);

		var layout = GetContainerController(bindable).GetLayout();

		try
		{
			if (layout.Children.Count > 0 && beforeStateChange is not null)
			{
				await beforeStateChange.Invoke(layout, cancellationToken).WaitAsync(cancellationToken);
			}

			ChangeState(bindable, state);

			if (layout.Children.Count > 0 && afterStateChange is not null)
			{
				await afterStateChange.Invoke(layout, cancellationToken).WaitAsync(cancellationToken);
			}

		}
		finally
		{
			SetCanStateChange(bindable, true);
			SetCurrentState(bindable, state);
		}
	}

	/// <summary>
	/// Change state using the default fade animation.
	/// </summary>
	public static async Task ChangeStateWithAnimation(BindableObject bindable, string? state, CancellationToken token)
	{
		ValidateCanStateChange(bindable);
		SetCanStateChange(bindable, false);

		var layout = GetContainerController(bindable).GetLayout();

		try
		{
			if (layout.Children.Count > 0)
			{
				await Task.WhenAll(layout.Children.OfType<View>().Select(view => view.FadeTo(0))).WaitAsync(token);
			}

			ChangeState(bindable, state);

			if (layout.Children.Count > 0)
			{
				await Task.WhenAll(layout.Children.OfType<View>().Select(view => view.FadeTo(1))).WaitAsync(token);
			}
		}
		finally
		{
			SetCanStateChange(bindable, true);
			SetCurrentState(bindable, state);
		}
	}

	internal static StateContainerController GetContainerController(BindableObject b) =>
		(StateContainerController)b.GetValue(LayoutControllerProperty);

	static void SetCanStateChange(BindableObject b, bool value)
		=> b.SetValue(CanStateChangeProperty, value);

	static void OnCurrentStateChanging(BindableObject bindable, object oldValue, object newValue)
	{
		if (oldValue == newValue)
		{
			return;
		}

		ValidateCanStateChange(bindable);

		ChangeState(bindable, (string?)newValue);
	}

	static void ChangeState(BindableObject bindable, string? state)
	{
		if (string.IsNullOrEmpty(state))
		{
			GetContainerController(bindable).SwitchToContent();
		}
		else
		{
			GetContainerController(bindable).SwitchToState(state);
		}
	}

	static StateContainerController ContainerControllerCreator(BindableObject bindable)
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

	static void ValidateCanStateChange(in BindableObject bindable)
	{
		if (!GetCanStateChange(bindable))
		{
			throw new StateContainerException($"{canStateChangePropertyName} is false. {currentStatePropertyName} cannot be changed while a state change is in progress. To avoid this exception, first verify {canStateChangePropertyName} is {true} before changing {currentStatePropertyName}.");
		}
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