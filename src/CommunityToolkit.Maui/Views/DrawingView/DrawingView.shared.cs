using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;

namespace CommunityToolkit.Maui.Views;

/// <summary>
/// The DrawingView allows you to draw one or multiple lines on a canvas.
/// </summary>
public partial class DrawingView : View, IDrawingView
{
	readonly WeakEventManager drawingViewEventManager = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="DrawingView"/> class.
	/// </summary>
	public DrawingView()
	{
		BackgroundColor = DrawingViewDefaults.BackgroundColor;
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
	/// <returns><see cref="ValueTask{Stream}"/> containing the data of the requested image with data that's provided through the <paramref name="options"/> parameter.</returns>
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

	void OnDrawingViewUnloaded(object? sender, EventArgs e)
	{
		Unloaded -= OnDrawingViewUnloaded;
		Handler?.DisconnectHandler();
	}
}