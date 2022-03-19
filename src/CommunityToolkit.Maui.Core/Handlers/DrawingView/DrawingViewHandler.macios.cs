using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace CommunityToolkit.Maui.Core.Views;

public partial class DrawingViewHandler : ViewHandler<IDrawingView, MauiDrawingView>
{
	/// <summary>
	/// Action that's triggered when the DrawingView <see cref="IDrawingView.Lines"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="DrawingViewHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IDrawingView"/>.</param>
	public static void MapLines(DrawingViewHandler handler, IDrawingView view)
	{
		UpdateLines(view, handler.PlatformView);
	}

	/// <summary>
	/// Action that's triggered when the DrawingView <see cref="IDrawingView.ClearOnFinish"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="DrawingViewHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IDrawingView"/>.</param>
	public static void MapClearOnFinish(DrawingViewHandler handler, IDrawingView view)
	{
		handler.PlatformView.ClearOnFinish = view.ClearOnFinish;
	}

	/// <summary>
	/// Action that's triggered when the DrawingView <see cref="IDrawingView.LineColor"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="DrawingViewHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IDrawingView"/>.</param>
	public static void MapLineColor(DrawingViewHandler handler, IDrawingView view)
	{
		handler.PlatformView.LineColor = view.LineColor.ToPlatform();
	}

	/// <summary>
	/// Action that's triggered when the DrawingView <see cref="IDrawingView.LineWidth"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="DrawingViewHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IDrawingView"/>.</param>
	public static void MapLineWidth(DrawingViewHandler handler, IDrawingView view)
	{
		handler.PlatformView.LineWidth = view.LineWidth;
	}

	/// <summary>
	/// Action that's triggered when the DrawingView <see cref="IDrawingView.MultiLineMode"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="DrawingViewHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IDrawingView"/>.</param>
	public static void MapMultiLineMode(DrawingViewHandler handler, IDrawingView view)
	{
		handler.PlatformView.MultiLineMode = view.MultiLineMode;
	}

	/// <summary>
	/// Action that's triggered when the DrawingView <see cref="IDrawingView.DrawingLineCompletedCommand"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="DrawingViewHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IDrawingView"/>.</param>
	public static void MapDrawingLineCompletedCommand(DrawingViewHandler handler, IDrawingView view)
	{
		handler.PlatformView.DrawingLineCompletedCommand = view.DrawingLineCompletedCommand;
	}

	/// <summary>
	/// Action that's triggered when the DrawingView <see cref="IDrawingView.DrawingLineCompletedCommand"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="DrawingViewHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IDrawingView"/>.</param>
	/// <param name="parameter">Command argument</param>
	public static void MapDrawingLineCompletedEvent(DrawingViewHandler handler, IDrawingView view, object? parameter)
	{
		if (parameter is ILine line)
		{
			view.DrawingLineCompleted(line);
		}
	}

	/// <inheritdoc />
	protected override MauiDrawingView CreatePlatformView() => new();

	/// <inheritdoc />
	protected override void ConnectHandler(MauiDrawingView nativeView)
	{
		base.ConnectHandler(nativeView);
		nativeView.Initialize();
		VirtualView.Lines.CollectionChanged += OnLinesCollectionChanged;
	}

	/// <inheritdoc />
	protected override void DisconnectHandler(MauiDrawingView nativeView)
	{
		base.DisconnectHandler(nativeView);
		nativeView.CleanUp();
		VirtualView.Lines.CollectionChanged -= OnLinesCollectionChanged;
	}

	void OnLinesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
	{
		UpdateLines(VirtualView, PlatformView);
	}

	static void UpdateLines(IDrawingView virtualView, MauiDrawingView platformView)
	{
		platformView.Lines.Clear();
		if (!virtualView.MultiLineMode && virtualView.Lines.Count > 1)
		{
			throw new InvalidOperationException("Only 1 line is allowed with multiline mode");
		}

		foreach (var line in virtualView.Lines)
		{
			platformView.Lines.Add(new MauiDrawingLine()
			{
				LineColor = line.LineColor,
				EnableSmoothedPath = line.EnableSmoothedPath,
				Granularity = line.Granularity,
				LineWidth = line.LineWidth,
				Points = line.Points
			});
		}
	}
}