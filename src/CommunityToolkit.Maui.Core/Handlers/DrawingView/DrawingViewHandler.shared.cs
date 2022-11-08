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
	/// <see cref ="PropertyMapper"/> for DrawingView Control.
	/// </summary>
	public static readonly IPropertyMapper<IDrawingView, DrawingViewHandler> DrawingViewMapper = new PropertyMapper<IDrawingView, DrawingViewHandler>(ViewMapper)
	{
		// Be careful when editing the order of the mappers below. `IDrawingView.Lines` must be mapped last.
		[nameof(IDrawingView.DrawAction)] = MapDrawAction,
		[nameof(IDrawingView.ShouldClearOnFinish)] = MapShouldClearOnFinish,
		[nameof(IDrawingView.IsMultiLineModeEnabled)] = MapIsMultiLineModeEnabled,
		[nameof(IDrawingView.LineColor)] = MapLineColor,
		[nameof(IDrawingView.LineWidth)] = MapLineWidth,
		[nameof(IDrawingView.Background)] = MapDrawingViewBackground,
		[nameof(IDrawingView.Lines)] = MapLines, // `IDrawingView.Lines` must be mapped last
	};

	/// <summary>
	/// <see cref ="CommandMapper"/> for DrawingView Control.
	/// </summary>
	public static readonly CommandMapper<IDrawingView, DrawingViewHandler> DrawingViewCommandMapper = new(ViewCommandMapper);

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
	public DrawingViewHandler() : this(DrawingViewMapper, DrawingViewCommandMapper)
	{
	}
}

#if ANDROID || IOS || MACCATALYST || WINDOWS || TIZEN
public partial class DrawingViewHandler : ViewHandler<IDrawingView, MauiDrawingView>, IDrawingViewHandler
{

	IDrawingLineAdapter adapter = new DrawingLineAdapter();

	/// <inheritdoc />
	public void SetDrawingLineAdapter(IDrawingLineAdapter drawingLineAdapter)
	{
		adapter = drawingLineAdapter;
	}

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
	/// Action that's triggered when the DrawingView <see cref="IDrawingView.ShouldClearOnFinish"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="DrawingViewHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IDrawingView"/>.</param>
	public static void MapShouldClearOnFinish(DrawingViewHandler handler, IDrawingView view)
	{
		handler.PlatformView.SetShouldClearOnFinish(view.ShouldClearOnFinish);
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
	/// Action that's triggered when the DrawingView <see cref="IDrawingView.IsMultiLineModeEnabled"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="DrawingViewHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IDrawingView"/>.</param>
	public static void MapIsMultiLineModeEnabled(DrawingViewHandler handler, IDrawingView view)
	{
		handler.PlatformView.SetIsMultiLineModeEnabled(view.IsMultiLineModeEnabled);
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
		handler.PlatformView.SetPaint(view.Background ?? new SolidPaint(DrawingViewDefaults.BackgroundColor));
	}

	/// <inheritdoc />
	protected override void ConnectHandler(MauiDrawingView platformView)
	{
		base.ConnectHandler(platformView);
		platformView.Initialize();

		platformView.DrawingLineCompleted += OnPlatformViewDrawingLineCompleted;
		VirtualView.Lines.CollectionChanged += OnVirtualViewLinesCollectionChanged;
		platformView.Lines.CollectionChanged += OnPlatformViewLinesCollectionChanged;
	}

	/// <inheritdoc />
	protected override void DisconnectHandler(MauiDrawingView platformView)
	{
		platformView.DrawingLineCompleted -= OnPlatformViewDrawingLineCompleted;
		VirtualView.Lines.CollectionChanged -= OnVirtualViewLinesCollectionChanged;
		platformView.Lines.CollectionChanged -= OnPlatformViewLinesCollectionChanged;

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
		var drawingLine = adapter.ConvertMauiDrawingLine(e.Line);
		VirtualView.DrawingLineCompleted(drawingLine);
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
		VirtualView.SetLines(PlatformView, adapter);
		VirtualView.Lines.CollectionChanged += OnVirtualViewLinesCollectionChanged;
	}
}
#endif