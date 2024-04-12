using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace CommunityToolkit.Maui.Core.Handlers;

public partial class MediaElementHandler : ViewHandler<MediaElement, MauiMediaElement>, IDisposable
{
	/// <inheritdoc/>
	/// <exception cref="NullReferenceException">Thrown if <see cref="MauiContext"/> is <see langword="null"/>.</exception>
	protected override MauiMediaElement CreatePlatformView()
	{
		if (MauiContext is null)
		{
			throw new InvalidOperationException($"{nameof(MauiContext)} cannot be null");
		}

		mediaManager ??= new(MauiContext,
								VirtualView,
								Dispatcher.GetForCurrentThread() ?? throw new InvalidOperationException($"{nameof(IDispatcher)} cannot be null"));

		var (_, playerViewController) = mediaManager.CreatePlatformView();
		var page = VirtualView.FindParent<Page>();
		var parentViewController = (page?.Handler as PageHandler)?.ViewController;

		return new(playerViewController, parentViewController);
	}

	/// <inheritdoc/>
	protected override void ConnectHandler(MauiMediaElement platformView)
	{
		base.ConnectHandler(platformView);
	}

	/// <inheritdoc/>
	protected override void DisconnectHandler(MauiMediaElement platformView)
	{
		platformView.Dispose();
		Dispose();
		base.DisconnectHandler(platformView);
	}
}

/// <summary>
/// An extension class for finding the Parent of <see cref="VisualElement"/>.
/// </summary>
public static class ParentPage
{
	/// <summary>
	/// Extension method to find the Parent of <see cref="VisualElement"/>.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="child"></param>
	/// <returns></returns>
	public static T? FindParent<T>(this VisualElement? child) where T : VisualElement
	{
		while (true)
		{
			if (child == null)
			{
				return null;
			}
			if (child.Parent is T parent)
			{
				return parent;
			}
			child = child.Parent as VisualElement;
		}
	}
}