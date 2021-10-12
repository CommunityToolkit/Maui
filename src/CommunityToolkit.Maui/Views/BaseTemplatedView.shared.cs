using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Views.Internals
{
	/// <summary>
	/// Abstract class that templated views should inherit
	/// </summary>
	/// <typeparam name="TControl">The type of the control that this template will be used for</typeparam>
	public abstract class BaseTemplatedView<TControl> : TemplatedView where TControl : View, new()
	{
		protected TControl? Control { get; private set; }

		/// <summary>
		/// Constructor of <see cref="BaseTemplatedView{TControl}" />
		/// </summary>
		public BaseTemplatedView()
			=> ControlTemplate = new ControlTemplate(typeof(TControl));

		protected override void OnBindingContextChanged()
		{
			base.OnBindingContextChanged();

			if (Control != null)
				Control.BindingContext = BindingContext;
		}

		protected override void OnChildAdded(Element child)
		{
			if (Control == null && child is TControl control)
			{
				Control = control;
				OnControlInitialized(Control);
			}

			base.OnChildAdded(child);
		}

		protected abstract void OnControlInitialized(TControl control);
	}
}