using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace CommunityToolkit.Maui.Core.Views;

public partial class DrawingViewHandler : ViewHandler<IDrawingView, DrawingNativeView>
{
	/// <inheritdoc />
	protected override DrawingNativeView CreatePlatformView()
	{
		return new DrawingNativeView();
	}

	/// <inheritdoc />
	protected override void ConnectHandler(DrawingNativeView nativeView)
	{
		base.ConnectHandler(nativeView);
		nativeView.Initialize();
		VirtualView.Lines.CollectionChanged += Lines_CollectionChanged;
	}


	/// <inheritdoc />
	protected override void DisconnectHandler(DrawingNativeView nativeView)
	{
		base.DisconnectHandler(nativeView);
		nativeView.CleanUp();
		VirtualView.Lines.CollectionChanged -= Lines_CollectionChanged;
	}


	/// <summary>
	/// Action that's triggered when the DrawingView <see cref="IDrawingView.Lines"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="DrawingViewHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IDrawingView"/>.</param>
	public static void MapLines(DrawingViewHandler handler, IDrawingView view)
	{

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
	}

	void Lines_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
	{
		PlatformView.Lines.Clear();
		foreach (var line in VirtualView.Lines)
		{
			PlatformView.Lines.Add(new DrawingNativeLine()
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