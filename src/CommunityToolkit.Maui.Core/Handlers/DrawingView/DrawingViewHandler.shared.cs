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
		[nameof(IDrawingView.MultiLineMode)] = MapMultiLineMode
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

#if !(ANDROID || IOS || MACCATALYST || WINDOWS)
public partial class DrawingViewHandler : Microsoft.Maui.Handlers.ViewHandler<IDrawingView, object>
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
}
#endif