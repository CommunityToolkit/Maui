using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// Abstract class for our behaviors to inherit.
/// </summary>
/// <typeparam name="TView">The <see cref="VisualElement"/> that the behavior can be applied to</typeparam>
public abstract class BaseBehavior<TView> : Behavior<TView> where TView : VisualElement
{
	static readonly MethodInfo? getContextMethod
		= typeof(BindableObject).GetRuntimeMethods()?.FirstOrDefault(m => m.Name is "GetContext");

	static readonly FieldInfo? bindingField
		= getContextMethod?.ReturnType.GetRuntimeField("Binding");

	BindingBase? defaultBindingContextBinding;

	/// <summary>
	/// View used by the Behavior
	/// </summary>
	protected TView? View { get; private set; }

	[MemberNotNullWhen(true, nameof(defaultBindingContextBinding))]
	internal bool TrySetBindingContext(Binding binding)
	{
		if (!IsBound(BindingContextProperty))
		{
			SetBinding(BindingContextProperty, defaultBindingContextBinding = binding);
			return true;
		}

		return false;
	}

	internal bool TryRemoveBindingContext()
	{
		if (defaultBindingContextBinding != null)
		{
			RemoveBinding(BindingContextProperty);
			defaultBindingContextBinding = null;

			return true;
		}

		return false;
	}

	/// <summary>
	/// Virtual method that executes when a property on the View has changed
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	protected virtual void OnViewPropertyChanged(TView sender, PropertyChangedEventArgs e)
	{

	}

	/// <inheritdoc/>
	[MemberNotNull(nameof(View))]
	protected override void OnAttachedTo(TView bindable)
	{
		base.OnAttachedTo(bindable);

		View = bindable;
		bindable.PropertyChanged += OnViewPropertyChanged;

		TrySetBindingContext(new Binding
		{
			Path = BindingContextProperty.PropertyName,
			Source = bindable
		});
	}

	/// <inheritdoc/>
	protected override void OnDetachingFrom(TView bindable)
	{
		base.OnDetachingFrom(bindable);

		TryRemoveBindingContext();

		bindable.PropertyChanged -= OnViewPropertyChanged;

		View = null;
	}

	/// <summary>
	/// Virtual method that executes when a binding context is set
	/// </summary>
	/// <param name="property"></param>
	/// <param name="defaultBinding"></param>
	/// <returns></returns>
	[MemberNotNullWhen(true, nameof(bindingField), nameof(getContextMethod))]
	protected bool IsBound(BindableProperty property, BindingBase? defaultBinding = null)
	{
		var context = getContextMethod?.Invoke(this, new object[] { property });
		return context != null
			&& bindingField?.GetValue(context) is BindingBase binding
			&& binding != defaultBinding;
	}

	void OnViewPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (sender is not TView view)
		{
			throw new ArgumentException($"Behavior Cann Only Be Attached to {typeof(TView)}");
		}

		OnViewPropertyChanged(view, e);
	}
}