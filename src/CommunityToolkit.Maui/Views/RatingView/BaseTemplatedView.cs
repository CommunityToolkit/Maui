namespace CommunityToolkit.Maui.Views.RatingView;

///<summary>A Generic view based ont the <see cref="TemplatedView"/> to display controls.</summary>
public abstract class BaseTemplatedView<TControl> : TemplatedView where TControl : View, new()
{
	///<summary>The control to be displayed</summary>
	public TControl? Control { get; private set; }

	///<summary>the default constructor.</summary>
	protected BaseTemplatedView()
		=> ControlTemplate = new ControlTemplate(typeof(TControl));

	///<summary>Method called everytime the control's Binding Context is changed.</summary>
	protected override void OnBindingContextChanged()
	{
		base.OnBindingContextChanged();

		if (Control != null)
		{
			Control.BindingContext = BindingContext;
		}
	}

	///<summary>Called everytime a child is added to the control.</summary>
	protected override void OnChildAdded(Element child)
	{
		if (Control is null && child is TControl control)
		{
			Control = control;
			OnControlInitialized(Control);
		}

		base.OnChildAdded(child);
	}

	///<summary>Called when the control is initialized.</summary>
	protected abstract void OnControlInitialized(TControl control);
}