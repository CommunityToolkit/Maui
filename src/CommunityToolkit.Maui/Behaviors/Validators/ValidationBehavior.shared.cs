using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;

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
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
[RequiresUnreferencedCode($"{nameof(ValidationBehavior)} is not trim safe because it uses bindings with string paths.")]
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
	protected override async void OnAttachedTo(VisualElement bindable)
	{
		base.OnAttachedTo(bindable);

		await isAttachingSemaphoreSlim.WaitAsync();

		try
		{
			currentStatus = ValidationFlags.ValidateOnAttaching;

			OnValuePropertyNamePropertyChanged();
			await UpdateStateAsync(View, Flags, false).ConfigureAwait(false);
		}
		finally
		{
			isAttachingSemaphoreSlim.Release();
		}
	}

	/// <inheritdoc/>
	protected override void OnDetachingFrom(VisualElement bindable)
	{
		RemoveBinding(ValueProperty);

		currentStatus = ValidationFlags.None;
		base.OnDetachingFrom(bindable);
	}

	/// <inheritdoc/>
	protected override async void OnViewPropertyChanged(VisualElement sender, PropertyChangedEventArgs e)
	{
		base.OnViewPropertyChanged(sender, e);

		if (e.PropertyName == VisualElement.IsFocusedProperty.PropertyName)
		{
			currentStatus = sender.IsFocused switch
			{
				true => ValidationFlags.ValidateOnFocused,
				false => ValidationFlags.ValidateOnUnfocused
			};

			await UpdateStateAsync(View, Flags, false).ConfigureAwait(false);
		}
	}

	/// <summary>
	/// Used when a property in <see cref="ValidationBehavior"/> changes to update the validation state
	/// </summary>
	/// <param name="bindable"></param>
	/// <param name="oldValue"></param>
	/// <param name="newValue"></param>
	protected static async void OnValidationPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var validationBehavior = (ValidationBehavior)bindable;
		await validationBehavior.UpdateStateAsync(validationBehavior.View, validationBehavior.Flags, false).ConfigureAwait(false);
	}

	static void OnIsValidPropertyChanged(BindableObject bindable, object oldValue, object newValue)
		=> ((ValidationBehavior)bindable).OnIsValidPropertyChanged();

	static async void OnValuePropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		await ((ValidationBehavior)bindable).OnValuePropertyChanged(CancellationToken.None);
		OnValidationPropertyChanged(bindable, oldValue, newValue);
	}

	static void OnValuePropertyNamePropertyChanged(BindableObject bindable, object oldValue, object newValue)
		=> ((ValidationBehavior)bindable).OnValuePropertyNamePropertyChanged();

	static object GetDefaultForceValidateCommand(BindableObject bindable)
		=> ((ValidationBehavior)bindable).DefaultForceValidateCommand;

	static object GetDefaultValuePropertyName(BindableObject bindable)
		=> ((ValidationBehavior)bindable).DefaultValuePropertyName;

	void OnIsValidPropertyChanged() => IsNotValid = !IsValid;

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
		SetBinding(ValueProperty, new Binding
		{
			Path = ValuePropertyName,
			Source = View
		});
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
		else if (isForced || (currentStatus != ValidationFlags.None && Flags.HasFlag(currentStatus)))
		{
			IsRunning = true;

			using var tokenSource = new CancellationTokenSource();
			var token = parentToken ?? tokenSource.Token;
			ResetValidationTokenSource(tokenSource);

			try
			{
				var isValid = await ValidateAsync(Decorate(Value), token);

				if (token.IsCancellationRequested)
				{
					return;
				}

				validationTokenSource = null;
				IsValid = isValid;
				IsRunning = false;
			}
			catch (TaskCanceledException)
			{
				return;
			}
			catch (Exception ex) when (Options.ShouldSuppressExceptionsInBehaviors)
			{
				validationTokenSource = null;
				IsValid = false;
				IsRunning = false;

				Trace.TraceInformation("{0}", ex);
			}
		}

		if (view is not null)
		{
			UpdateStyle(view, IsValid);
		}
	}

	void UpdateStyle(in VisualElement view, bool isValid)
	{
		VisualStateManager.GoToState(view, isValid ? ValidVisualState : InvalidVisualState);

		view.Style = (isValid ? ValidStyle : InvalidStyle) ?? view.Style;
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
}

/// <inheritdoc />
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
[RequiresUnreferencedCode($"{nameof(ValidationBehavior)} is not trim safe because it uses bindings with string paths.")]
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
	protected virtual T? Decorate(T? value) => value;

	/// <inheritdoc />
	protected override object? Decorate(object? value) => (T?)value;

	/// <summary>
	/// Validate value
	/// </summary>
	protected abstract ValueTask<bool> ValidateAsync(T? value, CancellationToken token);

	/// <inheritdoc />
	protected override ValueTask<bool> ValidateAsync(object? value, CancellationToken token) => ValidateAsync((T?)value, token);
}