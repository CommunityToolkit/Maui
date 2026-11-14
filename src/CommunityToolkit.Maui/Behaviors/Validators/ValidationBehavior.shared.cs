using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Windows.Input;
using Microsoft.Maui.Controls.Internals;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>Validation flags</summary>
[Flags]
public enum ValidationFlags
{
	/// <summary> No validation</summary>
	None = 0,

	/// <summary>Validate on attaching</summary>
	ValidateOnAttaching = 1,

	/// <summary> Validate on focusing</summary>
	ValidateOnFocused = 2,

	/// <summary>Validate on unfocus</summary>
	ValidateOnUnfocused = 4,

	/// <summary>Validate upon value changed</summary>
	ValidateOnValueChanged = 8,

	/// <summary>Force make valid when focused</summary>
	ForceMakeValidWhenFocused = 16
}

/// <summary>
/// The <see cref="ValidationBehavior"/> allows users to create custom validation behaviors. All the validation behaviors in the Xamarin Community Toolkit inherit from this behavior, to expose a number of shared properties. Users can inherit from this class to create a custom validation behavior currently not supported through the Xamarin Community Toolkit. This behavios cannot be used directly as it's abstract.
/// </summary>
public abstract partial class ValidationBehavior : BaseBehavior<VisualElement>, IDisposable
{
	/// <summary>
	/// Valid visual state
	/// </summary>
	public const string ValidVisualState = "Valid";

	/// <summary>
	/// Invalid visual state
	/// </summary>
	public const string InvalidVisualState = "Invalid";

	readonly SemaphoreSlim isAttachingSemaphoreSlim = new(1, 1);

	ValidationFlags currentStatus;

	CancellationTokenSource? validationTokenSource;

	bool isDisposed;

	bool hasValidated;

	// MAUI has no public way to read which binding a BindableProperty has, and a binding that the user set on Value must survive
	bool isValueBoundByBehavior;

	/// <summary>
	/// Initialize a new instance of ValidationBehavior
	/// </summary>
	protected ValidationBehavior()
	{
		DefaultForceValidateCommand = new Command<CancellationToken>(async token => await ForceValidate(token).ConfigureAwait(false));
	}

	/// <summary>
	/// Finalizer
	/// </summary>
	~ValidationBehavior()
	{
		Dispose(false);
	}

	/// <summary>
	/// Indicates whether the current value is considered valid. This is a bindable property.
	/// </summary>
	[BindableProperty(DefaultBindingMode = BindingMode.OneWayToSource, PropertyChangedMethodName = nameof(OnIsValidPropertyChanged))]
	public partial bool IsValid { get; set; } = ValidationBehaviorDefaults.IsValid;

	/// <summary>
	/// Indicates whether the validation is in progress now (waiting for an asynchronous call is finished).
	/// </summary>
	[BindableProperty(DefaultBindingMode = BindingMode.OneWayToSource)]
	public partial bool IsRunning { get; set; } = ValidationBehaviorDefaults.IsRunning;

	/// <summary>
	/// Indicates whether the current value is considered not valid. This is a bindable property.
	/// </summary>
	[BindableProperty(DefaultBindingMode = BindingMode.OneWayToSource)]
	public partial bool IsNotValid { get; set; } = ValidationBehaviorDefaults.IsNotValid;

	/// <summary>
	/// The <see cref="Style"/> to apply to the element when validation is successful. This is a bindable property.
	/// </summary>
	[BindableProperty(PropertyChangedMethodName = nameof(OnValidationPropertyChanged))]
	public partial Style? ValidStyle { get; set; } = ValidationBehaviorDefaults.ValidStyle;

	/// <summary>
	/// The <see cref="Style"/> to apply to the element when validation fails. This is a bindable property.
	/// </summary>
	[BindableProperty(PropertyChangedMethodName = nameof(OnValidationPropertyChanged))]
	public partial Style? InvalidStyle { get; set; } = ValidationBehaviorDefaults.InvalidStyle;

	/// <summary>
	/// Provides an enumerated value that specifies how to handle validation. This is a bindable property.
	/// </summary>
	/// <remarks>
	/// The flags decide when <see cref="Value"/> is validated. Once the behavior has validated a value, a change of a validation rule (for example <see cref="TextValidationBehavior.MinimumLength"/>), of <see cref="ValidStyle"/>, of <see cref="InvalidStyle"/> or of <see cref="Flags"/> validates the current <see cref="Value"/>, whatever the flags, so that <see cref="IsValid"/> follows the current rules.
	/// <see cref="ValidationFlags.ForceMakeValidWhenFocused"/> still keeps a focused view valid.
	/// </remarks>
	[BindableProperty(PropertyChangedMethodName = nameof(OnValidationPropertyChanged))]
	public partial ValidationFlags Flags { get; set; } = ValidationBehaviorDefaults.Flags;

	/// <summary>
	/// The value to validate. This is a bindable property.
	/// </summary>
	[BindableProperty(PropertyChangedMethodName = nameof(OnValuePropertyChanged))]
	public partial object? Value { get; set; } = ValidationBehaviorDefaults.Value;

	/// <summary>
	/// Allows the user to override the property that will be used as the value to validate. This is a bindable property.
	/// </summary>
	/// <remarks>
	/// A name that matches a <see cref="BindableProperty"/> on the view (for example <c>Text</c>, <c>IsChecked</c> or <c>SelectedItem</c>) is safe to use with trimming and NativeAOT.
	/// To validate any other property, set this property to <see langword="null"/> first, then bind <see cref="Value"/> with a compiled binding, which the behavior leaves untouched.
	/// Any other non-empty name (a CLR property, a dotted path, a typo) throws an <see cref="ArgumentException"/> when the behavior attaches, or when the name changes while attached (a trace warning instead when exceptions in behaviors are suppressed).
	/// </remarks>
	[BindableProperty(DefaultValueCreatorMethodName = nameof(GetDefaultValuePropertyName), PropertyChangedMethodName = nameof(OnValuePropertyNamePropertyChanged))]
	public partial string? ValuePropertyName { get; set; }

	/// <summary>
	/// Allows the user to provide a custom <see cref="ICommand"/> that handles forcing validation. This is a bindable property.
	/// </summary>
	/// <remarks>
	/// The Default Value for <see cref="ForceValidateCommand"/> has a <see cref="Type"/> of Command&lt;CancellationToken&gt; which requires a <see cref="CancellationToken"/> as a CommandParameter. See <see cref="Command{CancellationToken}"/> and <see cref="System.Windows.Input.ICommand.Execute(object)"/> for more information on passing a <see cref="CancellationToken"/> into <see cref="Command{T}"/> as a CommandParameter"
	/// </remarks>
	[BindableProperty(DefaultBindingMode = BindingMode.OneWayToSource, DefaultValueCreatorMethodName = nameof(GetDefaultForceValidateCommand))]
	public partial ICommand ForceValidateCommand { get; set; }

	/// <summary>
	/// Default value property name
	/// </summary>
	protected virtual string DefaultValuePropertyName { get; } = ValidationBehaviorDefaults.ValuePropertyName;

	/// <summary>
	/// Default force validate command
	/// </summary>
	protected virtual Command<CancellationToken> DefaultForceValidateCommand { get; }

	/// <summary>
	/// Forces the behavior to make a validation pass.
	/// </summary>
	public ValueTask ForceValidate(CancellationToken token = default) => UpdateStateAsync(View, Flags, true, token);

	/// <inheritdoc/>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	internal ValueTask ValidateNestedAsync(CancellationToken token) => UpdateStateAsync(View, Flags, true, token);

	/// <summary>
	/// Used when a property in <see cref="ValidationBehavior"/> changes to update the validation state
	/// </summary>
	/// <remarks>
	/// Once the behavior has validated a value, this method validates the current <see cref="Value"/> whatever <see cref="Flags"/> includes, so that <see cref="IsValid"/> follows the new property value.
	/// Before that, it validates the value only when <see cref="Flags"/> includes the current state of the view, as for a new <see cref="Value"/>.
	/// <see cref="ValidationFlags.ForceMakeValidWhenFocused"/> still keeps a focused view valid.
	/// </remarks>
	/// <param name="bindable"></param>
	/// <param name="oldValue"></param>
	/// <param name="newValue"></param>
	protected static async void OnValidationPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		try
		{
			var validationBehavior = (ValidationBehavior)bindable;

			// The last result can be stale even when Flags does not include the current state of the view, for example ValidateOnValueChanged after the view lost the focus
			await validationBehavior.UpdateStateAsync(validationBehavior.View, validationBehavior.Flags, validationBehavior.hasValidated);
		}
		catch (Exception ex) when (Options.ShouldSuppressExceptionsInBehaviors)
		{
			Trace.TraceInformation("{0}", ex);
		}
	}

	/// <summary>
	/// Validate value
	/// </summary>
	protected abstract ValueTask<bool> ValidateAsync(object? value, CancellationToken token);

	/// <summary>
	/// Disposes of managed resources
	/// </summary>
	/// <param name="disposing"></param>
	protected virtual void Dispose(bool disposing)
	{
		if (isDisposed)
		{
			return;
		}

		if (disposing)
		{
			isAttachingSemaphoreSlim.Dispose();
		}

		isDisposed = true;
	}

	/// <summary>
	/// Decorate value
	/// </summary>
	protected virtual object? Decorate(object? value) => value;

	/// <inheritdoc/>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="bindable"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentException">Thrown when <see cref="ValuePropertyName"/> names no BindableProperty on <paramref name="bindable"/>, unless exceptions in behaviors are suppressed.</exception>
	protected override void OnAttachedTo(VisualElement bindable)
	{
		ArgumentNullException.ThrowIfNull(bindable);

		// Resolved here, not in the async void ValidateOnAttaching, because a throw there would crash the app
		if (!IsValuePropertyNameValidFor(bindable))
		{
			ReportInvalidValuePropertyName(bindable);
		}

		base.OnAttachedTo(bindable);

		ValidateOnAttaching();
	}

	/// <inheritdoc/>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="bindable"/> is <see langword="null"/>.</exception>
	protected override void OnDetachingFrom(VisualElement bindable)
	{
		ArgumentNullException.ThrowIfNull(bindable);

		RemoveValueBindingOfBehavior();

		currentStatus = ValidationFlags.None;
		hasValidated = false;
		base.OnDetachingFrom(bindable);
	}

	/// <inheritdoc/>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="sender"/> or <paramref name="e"/> is <see langword="null"/>.</exception>
	protected override void OnViewPropertyChanged(VisualElement sender, PropertyChangedEventArgs e)
	{
		ArgumentNullException.ThrowIfNull(sender);
		ArgumentNullException.ThrowIfNull(e);

		base.OnViewPropertyChanged(sender, e);

		if (e.PropertyName == VisualElement.IsFocusedProperty.PropertyName)
		{
			currentStatus = sender.IsFocused switch
			{
				true => ValidationFlags.ValidateOnFocused,
				false => ValidationFlags.ValidateOnUnfocused
			};

			UpdateStateOnFocusChanged();
		}
	}

	static void OnIsValidPropertyChanged(BindableObject bindable, object oldValue, object newValue)
		=> ((ValidationBehavior)bindable).OnIsValidPropertyChanged();

	static async void OnValuePropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var validationBehavior = (ValidationBehavior)bindable;
		await validationBehavior.OnValuePropertyChanged(CancellationToken.None);

		try
		{
			// Unlike a rule change (OnValidationPropertyChanged), a new value is validated only when Flags includes ValidateOnValueChanged
			await validationBehavior.UpdateStateAsync(validationBehavior.View, validationBehavior.Flags, false);
		}
		catch (Exception ex) when (Options.ShouldSuppressExceptionsInBehaviors)
		{
			Trace.TraceInformation("{0}", ex);
		}
	}

	static void OnValuePropertyNamePropertyChanged(BindableObject bindable, object oldValue, object newValue)
		=> ((ValidationBehavior)bindable).OnValuePropertyNamePropertyChanged();

	static object GetDefaultForceValidateCommand(BindableObject bindable)
		=> ((ValidationBehavior)bindable).DefaultForceValidateCommand;

	static object GetDefaultValuePropertyName(BindableObject bindable)
		=> ((ValidationBehavior)bindable).DefaultValuePropertyName;

	void OnIsValidPropertyChanged() => IsNotValid = !IsValid;

	async void ValidateOnAttaching()
	{
		await isAttachingSemaphoreSlim.WaitAsync();

		try
		{
			currentStatus = ValidationFlags.ValidateOnAttaching;

			BindValueToValuePropertyName();
			await UpdateStateAsync(View, Flags, false);
		}
		finally
		{
			isAttachingSemaphoreSlim.Release();
		}
	}

	async void UpdateStateOnFocusChanged() => await UpdateStateAsync(View, Flags, false);

	async Task OnValuePropertyChanged(CancellationToken token)
	{
		await isAttachingSemaphoreSlim.WaitAsync(token);

		try
		{
			currentStatus = ValidationFlags.ValidateOnValueChanged;
		}
		finally
		{
			isAttachingSemaphoreSlim.Release();
		}
	}

	void OnValuePropertyNamePropertyChanged()
	{
		if (View is not VisualElement view || string.IsNullOrEmpty(ValuePropertyName))
		{
			RemoveValueBindingOfBehavior();
			return;
		}

		if (!TryGetViewBindableProperty(view, ValuePropertyName, out var viewProperty))
		{
			RemoveValueBindingOfBehavior();
			ReportInvalidValuePropertyName(view);
			return;
		}

		BindValueTo(view, viewProperty);
	}

	void BindValueToValuePropertyName()
	{
		if (View is VisualElement view
			&& !string.IsNullOrEmpty(ValuePropertyName)
			&& TryGetViewBindableProperty(view, ValuePropertyName, out var viewProperty))
		{
			BindValueTo(view, viewProperty);
			return;
		}

		RemoveValueBindingOfBehavior();
	}

	void BindValueTo(VisualElement view, BindableProperty viewProperty)
	{
		SetBinding(ValueProperty, new TypedBinding<VisualElement, object?>(
			getter: source => (source.GetValue(viewProperty), true),
			setter: null,
			handlers: [Tuple.Create<Func<VisualElement, object?>, string>(static source => source, viewProperty.PropertyName)])
		{
			Source = view
		});
		isValueBoundByBehavior = true;
	}

	void RemoveValueBindingOfBehavior()
	{
		if (!isValueBoundByBehavior)
		{
			return;
		}

		RemoveBinding(ValueProperty);
		isValueBoundByBehavior = false;
	}

	bool IsValuePropertyNameValidFor(VisualElement view)
		=> string.IsNullOrEmpty(ValuePropertyName) || TryGetViewBindableProperty(view, ValuePropertyName, out _);

	[SuppressMessage("Usage", "CA2208:Instantiate argument exceptions correctly", Justification = "The invalid argument is the ValuePropertyName property.")]
	void ReportInvalidValuePropertyName(VisualElement view)
	{
		var message = $"{GetType().Name}.{nameof(ValuePropertyName)} '{ValuePropertyName}' names no public static BindableProperty field '{ValuePropertyName}Property' on {view.GetType().Name}. "
			+ $"Name a BindableProperty, or set {nameof(ValuePropertyName)}=\"{{x:Null}}\" and bind {nameof(Value)} with a compiled binding, for example Value=\"{{Binding {ValuePropertyName}, Source={{x:Reference myView}}, x:DataType={view.GetType().Name}}}\".";

		if (Options.ShouldSuppressExceptionsInBehaviors)
		{
			Trace.TraceWarning("{0}", message);
			return;
		}

		throw new ArgumentException(message, nameof(ValuePropertyName));
	}

	// view.GetType() carries MAUI's DynamicallyAccessedMembers(PublicFields) on VisualElement, which keeps the "<Name>Property" fields
	static bool TryGetViewBindableProperty(VisualElement view, string propertyName, [NotNullWhen(true)] out BindableProperty? viewProperty)
	{
		var field = view.GetType().GetField(propertyName + "Property", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
		viewProperty = field?.GetValue(null) as BindableProperty;
		return viewProperty is not null;
	}

	async ValueTask UpdateStateAsync(VisualElement? view, ValidationFlags flags, bool isForced, CancellationToken? parentToken = null)
	{
		parentToken?.ThrowIfCancellationRequested();

		if ((view?.IsFocused ?? false) && flags.HasFlag(ValidationFlags.ForceMakeValidWhenFocused))
		{
			IsRunning = true;

			ResetValidationTokenSource(null);

			IsValid = true;
			IsRunning = false;
		}
		else if (isForced || (currentStatus != ValidationFlags.None && flags.HasFlag(currentStatus)))
		{
			IsRunning = true;
			hasValidated = true;

			using var tokenSource = new CancellationTokenSource();
			var token = parentToken ?? tokenSource.Token;
			ResetValidationTokenSource(tokenSource);

			try
			{
				var isValid = await ValidateAsync(Decorate(Value), token);

				if (token.IsCancellationRequested)
				{
					StopRunningUnlessSuperseded(tokenSource);
					return;
				}

				validationTokenSource = null;
				IsValid = isValid;
				IsRunning = false;
			}
			catch (TaskCanceledException)
			{
				StopRunningUnlessSuperseded(tokenSource);
				return;
			}
			catch (Exception ex) when (Options.ShouldSuppressExceptionsInBehaviors)
			{
				validationTokenSource = null;
				IsValid = false;
				IsRunning = false;

				Trace.TraceInformation("{0}", ex);
			}
			catch (OperationCanceledException)
			{
				// Not swallowed like TaskCanceledException above, so that the caller still sees it
				StopRunningUnlessSuperseded(tokenSource);
				throw;
			}
		}

		if (view is not null)
		{
			TryUpdateStyle(view, IsValid);
		}
	}

	bool TryUpdateStyle(in VisualElement view, bool isValid)
	{
		try
		{
			VisualStateManager.GoToState(view, isValid ? ValidVisualState : InvalidVisualState);

			view.Style = (isValid ? ValidStyle : InvalidStyle) ?? view.Style;

			return true;
		}
		catch (ArgumentException)
		{
			return false;
		}
	}

	void ResetValidationTokenSource(CancellationTokenSource? newTokenSource)
	{
		try
		{
			validationTokenSource?.Cancel();
			validationTokenSource?.Dispose();
		}
		catch (ObjectDisposedException)
		{
		}
		finally
		{
			validationTokenSource = newTokenSource;
		}
	}

	// A newer validation that replaced this one in validationTokenSource owns IsRunning; otherwise, as after a cancellation through the caller's token, nothing else resets it
	// Checking tokenSource.IsCancellationRequested is not enough: a newer validation cancels nothing when an older validation that finished has already cleared validationTokenSource
	void StopRunningUnlessSuperseded(CancellationTokenSource tokenSource)
	{
		if (validationTokenSource != tokenSource)
		{
			return;
		}

		validationTokenSource = null;
		IsRunning = false;
	}
}

/// <inheritdoc />
public abstract class ValidationBehavior<T> : ValidationBehavior
{
	/// <summary>
	/// The value to validate. This is a bindable property.
	/// </summary>
	public new T? Value
	{
		get => (T?)GetValue(ValueProperty);
		set => SetValue(ValueProperty, value);
	}

	/// <summary>
	/// Decorate value
	/// </summary>
	/// <remarks>
	/// Called before each validation: <see cref="ValidateAsync(T, CancellationToken)"/> receives the value that this method returns, for example the text after <see cref="TextValidationBehavior.DecorationFlags"/> are applied.
	/// The base implementation returns <paramref name="value"/> unchanged.
	/// </remarks>
	protected virtual T? Decorate(T? value) => value;

	/// <inheritdoc />
	/// <remarks>Casts <paramref name="value"/> to <typeparamref name="T"/> and returns the result of <see cref="Decorate(T)"/>.</remarks>
	protected override object? Decorate(object? value) => Decorate((T?)value);

	/// <summary>
	/// Validate value
	/// </summary>
	protected abstract ValueTask<bool> ValidateAsync(T? value, CancellationToken token);

	/// <inheritdoc />
	protected override ValueTask<bool> ValidateAsync(object? value, CancellationToken token) => ValidateAsync((T?)value, token);
}