using System.ComponentModel;
using System.Reflection;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// Abstract class for our behaviors to inherit.
/// </summary>
/// <typeparam name="TView">The <see cref="VisualElement"/> that the behavior can be applied to</typeparam>
public abstract class BaseBehavior<TView> : Behavior<TView>, ICommunityToolkitBehavior<TView> where TView : VisualElement
{
	private protected BaseBehavior()
	{

	}

	/// <summary>
	/// View used by the Behavior
	/// </summary>
	protected TView? View { get; private set; }

	TView? ICommunityToolkitBehavior<TView>.View
	{
		get => View;
		set => View = value;
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
	protected override void OnAttachedTo(TView bindable)
	{
		base.OnAttachedTo(bindable);

		((ICommunityToolkitBehavior<TView>)this).AssignViewAndBingingContext(bindable);
	}

	/// <inheritdoc/>
	protected override void OnDetachingFrom(TView bindable)
	{
		base.OnDetachingFrom(bindable);

		((ICommunityToolkitBehavior<TView>)this).UnassignViewAndBingingContext(bindable);
	}

	/// <summary>
	/// Virtual method that executes when a binding context is set
	/// </summary>
	/// <param name="property"></param>
	/// <param name="defaultBinding"></param>
	/// <returns></returns>
	[EditorBrowsable(EditorBrowsableState.Never)]
	[Obsolete($"{nameof(IsBound)} is no longer used by {nameof(CommunityToolkit)}.{nameof(CommunityToolkit.Maui)} and will be removed in a future release")]
	protected bool IsBound(BindableProperty property, BindingBase? defaultBinding = null)
	{
		var getContextMethod = typeof(BindableObject).GetRuntimeMethods().FirstOrDefault(m => m.Name is "GetContext");
		var bindingField = getContextMethod?.ReturnType.GetRuntimeField("Binding");

		var context = getContextMethod?.Invoke(this, [property]);
		return context is not null
			&& bindingField?.GetValue(context) is BindingBase binding
			&& binding != defaultBinding;
	}

	void ICommunityToolkitBehavior<TView>.OnViewPropertyChanged(TView sender, PropertyChangedEventArgs e) => OnViewPropertyChanged(sender, e);
}