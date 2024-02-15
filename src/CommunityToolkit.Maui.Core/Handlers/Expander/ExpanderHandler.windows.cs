using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml.Controls;

namespace CommunityToolkit.Maui.Core.Handlers;

public partial class ExpanderHandler : ViewHandler<IExpander, Expander>
{
	/// <summary>
	/// 
	/// </summary>
	/// <param name="handler"></param>
	/// <param name="view"></param>
	/// <exception cref="NotImplementedException"></exception>
	public static void MapIsExpanded(ExpanderHandler handler, IExpander view)
	{
		handler.PlatformView.IsExpanded = view.IsExpanded;
	}

	/// <summary>
	/// Action that's triggered when the Expander <see cref="IExpander.Direction"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="ExpanderHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IExpander"/>.</param>
	public static void MapDirection(ExpanderHandler handler, IExpander view)
	{
		handler.PlatformView.ExpandDirection = view.Direction switch
		{
			ExpandDirection.Down => Microsoft.UI.Xaml.Controls.ExpandDirection.Down,
			ExpandDirection.Up => Microsoft.UI.Xaml.Controls.ExpandDirection.Up,
			_ => handler.PlatformView.ExpandDirection
		};
	}

	/// <summary>
	/// Action that's triggered when the Expander <see cref="IExpander.Header"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="ExpanderHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IExpander"/>.</param>
	public static void MapHeader(ExpanderHandler handler, IExpander view)
	{
		handler.PlatformView.Header = view.Header?.ToPlatform(handler.MauiContext!);
	}

	/// <summary>
	/// Action that's triggered when the Expander <see cref="IExpander.Content"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="ExpanderHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IExpander"/>.</param>
	public static void MapContent(ExpanderHandler handler, IExpander view)
	{
		handler.PlatformView.Content = view.Content?.ToPlatform(handler.MauiContext!);
	}

	/// <inheritdoc/>
	protected override Expander CreatePlatformView()
	{
		return new Expander();
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="platformView"></param>
	protected override void ConnectHandler(Expander platformView)
	{
		base.ConnectHandler(platformView);
		platformView.Expanding += PlatformView_Expanding;
		platformView.Collapsed += PlatformView_Collapsed;
	}

	void PlatformView_Collapsed(Expander sender, ExpanderCollapsedEventArgs args)
	{
		VirtualView.IsExpanded = sender.IsExpanded;
	}

	void PlatformView_Expanding(Expander sender, ExpanderExpandingEventArgs args)
	{
		VirtualView.IsExpanded = sender.IsExpanded;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="platformView"></param>
	protected override void DisconnectHandler(Expander platformView)
	{
		platformView.Expanding -= PlatformView_Expanding;
		platformView.Collapsed -= PlatformView_Collapsed;
		base.DisconnectHandler(platformView);
	}
}