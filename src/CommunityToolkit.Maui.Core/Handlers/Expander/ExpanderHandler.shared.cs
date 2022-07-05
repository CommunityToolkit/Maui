using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Maui.Core.Views;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.Core.Handlers;

#if WINDOWS
using MauiExpander = Microsoft.UI.Xaml.Controls.Expander;
using ExpanderCollapsedEventArgs = Microsoft.UI.Xaml.Controls.ExpanderCollapsedEventArgs;
using Microsoft.UI.Xaml.Controls;
#endif

/// <summary>
/// Expander handler
/// </summary>
public partial class ExpanderHandler
{
	/// <summary>
	/// <see cref ="PropertyMapper"/> for Expander Control.
	/// </summary>
	public static readonly IPropertyMapper<IExpander, ExpanderHandler> ExpanderMapper = new PropertyMapper<IExpander, ExpanderHandler>(ViewMapper)
	{
		
		[nameof(IExpander.Header)] = MapHeader,
		[nameof(IExpander.Content)] = MapContent,
		[nameof(IExpander.IsExpanded)] = MapIsExpanded,
		[nameof(IExpander.Direction)] = MapDirection,
	};

	/// <summary>
	/// <see cref ="CommandMapper"/> for Expander Control.
	/// </summary>
	public static readonly CommandMapper<IExpander, ExpanderHandler> ExpanderCommandMapper = new(ViewCommandMapper);

	/// <summary>
	/// Initialize new instance of <see cref="ExpanderHandler"/>.
	/// </summary>
	/// <param name="mapper">Custom instance of <see cref="PropertyMapper"/>, if it's null the <see cref="ExpanderMapper"/> will be used</param>
	/// <param name="commandMapper">Custom instance of <see cref="CommandMapper"/></param>
	public ExpanderHandler(IPropertyMapper? mapper, CommandMapper? commandMapper)
		: base(mapper ?? ExpanderMapper, commandMapper ?? ExpanderCommandMapper)
	{

	}

	/// <summary>
	/// Initialize new instance of <see cref="ExpanderHandler"/>.
	/// </summary>
	public ExpanderHandler() : this(ExpanderMapper, ExpanderCommandMapper)
	{
	}
}

#if ANDROID || IOS || MACCATALYST || WINDOWS
public partial class ExpanderHandler : ViewHandler<IExpander, MauiExpander>
{
	/// <inheritdoc />
#if ANDROID
	protected override MauiExpander CreatePlatformView() => new(Context);
#else
	protected override MauiExpander CreatePlatformView() => new();
#endif

	/// <summary>
	/// Action that's triggered when the <see cref="IExpander.Header"/> property changes.
	/// </summary>
	public static void MapHeader(ExpanderHandler handler, IExpander view)
	{
		ArgumentNullException.ThrowIfNull(handler.MauiContext);
		handler.PlatformView.SetHeader(view.Header, handler.MauiContext);
	}

	/// <summary>
	/// Action that's triggered when the <see cref="Core.IExpander.Content"/> property changes.
	/// </summary>
	public static void MapContent(ExpanderHandler handler, IExpander view)
	{
		ArgumentNullException.ThrowIfNull(handler.MauiContext);
		handler.PlatformView.SetContent(view.Content, handler.MauiContext);
	}

	/// <summary>
	/// Action that's triggered when the <see cref="IExpander.IsExpanded"/> property changes.
	/// </summary>
	public static void MapIsExpanded(ExpanderHandler handler, IExpander view)
	{
		handler.PlatformView.SetIsExpanded(view.IsExpanded);
	}

	/// <summary>
	/// Action that's triggered when the <see cref="Core.IExpander.Direction"/> property changes.
	/// </summary>
	public static void MapDirection(ExpanderHandler handler, IExpander view)
	{
		handler.PlatformView.SetDirection(view.Direction);
	}

	/// <inheritdoc />
	protected override void ConnectHandler(MauiExpander platformView)
	{
		base.ConnectHandler(platformView);
		platformView.Collapsed += OnCollapsedChanged;
#if WINDOWS
		platformView.Expanding += OnExpanding;
#endif
	}

	/// <inheritdoc />
	protected override void DisconnectHandler(MauiExpander platformView)
	{
		platformView.Collapsed -= OnCollapsedChanged;
#if WINDOWS
		platformView.Expanding -= OnExpanding;
#endif
		base.DisconnectHandler(platformView);
	}

	void OnCollapsedChanged(object? sender, ExpanderCollapsedEventArgs e)
	{
#if WINDOWS
		VirtualView.IsExpanded = false;
		VirtualView.ExpandedChanged(false);
#else
		VirtualView.IsExpanded = !e.IsCollapsed;
		VirtualView.ExpandedChanged(!e.IsCollapsed);
#endif
	}

#if WINDOWS
	void OnExpanding(object? sender, ExpanderExpandingEventArgs e)
	{
		VirtualView.IsExpanded = true;
		VirtualView.ExpandedChanged(true);
	}
#endif
}
#else
	public partial class ExpanderHandler : ViewHandler<IExpander, object>
{
	/// <inheritdoc />
	protected override object CreatePlatformView() => throw new NotSupportedException();
	
	/// <summary>
	/// Action that's triggered when the <see cref="IExpander.Header"/> property changes.
	/// </summary>
	public static void MapHeader(ExpanderHandler handler, IExpander view)
	{
	}

	/// <summary>
	/// Action that's triggered when the <see cref="IExpander.Content"/> property changes.
	/// </summary>
	public static void MapContent(ExpanderHandler handler, IExpander view)
	{
	}

	/// <summary>
	/// Action that's triggered when the <see cref="IExpander.IsExpanded"/> property changes.
	/// </summary>
	public static void MapIsExpanded(ExpanderHandler handler, IExpander view)
	{
	}

	/// <summary>
	/// Action that's triggered when the <see cref="IExpander.Direction"/> property changes.
	/// </summary>
	public static void MapDirection(ExpanderHandler handler, IExpander view)
	{
	}
}
#endif