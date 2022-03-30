namespace CommunityToolkit.Maui.Views;

/// <summary>
/// Abstract class that templated views should inherit
/// </summary>
/// <typeparam name="TControl">The type of the control that this template will be used for</typeparam>
public abstract class BaseTemplatedView<TControl> : TemplatedView where TControl : View, new()
{
	/// <summary>
	/// Gets the <see cref="TControl"/> added as child.
	/// </summary>
	protected TControl? Control { get; private set; }

	/// <summary>
	/// Constructor of <see cref="BaseTemplatedView{TControl}" />
	/// </summary>
	public BaseTemplatedView()
		=> ControlTemplate = new ControlTemplate(typeof(TControl));

	/// <inheritdoc />
	protected override void OnBindingContextChanged()
	{
		base.OnBindingContextChanged();

		if (Control != null)
		{
			Control.BindingContext = BindingContext;
		}
	}

	/// <inheritdoc />
	protected override void OnChildAdded(Element child)
	{
		if (Control == null && child is TControl control)
		{
			Control = control;
			OnControlInitialized(Control);
		}

		base.OnChildAdded(child);
	}

	/// <summary>
	/// Called when <see cref="OnChildAdded"/> is called and the child is a <see cref="TControl"/>.
	/// </summary>
	/// <param name="control">The added child</param>
	protected abstract void OnControlInitialized(TControl control);
}