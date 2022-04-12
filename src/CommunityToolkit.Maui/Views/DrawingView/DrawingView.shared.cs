using System.Collections.ObjectModel;
using System.Runtime.Versioning;
using System.Windows.Input;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;

namespace CommunityToolkit.Maui.Views;

/// <summary>
/// The DrawingView allows you to draw one or multiple lines on a canvas.
/// </summary>
[UnsupportedOSPlatform("windows")]
public class DrawingView : View, IDrawingView
{
	readonly WeakEventManager drawingLineCompletedEventManager = new();

	/// <summary>
	/// Backing BindableProperty for the <see cref="ClearOnFinish"/> property.
	/// </summary>
	public static readonly BindableProperty ClearOnFinishProperty =
		BindableProperty.Create(nameof(ClearOnFinish), typeof(bool), typeof(DrawingView), default(bool));

	/// <summary>
	/// Backing BindableProperty for the <see cref="MultiLineMode"/> property.
	/// </summary>
	public static readonly BindableProperty MultiLineModeProperty =
		BindableProperty.Create(nameof(MultiLineMode), typeof(bool), typeof(DrawingView), default(bool));

	/// <summary>
	/// Backing BindableProperty for the <see cref="Lines"/> property.
	/// </summary>
	public static readonly BindableProperty LinesProperty = BindableProperty.Create(
		nameof(Lines), typeof(ObservableCollection<DrawingLine>), typeof(DrawingView), new ObservableCollection<DrawingLine>(), BindingMode.TwoWay);

	/// <summary>
	/// Backing BindableProperty for the <see cref="DrawingLineCompletedCommand"/> property.
	/// </summary>
	public static readonly BindableProperty DrawingLineCompletedCommandProperty = BindableProperty.Create(nameof(DrawingLineCompletedCommand), typeof(ICommand), typeof(DrawingView));

	/// <summary>
	/// Backing BindableProperty for the <see cref="LineColor"/> property.
	/// </summary>
	public static readonly BindableProperty DefaultLineColorProperty =
		BindableProperty.Create(nameof(LineColor), typeof(Color), typeof(DrawingView), Colors.Black);

	/// <summary>
	/// Backing BindableProperty for the <see cref="LineWidth"/> property.
	/// </summary>
	public static readonly BindableProperty DefaultLineWidthProperty =
		BindableProperty.Create(nameof(LineWidth), typeof(float), typeof(DrawingView), 5f);

	/// <summary>
	/// Event occurred when drawing line completed.
	/// </summary>
	public event EventHandler<DrawingLineCompletedEventArgs> DrawingLineCompleted
	{
		add => drawingLineCompletedEventManager.AddEventHandler(value);
		remove => drawingLineCompletedEventManager.RemoveEventHandler(value);
	}

	/// <summary>
	/// The <see cref="Color"/> that is used by default to draw a line on the <see cref="DrawingView"/>. This is a bindable property.
	/// </summary>
	public Color LineColor
	{
		get => (Color)GetValue(DefaultLineColorProperty);
		set => SetValue(DefaultLineColorProperty, value);
	}

	/// <summary>
	/// The width that is used by default to draw a line on the <see cref="DrawingView"/>. This is a bindable property.
	/// </summary>
	public float LineWidth
	{
		get => (float)GetValue(DefaultLineWidthProperty);
		set => SetValue(DefaultLineWidthProperty, value);
	}

	/// <summary>
	/// This command is invoked whenever the drawing of a line on <see cref="DrawingView"/> has completed.
	/// Note that this is fired after the tap or click is lifted. When <see cref="MultiLineMode"/> is enabled
	/// this command is fired multiple times.
	/// This is a bindable property.
	/// </summary>
	public ICommand? DrawingLineCompletedCommand
	{
		get => (ICommand)GetValue(DrawingLineCompletedCommandProperty);
		set => SetValue(DrawingLineCompletedCommandProperty, value);
	}

	/// <summary>
	/// The collection of lines that are currently on the <see cref="DrawingView"/>. This is a bindable property.
	/// </summary>
	public ObservableCollection<DrawingLine> Lines
	{
		get => (ObservableCollection<DrawingLine>)GetValue(LinesProperty);
		set => SetValue(LinesProperty, value);
	}

	/// <summary>
	/// Toggles multi-line mode. When true, multiple lines can be drawn on the <see cref="DrawingView"/> while the tap/click is released in-between lines.
	/// Note: when <see cref="ClearOnFinish"/> is also enabled, the lines are cleared after the tap/click is released.
	/// Additionally, <see cref="DrawingLineCompletedCommand"/> will be fired after each line that is drawn.
	/// This is a bindable property.
	/// </summary>
	public bool MultiLineMode
	{
		get => (bool)GetValue(MultiLineModeProperty);
		set => SetValue(MultiLineModeProperty, value);
	}

	/// <summary>
	/// Indicates whether the <see cref="DrawingView"/> is cleared after releasing the tap/click and a line is drawn.
	/// Note: when <see cref="MultiLineMode"/> is also enabled, this might cause unexpected behavior.
	/// This is a bindable property.
	/// </summary>
	public bool ClearOnFinish
	{
		get => (bool)GetValue(ClearOnFinishProperty);
		set => SetValue(ClearOnFinishProperty, value);
	}

	/// <summary>
	/// Retrieves a <see cref="Stream"/> containing an image of the <see cref="Lines"/> that are currently drawn on the <see cref="DrawingView"/>.
	/// </summary>
	/// <param name="imageSizeWidth">Desired width of the image that is returned.</param>
	/// <param name="imageSizeHeight">Desired height of the image that is returned.</param>
	/// <returns><see cref="Stream"/> containing the data of the requested image with data that's currently on the <see cref="DrawingView"/>.</returns>
	public Stream GetImageStream(double imageSizeWidth, double imageSizeHeight) => DrawingViewService.GetImageStream(Lines.ToList(), new Size(imageSizeWidth, imageSizeHeight), BackgroundColor);

	/// <summary>
	/// Retrieves a <see cref="Stream"/> containing an image of the collection of <see cref="DrawingLine"/> that is provided as a parameter.
	/// </summary>
	/// <param name="lines">A collection of <see cref="DrawingLine"/> that a image is generated from.</param>
	/// <param name="imageSize">The desired dimensions of the generated image.</param>
	/// <param name="backgroundColor">Background color of the generated image.</param>
	/// <returns><see cref="Stream"/> containing the data of the requested image with data that's provided through the <paramref name="lines"/> parameter.</returns>
	public static Stream GetImageStream(IEnumerable<DrawingLine> lines,
		Size imageSize,
		Color backgroundColor) =>
		DrawingViewService.GetImageStream(lines.ToList(), imageSize, backgroundColor);

	/// <summary>
	/// Executes DrawingLineCompleted event and DrawingLineCompletedCommand
	/// </summary>
	/// <param name="lastDrawingLine">Last drawing line</param>
	void IDrawingView.DrawingLineCompleted(DrawingLine lastDrawingLine)
	{
		drawingLineCompletedEventManager.HandleEvent(this, new DrawingLineCompletedEventArgs(lastDrawingLine), nameof(DrawingLineCompleted));

		if (DrawingLineCompletedCommand?.CanExecute(lastDrawingLine) ?? false)
		{
			DrawingLineCompletedCommand.Execute(lastDrawingLine);
		}
	}
}