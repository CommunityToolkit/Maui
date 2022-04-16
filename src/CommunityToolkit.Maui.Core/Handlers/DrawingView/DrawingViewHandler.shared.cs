using System.Collections.Specialized;
using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Maui.Core.Views;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.Core.Handlers;

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
		[nameof(IDrawingView.DrawAction)] = MapDrawAction,
		[nameof(IDrawingView.Background)] = MapDrawingViewBackground,
	};
public static CommandMapper<IDrawingView, DrawingViewHandler> DrawingViewCommandMapper = new(ViewCommandMapper)
		{
		};
	/// <summary>
	/// Initialize new instance of <see cref="DrawingViewHandler"/>.
	/// </summary>
	/// <param name="mapper">Custom instance of <see cref="PropertyMapper"/>, if it's null the <see cref="DrawingViewMapper"/> will be used</param>
	/// <param name="commandMapper">Custom instance of <see cref="CommandMapper"/></param>
	public DrawingViewHandler(IPropertyMapper? mapper, CommandMapper? commandMapper)
		: base(mapper ?? DrawingViewMapper, commandMapper ?? DrawingViewCommandMapper)
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
	/// Action that's triggered when the DrawingView <see cref="IDrawingView.DrawAction"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="DrawingViewHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IDrawingView"/>.</param>
	public static void MapDrawAction(DrawingViewHandler handler, IDrawingView view)
	{
		handler.PlatformView.SetDrawAction(view.DrawAction);
	}	

	/// <summary>
	/// Action that's triggered when the DrawingView Background property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="DrawingViewHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IDrawingView"/>.</param>
	public static void MapDrawingViewBackground(DrawingViewHandler handler, IDrawingView view)
	{
		handler.PlatformView.SetPaint(view.Background ?? new SolidPaint(Defaults.BackgroundColor));
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
			LineColor = e.Line.LineColor,
			EnableSmoothedPath = e.Line.EnableSmoothedPath,
			Granularity = e.Line.Granularity,
			LineWidth = e.Line.LineWidth,
			Points = e.Line.Points.ToObservableCollection()
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
	/// Action that's triggered when the DrawingView <see cref="IDrawingView.DrawAction"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="DrawingViewHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IDrawingView"/>.</param>
	public static void MapDrawAction(DrawingViewHandler handler, IDrawingView view)
	{
	}

	/// <summary>
	/// Action that's triggered when the DrawingView Background property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="DrawingViewHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IDrawingView"/>.</param>
	public static void MapDrawingViewBackground(DrawingViewHandler handler, IDrawingView view)
	{
	}
}
#endif