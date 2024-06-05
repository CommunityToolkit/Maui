using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Handlers;

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

		if (VirtualView.TryFindParent<Page>(out var page))
		{
			var parentViewController = (page.Handler as PageHandler)?.ViewController;
			return new(playerViewController, parentViewController);
		}

		return new(playerViewController, null);
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

static class ParentPage
{
	/// <summary>
	/// Extension method to find the Parent of <see cref="VisualElement"/>.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="child"></param>
	/// <param name="parent"></param>
	/// <returns></returns>
	public static bool TryFindParent<T>(this VisualElement? child, [NotNullWhen(true)] out T? parent) where T : VisualElement
	{
		while (true)
		{
			if (child is null)
			{
				parent = null;
				return false;
			}
			if (child.Parent is T element)
			{
				parent = element;
				return true;
			}

			child = child.Parent as VisualElement;
		}
	}
}