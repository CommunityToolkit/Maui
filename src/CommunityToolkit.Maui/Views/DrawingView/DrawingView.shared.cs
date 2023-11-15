using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;

namespace CommunityToolkit.Maui.Views;

/// <summary>
/// The DrawingView allows you to draw one or multiple lines on a canvas.
/// </summary>
public class DrawingView : View, IDrawingView
{
	readonly WeakEventManager drawingViewEventManager = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="DrawingView"/> class.
	/// </summary>
	public DrawingView()
	{
		Unloaded += OnDrawingViewUnloaded;
	}

	/// <summary>
	/// Backing BindableProperty for the <see cref="DrawAction"/> property.
	/// </summary>
	public static readonly BindableProperty DrawActionProperty =
		BindableProperty.Create(nameof(DrawAction), typeof(Action<ICanvas, RectF>), typeof(DrawingView));

	/// <summary>
	/// Backing BindableProperty for the <see cref="ShouldClearOnFinish"/> property.
	/// </summary>
	public static readonly BindableProperty ShouldClearOnFinishProperty =
		BindableProperty.Create(nameof(ShouldClearOnFinish), typeof(bool), typeof(DrawingView), DrawingViewDefaults.ShouldClearOnFinish);

	/// <summary>
	/// Backing BindableProperty for the <see cref="IsMultiLineModeEnabled"/> property.
	/// </summary>
	public static readonly BindableProperty IsMultiLineModeEnabledProperty =
		BindableProperty.Create(nameof(IsMultiLineModeEnabled), typeof(bool), typeof(DrawingView), DrawingViewDefaults.IsMultiLineModeEnabled);

	/// <summary>
	/// Backing BindableProperty for the <see cref="Lines"/> property.
	/// </summary>
	public static readonly BindableProperty LinesProperty = BindableProperty.Create(
		nameof(Lines), typeof(ObservableCollection<IDrawingLine>), typeof(DrawingView), defaultValueCreator: (_) => new ObservableCollection<IDrawingLine>(), defaultBindingMode: BindingMode.TwoWay);

	/// <summary>
	/// Backing BindableProperty for the <see cref="DrawingLineCompletedCommand"/> property.
	/// </summary>
	public static readonly BindableProperty DrawingLineCompletedCommandProperty = BindableProperty.Create(nameof(DrawingLineCompletedCommand), typeof(ICommand), typeof(DrawingView));

	/// <summary>
	/// Backing BindableProperty for the <see cref="DrawingLineStartedCommand"/> property.
	/// </summary>
	public static readonly BindableProperty DrawingLineStartedCommandProperty = BindableProperty.Create(nameof(DrawingLineStartedCommand), typeof(ICommand), typeof(DrawingView));

	/// <summary>
	/// Backing BindableProperty for the <see cref="DrawingLineCancelledCommand"/> property.
	/// </summary>
	public static readonly BindableProperty DrawingLineCancelledCommandProperty = BindableProperty.Create(nameof(DrawingLineCancelledCommand), typeof(ICommand), typeof(DrawingView));

	/// <summary>
	/// Backing BindableProperty for the <see cref="PointDrawnCommand"/> property.
	/// </summary>
	public static readonly BindableProperty PointDrawnCommandProperty = BindableProperty.Create(nameof(PointDrawnCommand), typeof(ICommand), typeof(DrawingView));

	/// <summary>
	/// Backing BindableProperty for the <see cref="LineColor"/> property.
	/// </summary>
	public static readonly BindableProperty LineColorProperty =
		BindableProperty.Create(nameof(LineColor), typeof(Color), typeof(DrawingView), DrawingViewDefaults.LineColor);

	/// <summary>
	/// Backing BindableProperty for the <see cref="LineWidth"/> property.
	/// </summary>
	public static readonly BindableProperty LineWidthProperty =
		BindableProperty.Create(nameof(LineWidth), typeof(float), typeof(DrawingView), DrawingViewDefaults.LineWidth);

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
	/// Event occurred when drawing line cancelled.
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
	/// The <see cref="Color"/> that is used by default to draw a line on the <see cref="DrawingView"/>. This is a bindable property.
	/// </summary>
	public Color LineColor
	{
		get => (Color)GetValue(LineColorProperty);
		set => SetValue(LineColorProperty, value);
	}

	/// <summary>
	/// The width that is used by default to draw a line on the <see cref="DrawingView"/>. This is a bindable property.
	/// </summary>
	public float LineWidth
	{
		get => (float)GetValue(LineWidthProperty);
		set => SetValue(LineWidthProperty, value);
	}

	/// <summary>
	/// This command is invoked whenever the drawing of a line on <see cref="DrawingView"/> has completed.
	/// Note that this is fired after the tap or click is lifted. When <see cref="IsMultiLineModeEnabled"/> is enabled
	/// this command is fired multiple times.
	/// This is a bindable property.
	/// </summary>
	public ICommand? DrawingLineCompletedCommand
	{
		get => (ICommand?)GetValue(DrawingLineCompletedCommandProperty);
		set => SetValue(DrawingLineCompletedCommandProperty, value);
	}

	/// <summary>
	/// This command is invoked whenever the drawing of a line on <see cref="DrawingView"/> has started.
	/// </summary>
	public ICommand? DrawingLineStartedCommand
	{
		get => (ICommand?)GetValue(DrawingLineStartedCommandProperty);
		set => SetValue(DrawingLineStartedCommandProperty, value);
	}

	/// <summary>
	/// This command is invoked whenever the drawing of a line on <see cref="DrawingView"/> has cancelled.
	/// </summary>
	public ICommand? DrawingLineCancelledCommand
	{
		get => (ICommand?)GetValue(DrawingLineCancelledCommandProperty);
		set => SetValue(DrawingLineCancelledCommandProperty, value);
	}

	/// <summary>
	/// This command is invoked whenever the drawing on <see cref="DrawingView"/>.
	/// </summary>
	public ICommand? PointDrawnCommand
	{
		get => (ICommand?)GetValue(PointDrawnCommandProperty);
		set => SetValue(PointDrawnCommandProperty, value);
	}

	/// <summary>
	/// The collection of lines that are currently on the <see cref="DrawingView"/>. This is a bindable property.
	/// </summary>
	public ObservableCollection<IDrawingLine> Lines
	{
		get => (ObservableCollection<IDrawingLine>)GetValue(LinesProperty);
		set => SetValue(LinesProperty, value);
	}

	/// <summary>
	/// Toggles multi-line mode. When true, multiple lines can be drawn on the <see cref="DrawingView"/> while the tap/click is released in-between lines.
	/// Note: when <see cref="ShouldClearOnFinish"/> is also enabled, the lines are cleared after the tap/click is released.
	/// Additionally, <see cref="DrawingLineCompletedCommand"/> will be fired after each line that is drawn.
	/// This is a bindable property.
	/// </summary>
	public bool IsMultiLineModeEnabled
	{
		get => (bool)GetValue(IsMultiLineModeEnabledProperty);
		set => SetValue(IsMultiLineModeEnabledProperty, value);
	}

	/// <summary>
	/// Indicates whether the <see cref="DrawingView"/> is cleared after releasing the tap/click and a line is drawn.
	/// Note: when <see cref="IsMultiLineModeEnabled"/> is also enabled, this might cause unexpected behavior.
	/// This is a bindable property.
	/// </summary>
	public bool ShouldClearOnFinish
	{
		get => (bool)GetValue(ShouldClearOnFinishProperty);
		set => SetValue(ShouldClearOnFinishProperty, value);
	}

	/// <summary>
	/// Allows to draw on the <see cref="IDrawingView"/>.
	/// This is a bindable property.
	/// </summary>
	public Action<ICanvas, RectF>? DrawAction
	{
		get => (Action<ICanvas, RectF>?)GetValue(DrawActionProperty);
		set => SetValue(DrawActionProperty, value);
	}

	/// <summary>
	/// Retrieves a <see cref="Stream"/> containing an image of the collection of <see cref="IDrawingLine"/> that is provided as a parameter.
	/// </summary>
	/// <param name="lines">A collection of <see cref="IDrawingLine"/> that a image is generated from.</param>
	/// <param name="imageSize">The desired dimensions of the generated image. The image will be resized proportionally.</param>
	/// <param name="background">Background of the generated image. If color is null, default background color is used.</param>
	/// <param name="token"><see cref="CancellationToken"/></param>
	/// <returns><see cref="ValueTask{Stream}"/> containing the data of the requested image with data that's provided through the <paramref name="lines"/> parameter.</returns>
	public static ValueTask<Stream> GetImageStream(IEnumerable<IDrawingLine> lines,
													Size imageSize,
													Brush? background,
													CancellationToken token = default) =>
		DrawingViewService.GetImageStream(lines.ToList(), imageSize, background, token);

	/// <summary>
	/// Retrieves a <see cref="Stream"/> containing an image of the <see cref="Lines"/> that are currently drawn on the <see cref="DrawingView"/>.
	/// </summary>
	/// <param name="imageSizeWidth">Desired width of the image that is returned. The image will be resized proportionally.</param>
	/// <param name="imageSizeHeight">Desired height of the image that is returned. The image will be resized proportionally.</param>
	/// <param name="token"><see cref="CancellationToken"/></param>
	/// <returns><see cref="ValueTask{Stream}"/> containing the data of the requested image with data that's currently on the <see cref="DrawingView"/>.</returns>
	public ValueTask<Stream> GetImageStream(double imageSizeWidth, double imageSizeHeight, CancellationToken token = default) =>
		DrawingViewService.GetImageStream(Lines.ToList(), new Size(imageSizeWidth, imageSizeHeight), Background, token);

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