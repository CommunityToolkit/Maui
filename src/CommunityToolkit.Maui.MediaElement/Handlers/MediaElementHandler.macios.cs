using CommunityToolkit.Maui.Core.Views;
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

		mediaManager ??= new(MauiContext, VirtualView);

		// Retrieve the parenting page so we can provide that to the platform control
		var parent = VirtualView.Parent;
		while (parent is not null)
		{
			if (parent is Page)
			{
				break;
			}

			parent = parent.Parent;
		}

		var parentPage = (parent as Page)?.ToHandler(MauiContext);

		var (_, playerViewController) = mediaManager.CreatePlatformView();
		return new(playerViewController, parentPage?.ViewController);
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