using Microsoft.Maui.Handlers;
using CommunityToolkit.Maui.Core.Extensions;
namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// Line handler
/// </summary>
public partial class LineHandler : ElementHandler<ILine, MauiDrawingLine>
{
	/// <summary>
	/// <see cref ="CommandMapper"/> for ILine Control.
	/// </summary>
	public static readonly PropertyMapper<ILine, LineHandler> LineMapper = new(ElementMapper)
	{
		[nameof(ILine.Points)] = MapPoints,
		[nameof(ILine.EnableSmoothedPath)] = MapEnableSmoothedPath,
		[nameof(ILine.LineColor)] = MapLineColor,
		[nameof(ILine.LineWidth)] = MapLineWidth,
		[nameof(ILine.Granularity)] = MapGranularity
	};

	/// <summary>
	/// Initialize new instance of <see cref="LineHandler"/>.
	/// </summary>
	/// <param name="mapper">Custom instance of <see cref="PropertyMapper"/>, if it's null the <see cref="LineMapper"/> will be used</param>
	/// <param name="commandMapper">Custom instance of <see cref="CommandMapper"/></param>
	public LineHandler(IPropertyMapper? mapper, CommandMapper? commandMapper)
		: base(mapper ?? LineMapper, commandMapper)
	{
	}

	/// <summary>
	/// Initialize new instance of <see cref="LineHandler"/>.
	/// </summary>
	public LineHandler() : base(LineMapper)
	{
	}

	/// <summary>
	/// Action that's triggered when the DrawingView <see cref="ILine.Points"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="LineHandler"/>.</param>
	/// <param name="view">An instance of <see cref="ILine"/>.</param>
	public static void MapPoints(LineHandler handler, ILine view)
	{
		UpdatePoints(view, handler.PlatformView);
	}

	/// <summary>
	/// Action that's triggered when the DrawingView <see cref="ILine.EnableSmoothedPath"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="LineHandler"/>.</param>
	/// <param name="view">An instance of <see cref="ILine"/>.</param>
	public static void MapEnableSmoothedPath(LineHandler handler, ILine view)
	{
		handler.PlatformView.SetEnableSmoothedPath(view.EnableSmoothedPath);
	}

	/// <summary>
	/// Action that's triggered when the DrawingView <see cref="ILine.LineColor"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="LineHandler"/>.</param>
	/// <param name="view">An instance of <see cref="ILine"/>.</param>
	public static void MapLineColor(LineHandler handler, ILine view)
	{
		handler.PlatformView.SetLineColor(view.LineColor);
	}

	/// <summary>
	/// Action that's triggered when the DrawingView <see cref="ILine.LineWidth"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="LineHandler"/>.</param>
	/// <param name="view">An instance of <see cref="ILine"/>.</param>
	public static void MapLineWidth(LineHandler handler, ILine view)
	{
		handler.PlatformView.SetLineWidth(view.LineWidth);
	}

	/// <summary>
	/// Action that's triggered when the DrawingView <see cref="ILine.Granularity"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="LineHandler"/>.</param>
	/// <param name="view">An instance of <see cref="ILine"/>.</param>
	public static void MapGranularity(LineHandler handler, ILine view)
	{
		handler.PlatformView.SetGranularity(view.Granularity);
	}
	/// <inheritdoc />
	protected override void ConnectHandler(MauiDrawingLine nativeView)
	{
		base.ConnectHandler(nativeView);
		VirtualView.Points.CollectionChanged += OnPointsCollectionChanged;
	}

	/// <inheritdoc />
	protected override void DisconnectHandler(MauiDrawingLine nativeView)
	{
		VirtualView.Points.CollectionChanged -= OnPointsCollectionChanged;
		base.DisconnectHandler(nativeView);
	}

#if !ANDROID
	/// <inheritdoc />
	protected override MauiDrawingLine CreatePlatformElement() => new();
#endif

	void OnPointsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
	{
		UpdatePoints(VirtualView, PlatformView);
	}

	static void UpdatePoints(ILine virtualView, MauiDrawingLine platformView)
	{
		platformView.Points.Clear();
		foreach (var point in virtualView.Points)
		{
			platformView.Points.Add(point);
		}
	}
}