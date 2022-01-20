using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WRect = Windows.Foundation.Rect;
using Microsoft.Maui.Controls.Platform;

namespace CommunityToolkit.Core.Extensions.Workarounds;

class WrapperControl : Panel
{
	readonly View view;
	IView View => view;
	INativeViewHandler? Handler => View.Handler as INativeViewHandler;

	FrameworkElement FrameworkElement { get; }

	internal void CleanUp()
	{
		//view?.Cleanup();

		if (view is not null)
		{
			view.MeasureInvalidated -= OnMeasureInvalidated;
		}
	}

	public WrapperControl(View view, IMauiContext mauiContext)
	{
		this.view = view;
		this.view.MeasureInvalidated += OnMeasureInvalidated;

		//TODO: When p12 releases uncomment this line
		//FrameworkElement = view.ToNative(mauiContext, true);
		FrameworkElement = view.ToNative(mauiContext);
		Children.Add(FrameworkElement);

		// make sure we re-measure once the template is applied

		FrameworkElement.Loaded += (sender, args) =>
		{
			// If the view is a layout (stacklayout, grid, etc) we need to trigger a layout pass
			// with all the controls in a consistent native state (i.e., loaded) so they'll actually
			// have Bounds set
			Handler?.NativeView?.InvalidateMeasure(View);
			InvalidateMeasure();
		};
	}

	void OnMeasureInvalidated(object? sender, EventArgs e)
	{
		InvalidateMeasure();
	}

	protected override global::Windows.Foundation.Size ArrangeOverride(global::Windows.Foundation.Size finalSize)
	{
		view.IsInNativeLayout = true;
		view.Frame = new Rectangle(0, 0, finalSize.Width, finalSize.Height);
		FrameworkElement?.Arrange(new WRect(0, 0, finalSize.Width, finalSize.Height));

		if (view.Width <= 0 || view.Height <= 0)
		{
			// Hide Panel when size _view is empty.
			// It is necessary that this element does not overlap other elements when it should be hidden.
			Opacity = 0;
		}
		else
		{
			Opacity = 1;
		}

		view.IsInNativeLayout = false;

		return finalSize;
	}

	protected override global::Windows.Foundation.Size MeasureOverride(global::Windows.Foundation.Size availableSize)
	{
		FrameworkElement.Measure(availableSize);

		var request = FrameworkElement.DesiredSize;

		if (request.Height < 0)
		{
			request.Height = availableSize.Height;
		}

		global::Windows.Foundation.Size result;
		if (view.HorizontalOptions.Alignment == Microsoft.Maui.Controls.LayoutAlignment.Fill && !double.IsInfinity(availableSize.Width) && availableSize.Width != 0)
		{
			result = new global::Windows.Foundation.Size(availableSize.Width, request.Height);
		}
		else
		{
			result = request;
		}

		return result;
	}
}
