using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// A common interface to be used across <see cref="BaseBehavior{TView}"/> and <see cref="BasePlatformBehavior{TView,TPlatformView}"/>
/// </summary>
public interface ICommunityToolkitBehavior<TView> where TView : Element
{
	/// <summary>
	/// View used by the Behavior
	/// </summary>
	protected TView? View { get; set; }

	internal bool TrySetBindingContext(in BindableObject bindable, in Binding binding)
	{
		if (bindable.IsSet(BindableObject.BindingContextProperty))
		{
			return false;
		}

		bindable.SetBinding(BindableObject.BindingContextProperty, binding);
		return true;

	}

	internal bool TryRemoveBindingContext(in BindableObject bindable)
	{
		if (bindable.IsSet(BindableObject.BindingContextProperty))
		{
			bindable.RemoveBinding(BindableObject.BindingContextProperty);
			return true;
		}

		return false;
	}

	[MemberNotNull(nameof(View))]
	internal void AssignViewAndBingingContext(TView bindable)
	{
		View = bindable;
		bindable.PropertyChanged += OnViewPropertyChanged;

		TrySetBindingContext(bindable, new Binding
		{
			Path = BindableObject.BindingContextProperty.PropertyName,
			Source = bindable
		});
	}

	internal void UnassignViewAndBingingContext(TView bindable)
	{
		TryRemoveBindingContext(bindable);

		bindable.PropertyChanged -= OnViewPropertyChanged;

		View = null;
	}

	internal void OnViewPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (sender is not TView view)
		{
			throw new ArgumentException($"Behavior can only be attached to {typeof(TView)}");
		}

		try
		{
			OnViewPropertyChanged(view, e);
		}
		catch (Exception ex) when (Options.ShouldSuppressExceptionsInBehaviors)
		{
			Trace.WriteLine(ex);
		}
	}

	/// <summary>
	/// Virtual method that executes when a property on the View has changed
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	protected void OnViewPropertyChanged(TView sender, PropertyChangedEventArgs e);
}