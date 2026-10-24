using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Maui.Core.Views;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.Core.Handlers;

/// <summary>
/// DrawingView handler
/// </summary>
/// <remarks>
/// While the toolkit's DrawingView is unloaded (for example while its page is not shown), its handler stays connected to it but stops listening to
/// the platform view and to <see cref="IDrawingView.Lines"/>. When the DrawingView is loaded again, the handler listens to both again as it does when it
/// connects, also with a mapper that does not map <see cref="IDrawingView.Lines"/>, and updates the platform view through the
/// <see cref="IDrawingView.Lines"/> mapping of its mapper. If the DrawingView is unloaded while the handler is being connected to it, the handler starts
/// listening once the DrawingView is loaded.
/// </remarks>
public partial class DrawingViewHandler
{
	/// <summary>
	/// <see cref ="PropertyMapper"/> for DrawingView Control.
	/// </summary>
	public static readonly IPropertyMapper<IDrawingView, DrawingViewHandler> DrawingViewMapper = new PropertyMapper<IDrawingView, DrawingViewHandler>(ViewMapper)
	{
		// `IDrawingView.Lines` is mapped last by convention. MapLines reads IsMultiLineModeEnabled from the DrawingView, not from the platform view,
		// so its result does not depend on the order of the mappers.
		[nameof(IDrawingView.DrawAction)] = MapDrawAction,
		[nameof(IDrawingView.ShouldClearOnFinish)] = MapShouldClearOnFinish,
		[nameof(IDrawingView.IsMultiLineModeEnabled)] = MapIsMultiLineModeEnabled,
		[nameof(IDrawingView.LineColor)] = MapLineColor,
		[nameof(IDrawingView.LineWidth)] = MapLineWidth,
		[nameof(IDrawingView.Background)] = MapDrawingViewBackground,
		[nameof(IDrawingView.Lines)] = MapLines,
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

	// Copying the platform lines back to DrawingView.Lines reuses the DrawingView line that each platform line stands for (the app's line that was
	// copied to the platform view, or the conversion of a drawn line), so the app's lines and the lines reported by DrawingLineCompleted stay in
	// DrawingView.Lines. The platform lines are weak keys: an entry goes away with its platform line.
	readonly ConditionalWeakTable<MauiDrawingLine, IDrawingLine> platformToVirtualViewLines = new();

	IDrawingLineAdapter adapter = new DrawingLineAdapter();
	ObservableCollection<IDrawingLine>? observedVirtualViewLines;

	bool isWaitingToBeLoaded;

	/// <summary>
	/// Action that's triggered when the DrawingView <see cref="IDrawingView.Lines"/> property changes.
	/// </summary>
	/// <remarks>
	/// While the DrawingView is unloaded, this only stops observing the previous <see cref="IDrawingView.Lines"/> collection: the handler maps
	/// <see cref="IDrawingView.Lines"/> to the platform view again when the DrawingView is loaded again.
	/// </remarks>
	/// <param name="handler">An instance of <see cref="DrawingViewHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IDrawingView"/>.</param>
	public static void MapLines(DrawingViewHandler handler, IDrawingView view)
	{
		handler.ObserveVirtualViewLines(null);

		if (handler.isWaitingToBeLoaded)
		{
			return;
		}

		// In single-line mode the platform view shows only the last line, so DrawingView.Lines keeps only that line (the app's instance),
		// and the image that DrawingView.GetImageStream exports matches the view
		if (!view.IsMultiLineModeEnabled)
		{
			while (view.Lines.Count > 1)
			{
				view.Lines.RemoveAt(0);
			}
		}

		handler.ObserveVirtualViewLines(view.Lines);
		handler.SetPlatformViewLines(view);
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
	public void SetDrawingLineAdapter(IDrawingLineAdapter drawingLineAdapter)
	{
		adapter = drawingLineAdapter;
	}

	/// <inheritdoc />
	protected override void ConnectHandler(MauiDrawingView platformView)
	{
		base.ConnectHandler(platformView);
		platformView.Initialize();

		platformView.AttachedToWindow += OnPlatformViewAttachedToWindow;

		// A handler that is given its DrawingView handler-first (IElementHandler.SetVirtualView) is set as DrawingView.Handler before ConnectHandler, and MAUI
		// raises Unloaded in between when the DrawingView was loaded and the new platform view is not in a window yet: the handler then listens from ListenAgain
		if (!isWaitingToBeLoaded)
		{
			Listen(platformView);
		}
	}

	/// <inheritdoc />
	protected override void DisconnectHandler(MauiDrawingView platformView)
	{
		isWaitingToBeLoaded = false;
		platformView.AttachedToWindow -= OnPlatformViewAttachedToWindow;
		StopListening(platformView);

		platformView.CleanUp();

		base.DisconnectHandler(platformView);
	}

	/// <inheritdoc />
#if ANDROID
	protected override MauiDrawingView CreatePlatformView() => new(Context);
#else
	protected override MauiDrawingView CreatePlatformView() => new();
#endif

	// DrawingView calls this on Unloaded (#420): while its page is away, the platform view and the DrawingView's Lines, which the app may keep, do not
	// keep this handler and each other alive. The handler stays connected to the DrawingView, so that MAUI can still remove the DrawingView from its
	// layout (or move it to another one) and property changes still reach the platform view.
	internal void StopListeningUntilLoaded()
	{
		if (isWaitingToBeLoaded || ((IElementHandler)this).PlatformView is not MauiDrawingView platformView)
		{
			return;
		}

		isWaitingToBeLoaded = true;
		StopListening(platformView);
	}

	// DrawingView.Lines may have changed meanwhile, so it is mapped to the platform view again. Listen comes first, as ConnectHandler comes before
	// the mappers: MapLines replaces the two Lines subscriptions of Listen, while a Listen after MapLines would subscribe to the platform view's
	// Lines a second time.
	internal void ListenAgain()
	{
		if (!isWaitingToBeLoaded)
		{
			return;
		}

		isWaitingToBeLoaded = false;
		Listen(PlatformView);
		UpdateValue(nameof(IDrawingView.Lines));
	}

	// A mapper without the Lines mapping (or with another one) must not leave the two Lines collections out of sync, so this subscribes to both itself.
	// MapLines observes DrawingView.Lines and subscribes to the platform view's Lines again without doubling them.
	void Listen(MauiDrawingView platformView)
	{
		platformView.DrawingStarted += OnPlatformViewDrawingStarted;
		platformView.DrawingCancelled += OnPlatformViewDrawingCancelled;
		platformView.Drawing += OnPlatformViewDrawing;
		platformView.DrawingLineCompleted += OnPlatformViewDrawingLineCompleted;
		ObserveVirtualViewLines(VirtualView.Lines);
		platformView.Lines.CollectionChanged += OnPlatformViewLinesCollectionChanged;
	}

	// AttachedToWindow stays subscribed (weakly) to start the listening again
	void StopListening(MauiDrawingView platformView)
	{
		platformView.DrawingStarted -= OnPlatformViewDrawingStarted;
		platformView.DrawingCancelled -= OnPlatformViewDrawingCancelled;
		platformView.Drawing -= OnPlatformViewDrawing;
		platformView.DrawingLineCompleted -= OnPlatformViewDrawingLineCompleted;
		ObserveVirtualViewLines(null);
		platformView.Lines.CollectionChanged -= OnPlatformViewLinesCollectionChanged;
	}

	// MAUI raises VisualElement.Loaded for a MauiDrawingView on iOS and Mac Catalyst only once it notices a layout change of the view, so the platform
	// view raises this event there.
	void OnPlatformViewAttachedToWindow(object? sender, EventArgs e) => ListenAgain();

	void OnPlatformViewDrawingLineCompleted(object? sender, MauiDrawingLineCompletedEventArgs e)
	{
		// MauiDrawingView adds the completed line to its Lines before it raises DrawingLineCompleted, so OnPlatformViewLinesCollectionChanged
		// has already stored its conversion in DrawingView.Lines: report that instance instead of converting the line again
		VirtualView.OnDrawingLineCompleted(GetVirtualViewLine(e.Line));
	}
	
	void OnPlatformViewDrawing(object? sender, MauiOnDrawingEventArgs e)
	{
		VirtualView.OnPointDrawn(e.Point);
	}
	
	void OnPlatformViewDrawingStarted(object? sender, MauiDrawingStartedEventArgs e)
	{
		VirtualView.OnDrawingLineStarted(e.Point);
	}
	
	void OnPlatformViewDrawingCancelled(object? sender, EventArgs e)
	{
		VirtualView.OnDrawingLineCancelled();
	}

	void OnVirtualViewLinesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		SetPlatformViewLines(VirtualView);
	}

	// The platform lines are not copied back to DrawingView.Lines, which would replace the line instances of the app with converted copies.
	void SetPlatformViewLines(IDrawingView view)
	{
		PlatformView.Lines.CollectionChanged -= OnPlatformViewLinesCollectionChanged;
		PlatformView.SetLines(view);

		// SetLines copies the last lines of DrawingView.Lines in their order: all of them in multi-line mode, the last one in single-line mode
		foreach (var (platformLine, line) in PlatformView.Lines.Zip(view.Lines.TakeLast(PlatformView.Lines.Count)))
		{
			platformToVirtualViewLines.AddOrUpdate(platformLine, line);
		}

		PlatformView.Lines.CollectionChanged += OnPlatformViewLinesCollectionChanged;
	}

	void OnPlatformViewLinesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		ObserveVirtualViewLines(null);
		VirtualView.SetLines(PlatformView, new VirtualViewLineAdapter(this));
		ObserveVirtualViewLines(VirtualView.Lines);
	}

	IDrawingLine GetVirtualViewLine(MauiDrawingLine platformLine) => platformToVirtualViewLines.GetValue(platformLine, adapter.ConvertMauiDrawingLine);

	// A collection that DrawingView.Lines no longer holds neither changes the platform view nor keeps this handler alive.
	void ObserveVirtualViewLines(ObservableCollection<IDrawingLine>? lines)
	{
		if (observedVirtualViewLines is not null)
		{
			observedVirtualViewLines.CollectionChanged -= OnVirtualViewLinesCollectionChanged;
		}

		observedVirtualViewLines = lines;

		if (observedVirtualViewLines is not null)
		{
			observedVirtualViewLines.CollectionChanged += OnVirtualViewLinesCollectionChanged;
		}
	}

	sealed class VirtualViewLineAdapter(DrawingViewHandler handler) : IDrawingLineAdapter
	{
		public IDrawingLine ConvertMauiDrawingLine(MauiDrawingLine mauiDrawingLine) => handler.GetVirtualViewLine(mauiDrawingLine);
	}
}
#endif