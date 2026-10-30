using ViewExtensions = Microsoft.Maui.Controls.ViewExtensions;

namespace CommunityToolkit.Maui.Layouts;

/// <summary>
/// The <see cref="StateContainer"/> attached properties enable any <see cref="Layout"/> inheriting element to become state-aware.
/// States are defined in the <see cref="StateViewsProperty"/> with <see cref="StateView"/> attached properties.
/// </summary>
[AttachedBindableProperty<IList<View>>(stateViewsPropertyName, DefaultValueCreatorMethodName = nameof(CreateDefaultStateViewsProperty))]
[AttachedBindableProperty<string>(currentStatePropertyName, IsNullable = true, DefaultValue = StateContainerDefaults.CurrentState, PropertyChangingMethodName = nameof(OnCurrentStateChanging), BindablePropertyXmlDocumentation = currentStateBindablePropertyXmlDocumentation)]
[AttachedBindableProperty<bool>(canStateChangePropertyName, DefaultValue = StateContainerDefaults.CanStateChange, DefaultBindingMode = BindingMode.OneWayToSource, SetterAccessibility = AccessModifier.Private)]
[AttachedBindableProperty<StateContainerController>(layoutControllerPropertyName, DefaultValueCreatorMethodName = nameof(ContainerControllerCreator), BindablePropertyAccessibility = AccessModifier.Internal, GetterAccessibility = AccessModifier.Internal, SetterAccessibility = AccessModifier.None)]
public static partial class StateContainer
{
	const string stateViewsPropertyName = "StateViews";
	const string currentStatePropertyName = "CurrentState";
	const string canStateChangePropertyName = "CanStateChange";
	const string layoutControllerPropertyName = "LayoutController";

	const string currentStateBindablePropertyXmlDocumentation =
		/* language=C#-test */
		//lang=csharp
		"""
		/// <summary>
		/// Backing <see cref="BindableProperty"/> for the <see cref="GetCurrentState"/> and <see cref="SetCurrentState"/> methods.
		/// To ensure <see cref="StateContainer"/> does not throw a <see cref="StateContainerException"/> due to active animations, first verify <see cref="CanStateChangeProperty"/> is <see langword="true"/> before changing <see cref="CurrentStateProperty"/>
		/// </summary>
		""";

	/// <summary>
	/// Change state with custom animation.
	/// </summary>
	public static async Task ChangeStateWithAnimation(
		BindableObject bindable,
		string? state,
		Animation? beforeStateChange,
		Animation? afterStateChange,
		CancellationToken token = default)
	{
		token.ThrowIfCancellationRequested();
		if (beforeStateChange is null && afterStateChange is null)
		{
			throw new ArgumentException($"Animation required. Parameters {nameof(beforeStateChange)} and {nameof(afterStateChange)} cannot both be null");
		}

		ValidateCanStateChange(bindable);
		SetCanStateChange(bindable, false);

		var layout = GetLayoutController(bindable).GetLayout();

		try
		{
			if (layout.Children.Count > 0 && beforeStateChange is not null)
			{
				await AnimateChildren(layout, nameof(beforeStateChange), beforeStateChange, token);
			}

			ChangeState(bindable, state);

			if (layout.Children.Count > 0 && afterStateChange is not null)
			{
				await AnimateChildren(layout, nameof(afterStateChange), afterStateChange, token);
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
		CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		if (beforeStateChange is null && afterStateChange is null)
		{
			throw new ArgumentException($"Animation required. Parameters {nameof(beforeStateChange)} and {nameof(afterStateChange)} cannot both be null");
		}

		ValidateCanStateChange(bindable);
		SetCanStateChange(bindable, false);

		var layout = GetLayoutController(bindable).GetLayout();

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
	/// Change state using the default fade animation: the current views fade out, then the views for <paramref name="state"/> replace them and fade in.
	/// The views that faded out get their <see cref="VisualElement.Opacity"/> back when the state change ends, also when it fails or is canceled, so they are visible the next time they are shown.
	/// Canceling <paramref name="token"/> ends the fade at once instead of letting it run on: a view that was fading in gets an <see cref="VisualElement.Opacity"/> of 1.
	/// </summary>
	public static async Task ChangeStateWithAnimation(BindableObject bindable, string? state, CancellationToken token = default)
	{
		token.ThrowIfCancellationRequested();
		ValidateCanStateChange(bindable);
		SetCanStateChange(bindable, false);

		var layout = GetLayoutController(bindable).GetLayout();
		var replacedViews = layout.Children.OfType<View>().Select(static view => (View: view, view.Opacity)).ToList();
		List<View> shownViews = [];

		try
		{
			if (layout.Children.Count > 0)
			{
				await Task.WhenAll(layout.Children.OfType<View>().Select(view => view.FadeToAsync(0))).WaitAsync(token);
			}

			ChangeState(bindable, state);

			if (layout.Children.Count > 0)
			{
				shownViews.AddRange(layout.Children.OfType<View>());

				// Fade in from transparent, also the first time a view is shown
				foreach (var view in shownViews)
				{
					view.Opacity = 0;
				}

				await Task.WhenAll(shownViews.Select(view => view.FadeToAsync(1))).WaitAsync(token);
			}
		}
		finally
		{
			// WaitAsync stops waiting when the token is canceled, not the fade: end it here, or it goes on changing Opacity after the state change ended,
			// and the next state change would give a view the Opacity it had in the middle of the fade
			foreach (var view in shownViews)
			{
				if (view.AbortAnimation(nameof(ViewExtensions.FadeToAsync)))
				{
					view.Opacity = 1;
				}
			}

			// Otherwise a view that the new state replaced stays transparent when a later state change without the fade animation shows it again,
			// and the current views stay transparent when the state change fails, for example for a state without a StateView
			foreach (var (view, opacity) in replacedViews)
			{
				view.AbortAnimation(nameof(ViewExtensions.FadeToAsync));
				view.Opacity = opacity;
			}

			SetCanStateChange(bindable, true);
			SetCurrentState(bindable, state);
		}
	}

	static async Task AnimateChildren(Layout layout, string name, Animation animation, CancellationToken token)
	{
		List<Task<bool>> animationTasks = [];
		foreach (var view in layout.Children.OfType<View>())
		{
			var animationTCS = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			view.Animate(name, animation, finished: (_, result) => animationTCS.TrySetResult(result));
			animationTasks.Add(animationTCS.Task);
		}

		await Task.WhenAll(animationTasks).WaitAsync(token);
	}

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
			GetLayoutController(bindable).SwitchToContent();
		}
		else
		{
			GetLayoutController(bindable).SwitchToState(state);
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

	static IList<View> CreateDefaultStateViewsProperty(BindableObject bindable) => [];
}

/// <summary>
/// An <see cref="InvalidOperationException"/> thrown when <see cref="StateContainer"/> enters an invalid state
/// </summary>
/// <remarks>
/// Constructor for <see cref="StateContainerException"/>
/// </remarks>
/// <param name="message">The error message that explains the reason for the exception.</param>
/// <param name="innerException">The exception that is the cause of the current exception. If the <paramref name="innerException" /> parameter is not a null reference (<see langword="Nothing" /> in Visual Basic), the current exception is raised in a <see langword="catch" /> block that handles the inner exception.</param>
public sealed class StateContainerException(string message, Exception? innerException = null) : InvalidOperationException(message, innerException);