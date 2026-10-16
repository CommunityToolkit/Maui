using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.Core.Views;

namespace CommunityToolkit.Maui.Views;

/// <summary>
/// The DrawingView allows you to draw one or multiple lines on a canvas.
/// </summary>
/// <remarks>
/// While the <see cref="DrawingView"/> is unloaded (for example while its page is not shown), it keeps its handler, so that it can be removed from
/// its layout or added to another one meanwhile, but the handler stops listening to the platform view and to <see cref="Lines"/>, so that
/// a <see cref="Lines"/> collection that the app keeps does not keep the view alive. Changes to the other properties still reach the platform view.
/// When the <see cref="DrawingView"/> is loaded again, its handler listens again and the platform view shows the current <see cref="Lines"/>,
/// including the lines added or removed while the <see cref="DrawingView"/> was unloaded.
/// </remarks>
public partial class DrawingView : View, IDrawingView
{
	readonly WeakEventManager drawingViewEventManager = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="DrawingView"/> class.
	/// </summary>
	public DrawingView()
	{
		BackgroundColor = DrawingViewDefaults.BackgroundColor;
		Loaded += OnDrawingViewLoaded;
		Unloaded += OnDrawingViewUnloaded;
	}

	/// <summary>
	/// Event occurred when drawing line completed.
	/// </summary>
	public event EventHandler<DrawingLineCompletedEventArgs> DrawingLineCompleted
	{
		add => drawingViewEventManager.AddEventHandler(value);
		remove => drawingViewEventManager.RemoveEventHandler(value);
	}

	/// <summary>
	/// Event occurred when drawing line started.
	/// </summary>
	public event EventHandler<DrawingLineStartedEventArgs> DrawingLineStarted
	{
		add => drawingViewEventManager.AddEventHandler(value);
		remove => drawingViewEventManager.RemoveEventHandler(value);
	}

	/// <summary>
	/// Event occurred when drawing line canceled.
	/// </summary>
	public event EventHandler<EventArgs> DrawingLineCancelled
	{
		add => drawingViewEventManager.AddEventHandler(value);
		remove => drawingViewEventManager.RemoveEventHandler(value);
	}

	/// <summary>
	/// Event occurred when drawing.
	/// </summary>
	public event EventHandler<PointDrawnEventArgs> PointDrawn
	{
		add => drawingViewEventManager.AddEventHandler(value);
		remove => drawingViewEventManager.RemoveEventHandler(value);
	}

	/// <summary>
	/// Gets or sets the action that allows drawing on the <see cref="IDrawingView"/>.
	/// </summary>
	/// <remarks>
	/// The action draws above the background and below the lines, in a canvas state of its own: the fill paint of the background is not set on
	/// the canvas that the action receives, and the canvas state is restored after the action, so the fill paint, stroke settings, transforms and
	/// other canvas state that the action sets do not apply to the lines.
	/// </remarks>
	[BindableProperty]
	public partial Action<ICanvas, RectF>? DrawAction { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the drawing surface should be cleared when the drawing operation finishes.
	/// </summary>
	[BindableProperty]
	public partial bool ShouldClearOnFinish { get; set; } = DrawingViewDefaults.ShouldClearOnFinish;

	/// <summary>
	/// Gets or sets a value indicating whether multi-line mode is enabled.
	/// </summary>
	[BindableProperty]
	public partial bool IsMultiLineModeEnabled { get; set; } = DrawingViewDefaults.IsMultiLineModeEnabled;

	/// <summary>
	/// Gets or sets the collection of lines that are currently on the <see cref="DrawingView"/>.
	/// </summary>
	[BindableProperty(DefaultBindingMode = BindingMode.TwoWay)]
	public partial ObservableCollection<IDrawingLine> Lines { get; set; } = [];

	/// <summary>
	/// This command is invoked whenever the drawing of a line on <see cref="DrawingView"/> has completed.
	/// This is a bindable property.
	/// </summary>
	[BindableProperty]
	public partial ICommand? DrawingLineCompletedCommand { get; set; }

	/// <summary>
	/// This command is invoked whenever the drawing of a line on <see cref="DrawingView"/> has started.
	/// </summary>
	[BindableProperty]
	public partial ICommand? DrawingLineStartedCommand { get; set; }

	/// <summary>
	/// This command is invoked whenever the drawing of a line on <see cref="DrawingView"/> has canceled.
	/// </summary>
	[BindableProperty]
	public partial ICommand? DrawingLineCancelledCommand { get; set; }

	/// <summary>
	/// This command is invoked whenever the drawing on <see cref="DrawingView"/>.
	/// </summary>
	[BindableProperty]
	public partial ICommand? PointDrawnCommand { get; set; }

	/// <summary>
	/// The <see cref="Color"/> that is used by default to draw a line on the <see cref="DrawingView"/>. This is a bindable property.
	/// </summary>
	[BindableProperty]
	public partial Color LineColor { get; set; } = DrawingViewDefaults.LineColor;

	/// <summary>
	/// The width that is used by default to draw a line on the <see cref="DrawingView"/>. This is a bindable property.
	/// </summary>
	[BindableProperty]
	public partial float LineWidth { get; set; } = DrawingViewDefaults.LineWidth;

	/// <summary>
	/// Retrieves a <see cref="Stream"/> containing an image of the collection of <see cref="IDrawingLine"/> that is provided as a parameter.
	/// </summary>
	/// <param name="options">The options controlling how the resulting image is generated.</param>
	/// <param name="token"><see cref="CancellationToken"/></param>
	/// <returns><see cref="ValueTask{Stream}"/> containing the data of the requested image with data that's provided through the <paramref name="options"/> parameter, or <see cref="Stream.Null"/> when there is nothing to draw (no lines, or no line with points).</returns>
	public static ValueTask<Stream> GetImageStream(
		ImageLineOptions options,
		CancellationToken token = default) =>
		DrawingViewService.GetImageStream(options, token);

	/// <inheritdoc cref="IDrawingView.GetImageStream(double, double, DrawingViewOutputOption, CancellationToken)"/>
	public ValueTask<Stream> GetImageStream(double desiredWidth, double desiredHeight, CancellationToken token = default) =>
		GetImageStream(desiredWidth, desiredHeight, DrawingViewOutputOption.Lines, token);

	/// <inheritdoc cref="IDrawingView.GetImageStream(double, double, DrawingViewOutputOption, CancellationToken)"/>
	public ValueTask<Stream> GetImageStream(double desiredWidth, double desiredHeight, DrawingViewOutputOption imageOutputOption, CancellationToken token = default)
	{
		var options = imageOutputOption == DrawingViewOutputOption.Lines
			? ImageLineOptions.JustLines(Lines.ToList(), new Size(desiredWidth, desiredHeight), Background)
			: ImageLineOptions.FullCanvas(Lines.ToList(), new Size(desiredWidth, desiredHeight), Background,
				new Size(this.Width, this.Height));

		return DrawingViewService.GetImageStream(options, token);
	}

	/// <summary>
	/// Clears the <see cref="Lines"/> collection.
	/// </summary>
	public void Clear()
	{
		Lines.Clear();
	}

	/// <summary>
	/// Executes DrawingLineCompleted event and DrawingLineCompletedCommand
	/// </summary>
	/// <param name="lastDrawingLine">Last drawing line</param>
	void IDrawingView.OnDrawingLineCompleted(IDrawingLine lastDrawingLine)
	{
		drawingViewEventManager.HandleEvent(this, new DrawingLineCompletedEventArgs(lastDrawingLine), nameof(DrawingLineCompleted));

		if (DrawingLineCompletedCommand?.CanExecute(lastDrawingLine) ?? false)
		{
			DrawingLineCompletedCommand.Execute(lastDrawingLine);
		}
	}

	/// <summary>
	/// Executes DrawingLineCancelled event and DrawingLineCancelledCommand
	/// </summary>
	void IDrawingView.OnDrawingLineCancelled()
	{
		drawingViewEventManager.HandleEvent(this, EventArgs.Empty, nameof(DrawingLineCancelled));

		if (DrawingLineCancelledCommand?.CanExecute(null) is true)
		{
			DrawingLineCancelledCommand.Execute(null);
		}
	}

	/// <summary>
	/// Executes DrawingLineStarted event and DrawingLineStartedCommand
	/// </summary>
	void IDrawingView.OnDrawingLineStarted(PointF point)
	{
		drawingViewEventManager.HandleEvent(this, new DrawingLineStartedEventArgs(point), nameof(DrawingLineStarted));

		if (DrawingLineStartedCommand?.CanExecute(point) is true)
		{
			DrawingLineStartedCommand.Execute(point);
		}
	}

	/// <summary>
	/// Executes PointDrawn event and PointDrawnCommand
	/// </summary>
	void IDrawingView.OnPointDrawn(PointF point)
	{
		drawingViewEventManager.HandleEvent(this, new PointDrawnEventArgs(point), nameof(PointDrawn));

		if (PointDrawnCommand?.CanExecute(point) is true)
		{
			PointDrawnCommand.Execute(point);
		}
	}

	// While the DrawingView is out of the window, its handler does not listen to the platform view and to Lines (#420), so that the platform view and
	// the Lines, which the app may keep, do not keep each other alive. The DrawingView keeps its handler: MAUI needs it to remove the DrawingView from its
	// layout (LayoutHandler.Remove) while its page is away. The DrawingView keeps listening, as it can leave the window again.
	// A handler of another type is disconnected, as before.
	void OnDrawingViewUnloaded(object? sender, EventArgs e)
	{
		if (Handler is DrawingViewHandler drawingViewHandler)
		{
			drawingViewHandler.StopListeningUntilLoaded();
		}
		else
		{
			Handler?.DisconnectHandler();
		}
	}

	// The handler listens again once the DrawingView is back in a window (its page is shown again). DrawingViewHandler also does this when its platform view
	// is in a window again; Loaded also covers an Unloaded that arrives after that (MAUI then raises Loaded right after it).
	void OnDrawingViewLoaded(object? sender, EventArgs e)
	{
		if (Handler is DrawingViewHandler drawingViewHandler)
		{
			drawingViewHandler.ListenAgain();
		}
	}
}