using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// DrawingView handler
/// </summary>
public partial class DrawingViewHandler
{
	/// <summary>
	/// <see cref ="CommandMapper"/> for DrawingView Control.
	/// </summary>
	public static PropertyMapper<IDrawingView, DrawingViewHandler> DrawingViewMapper = new (ViewHandler.ViewMapper)
	{
		[nameof(IDrawingView.Lines)] = MapLines,
		[nameof(IDrawingView.ClearOnFinish)] = MapClearOnFinish,
		[nameof(IDrawingView.DefaultLineColor)] = MapDefaultLineColor,
		[nameof(IDrawingView.DefaultLineWidth)] = MapDefaultLineWidth,
		[nameof(IDrawingView.MultiLineMode)] = MapMultiLineMode,
	};

	/// <summary>
	/// <see cref ="CommandMapper"/> for DrawingView Control.
	/// </summary>
	public static CommandMapper<IDrawingView, DrawingViewHandler> DrawingViewCommandMapper = new(ElementCommandMapper)
	{
		[nameof(IDrawingView.DrawingLineCompletedCommand)] = MapDrawingLineCompletedCommand
	};
	
	/// <summary>
	/// Initialize new instance of <see cref="DrawingViewHandler"/>.
	/// </summary>
	/// <param name="mapper">Custom instance of <see cref="PropertyMapper"/>, if it's null the <see cref="DrawingViewMapper"/> will be used</param>
	/// <param name="commandMapper">Custom instance of <see cref="CommandMapper"/>, if it's null the <see cref="DrawingViewMapper"/> will be used</param>
	public DrawingViewHandler(PropertyMapper? mapper, CommandMapper? commandMapper)
		: base(mapper ?? DrawingViewMapper, commandMapper ?? DrawingViewCommandMapper)
	{
	}

	/// <summary>
	/// Initialize new instance of <see cref="DrawingViewHandler"/>.
	/// </summary>
	public DrawingViewHandler() : base(DrawingViewMapper, DrawingViewCommandMapper)
	{
	}
}

#if !(ANDROID || IOS || MACCATALYST || WINDOWS)
public partial class DrawingViewHandler : ViewHandler<IDrawingView, object>
{
	/// <inheritdoc />
	protected override object CreateNativeView() => throw new NotImplementedException();

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
	/// Action that's triggered when the DrawingView <see cref="IDrawingView.DefaultLineColor"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="DrawingViewHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IDrawingView"/>.</param>
	public static void MapDefaultLineColor(DrawingViewHandler handler, IDrawingView view)
	{
	}

	/// <summary>
	/// Action that's triggered when the DrawingView <see cref="IDrawingView.DefaultLineWidth"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="DrawingViewHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IDrawingView"/>.</param>
	public static void MapDefaultLineWidth(DrawingViewHandler handler, IDrawingView view)
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
	/// Action that's triggered when the DrawingView <see cref="IDrawingView.DrawingLineCompletedCommand"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="DrawingViewHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IDrawingView"/>.</param>
	/// <param name="argument">Command argument</param>
	public static void MapDrawingLineCompletedCommand(DrawingViewHandler handler, IDrawingView view, object? argument)
	{
	}
}
#endif