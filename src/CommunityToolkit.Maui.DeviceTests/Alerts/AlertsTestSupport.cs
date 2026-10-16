using CommunityToolkit.Maui.Alerts;

namespace CommunityToolkit.Maui.DeviceTests;

// A minimal application handler. The toolkit reads Application.Current.Handler.MauiContext.Services (for example IFontManager),
// which the headless runner's application does not provide on Apple platforms and which a test-owned application never has.
sealed class StubApplicationHandler(IMauiContext? mauiContext) : IElementHandler
{
	public object? PlatformView => this;

	public IElement? VirtualView { get; private set; }

	public IMauiContext? MauiContext => mauiContext;

	public void SetMauiContext(IMauiContext mauiContext)
	{
		// The context is fixed by the constructor.
	}

	public void SetVirtualView(IElement view) => VirtualView = view;

	public void UpdateValue(string property)
	{
	}

	public void Invoke(string command, object? args = null)
	{
	}

	public void DisconnectHandler()
	{
	}
}

// A minimal view handler: Controls views only accept an IViewHandler. Its platform view is the handler itself, which is not a native
// view on any platform, so code that casts the platform view of an anchor or of a modal page to a native view gets null.
sealed class StubViewHandler(IMauiContext? mauiContext) : IViewHandler
{
	public object PlatformView => this;

	public object? ContainerView => null;

	public bool HasContainer
	{
		get => false;
		set
		{
		}
	}

	public IView? VirtualView { get; private set; }

	IElement? IElementHandler.VirtualView => VirtualView;

	public IMauiContext? MauiContext => mauiContext;

	object? IElementHandler.PlatformView => PlatformView;

	public Size GetDesiredSize(double widthConstraint, double heightConstraint) => Size.Zero;

	public void PlatformArrange(Rect frame)
	{
	}

	public void SetMauiContext(IMauiContext mauiContext)
	{
		// The context is fixed by the constructor.
	}

	public void SetVirtualView(IElement view) => VirtualView = view as IView;

	public void UpdateValue(string property)
	{
	}

	public void Invoke(string command, object? args = null)
	{
	}

	public void DisconnectHandler()
	{
	}
}

// A view that is not a Controls view (Controls views require a platform view handler as soon as one is set): only its handler matters.
sealed class StubView : IView
{
	public IViewHandler? Handler { get; set; }

	IElementHandler? IElement.Handler
	{
		get => Handler;
		set => Handler = value as IViewHandler;
	}

	public IElement? Parent => null;

	public string AutomationId => string.Empty;

	public FlowDirection FlowDirection => FlowDirection.MatchParent;

	public Microsoft.Maui.Primitives.LayoutAlignment HorizontalLayoutAlignment => Microsoft.Maui.Primitives.LayoutAlignment.Fill;

	public Microsoft.Maui.Primitives.LayoutAlignment VerticalLayoutAlignment => Microsoft.Maui.Primitives.LayoutAlignment.Fill;

	public Semantics? Semantics => null;

	public IShape? Clip => null;

	public IShadow? Shadow => null;

	public bool IsEnabled => true;

	public bool IsFocused { get; set; }

	public Visibility Visibility => Visibility.Visible;

	public double Opacity => 1;

	public Paint? Background => null;

	public Rect Frame { get; set; }

	public double Width => -1;

	public double MinimumWidth => 0;

	public double MaximumWidth => double.PositiveInfinity;

	public double Height => -1;

	public double MinimumHeight => 0;

	public double MaximumHeight => double.PositiveInfinity;

	public Thickness Margin => Thickness.Zero;

	public Size DesiredSize => Size.Zero;

	public int ZIndex => 0;

	public bool InputTransparent => false;

	public double TranslationX => 0;

	public double TranslationY => 0;

	public double Scale => 1;

	public double ScaleX => 1;

	public double ScaleY => 1;

	public double Rotation => 0;

	public double RotationX => 0;

	public double RotationY => 0;

	public double AnchorX => 0.5;

	public double AnchorY => 0.5;

	public Size Arrange(Rect bounds) => Size.Zero;

	public Size Measure(double widthConstraint, double heightConstraint) => Size.Zero;

	public void InvalidateMeasure()
	{
	}

	public void InvalidateArrange()
	{
	}

	public bool Focus() => false;

	public void Unfocus()
	{
	}
}

// Records the static Snackbar.Shown and Snackbar.Dismissed events. The toolkit keeps its handlers in a WeakEventManager, so the
// handlers are instance methods of this object, and the test keeps the recorder alive until it is disposed.
// afterDismissed runs inside the Dismissed handler, after the event was recorded (for example to cancel a token while Show is
// dismissing the snackbar that is shown).
sealed class SnackbarEventRecorder : IDisposable
{
	readonly Action? afterDismissed;

	public SnackbarEventRecorder(Action? afterDismissed = null)
	{
		this.afterDismissed = afterDismissed;
		Snackbar.Shown += OnShown;
		Snackbar.Dismissed += OnDismissed;
	}

	public int ShownCount { get; private set; }

	public int DismissedCount { get; private set; }

	public List<string> Sequence { get; } = [];

	public void Dispose()
	{
		Snackbar.Shown -= OnShown;
		Snackbar.Dismissed -= OnDismissed;
	}

	void OnShown(object? sender, EventArgs e)
	{
		ShownCount++;
		Sequence.Add(nameof(Snackbar.Shown));
	}

	void OnDismissed(object? sender, EventArgs e)
	{
		DismissedCount++;
		Sequence.Add(nameof(Snackbar.Dismissed));
		afterDismissed?.Invoke();
	}
}