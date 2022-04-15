using System.Collections.Specialized;
using CommunityToolkit.Maui.Core.Extensions;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// DrawingView handler
/// </summary>
public partial class DrawingViewHandler
{
	/// <summary>
	/// <see cref ="CommandMapper"/> for DrawingView Control.
	/// </summary>
	public static readonly PropertyMapper<IDrawingView, DrawingViewHandler> DrawingViewMapper = new(ViewMapper)
	{
		[nameof(IDrawingView.Lines)] = MapLines,
		[nameof(IDrawingView.ClearOnFinish)] = MapClearOnFinish,
		[nameof(IDrawingView.LineColor)] = MapLineColor,
		[nameof(IDrawingView.LineWidth)] = MapLineWidth,
		[nameof(IDrawingView.MultiLineMode)] = MapMultiLineMode,
		[nameof(IDrawingView.Draw)] = MapDraw
	};

	/// <summary>
	/// Initialize new instance of <see cref="DrawingViewHandler"/>.
	/// </summary>
	/// <param name="mapper">Custom instance of <see cref="PropertyMapper"/>, if it's null the <see cref="DrawingViewMapper"/> will be used</param>
	/// <param name="commandMapper">Custom instance of <see cref="CommandMapper"/></param>
	public DrawingViewHandler(IPropertyMapper? mapper, CommandMapper? commandMapper)
		: base(mapper ?? DrawingViewMapper, commandMapper)
	{
	}

	/// <summary>
	/// Initialize new instance of <see cref="DrawingViewHandler"/>.
	/// </summary>
	public DrawingViewHandler() : base(DrawingViewMapper)
	{
	}
}

#if ANDROID || IOS || MACCATALYST || WINDOWS
public partial class DrawingViewHandler : ViewHandler<IDrawingView, MauiDrawingView>
{
	/// <summary>
	/// Action that's triggered when the DrawingView <see cref="IDrawingView.Lines"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="DrawingViewHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IDrawingView"/>.</param>
	public static void MapLines(DrawingViewHandler handler, IDrawingView view)
	{
		handler.PlatformView.SetLines(view);
	}

	/// <summary>
	/// Action that's triggered when the DrawingView <see cref="IDrawingView.ClearOnFinish"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="DrawingViewHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IDrawingView"/>.</param>
	public static void MapClearOnFinish(DrawingViewHandler handler, IDrawingView view)
	{
		handler.PlatformView.SetClearOnFinish(view.ClearOnFinish);
	}

	/// <summary>
	/// Action that's triggered when the DrawingView <see cref="IDrawingView.LineColor"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="DrawingViewHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IDrawingView"/>.</param>
	public static void MapLineColor(DrawingViewHandler handler, IDrawingView view)
	{
		handler.PlatformView.SetLineColor(view.LineColor);
	}

	/// <summary>
	/// Action that's triggered when the DrawingView <see cref="IDrawingView.LineWidth"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="DrawingViewHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IDrawingView"/>.</param>
	public static void MapLineWidth(DrawingViewHandler handler, IDrawingView view)
	{
		handler.PlatformView.SetLineWidth(view.LineWidth);
	}

	/// <summary>
	/// Action that's triggered when the DrawingView <see cref="IDrawingView.MultiLineMode"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="DrawingViewHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IDrawingView"/>.</param>
	public static void MapMultiLineMode(DrawingViewHandler handler, IDrawingView view)
	{
		handler.PlatformView.SetMultiLineMode(view.MultiLineMode);
	}

	/// <summary>
	/// Action that's triggered when the DrawingView <see cref="IDrawingView.Draw"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="DrawingViewHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IDrawingView"/>.</param>
	public static void MapDraw(DrawingViewHandler handler, IDrawingView view)
	{
		handler.PlatformView.SetDrawAction(view.Draw);
	}
	
	/// <inheritdoc />
	protected override void ConnectHandler(MauiDrawingView platformView)
	{
		base.ConnectHandler(platformView);
		platformView.Initialize();

		PlatformView.DrawingLineCompleted += OnPlatformViewDrawingLineCompleted;
		VirtualView.Lines.CollectionChanged += OnVirtualViewLinesCollectionChanged;
		PlatformView.Lines.CollectionChanged += OnPlatformViewLinesCollectionChanged;
	}

	/// <inheritdoc />
	protected override void DisconnectHandler(MauiDrawingView platformView)
	{
		PlatformView.DrawingLineCompleted -= OnPlatformViewDrawingLineCompleted;
		VirtualView.Lines.CollectionChanged -= OnVirtualViewLinesCollectionChanged;
		PlatformView.Lines.CollectionChanged -= OnPlatformViewLinesCollectionChanged;
		platformView.CleanUp();
		base.DisconnectHandler(platformView);
	}

	/// <inheritdoc />
#if ANDROID
	protected override MauiDrawingView CreatePlatformView() => new(Context);
#else
	protected override MauiDrawingView CreatePlatformView() => new();
#endif

	void OnPlatformViewDrawingLineCompleted(object? sender, MauiDrawingLineCompletedEventArgs e)
	{
		VirtualView.DrawingLineCompleted(new DrawingLine
		{
			LineColor = e.Line.LineColor.ToColor() ?? Colors.Black,
			EnableSmoothedPath = e.Line.EnableSmoothedPath,
			Granularity = e.Line.Granularity,
			LineWidth = e.Line.LineWidth,
			Points = e.Line.Points.Select(x => new Point(x.X, x.Y)).ToObservableCollection()
		});
	}

	void OnVirtualViewLinesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		PlatformView.Lines.CollectionChanged -= OnPlatformViewLinesCollectionChanged;
		PlatformView.SetLines(VirtualView);
		PlatformView.Lines.CollectionChanged += OnPlatformViewLinesCollectionChanged;
	}

	void OnPlatformViewLinesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		VirtualView.Lines.CollectionChanged -= OnVirtualViewLinesCollectionChanged;
		VirtualView.SetLines(PlatformView);
		VirtualView.Lines.CollectionChanged += OnVirtualViewLinesCollectionChanged;
	}
}
#else
public partial class DrawingViewHandler : ViewHandler<IDrawingView, object>
{
	/// <inheritdoc />
	protected override object CreatePlatformView() => throw new NotSupportedException();

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
	}

	/// <summary>
	/// Action that's triggered when the DrawingView <see cref="IDrawingView.LineColor"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="DrawingViewHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IDrawingView"/>.</param>
	public static void MapLineColor(DrawingViewHandler handler, IDrawingView view)
	{
	}

	/// <summary>
	/// Action that's triggered when the DrawingView <see cref="IDrawingView.LineWidth"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="DrawingViewHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IDrawingView"/>.</param>
	public static void MapLineWidth(DrawingViewHandler handler, IDrawingView view)
	{
	}

	/// <summary>
	/// Action that's triggered when the DrawingView <see cref="IDrawingView.MultiLineMode"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="DrawingViewHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IDrawingView"/>.</param>
	public static void MapMultiLineMode(DrawingViewHandler handler, IDrawingView view)
	{
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="handler">An instance of <see cref="DrawingViewHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IDrawingView"/>.</param>
	public static void MapDraw(DrawingViewHandler handler, IDrawingView view)
	{
	}
}
#endif