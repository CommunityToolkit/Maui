using CommunityToolkit.Maui.MediaView.PlatformView;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.MediaView;

public partial class MediaViewHandler : ViewHandler<MediaView, MauiMediaView>, IDisposable
{
	/// <inheritdoc/>
	/// <exception cref="NullReferenceException">Thrown if <see cref="MauiContext"/> is <see langword="null"/>.</exception>
	protected override MauiMediaView CreatePlatformView()
	{
		mediaManager ??= new(MauiContext ?? throw new InvalidOperationException($"{nameof(MauiContext)} cannot be null"), VirtualView);
		var (_, playerViewController) = mediaManager.CreatePlatformView();
		return new(playerViewController);
	}

	/// <inheritdoc/>
	protected override void ConnectHandler(MauiMediaView platformView)
	{
		base.ConnectHandler(platformView);
	}

	/// <inheritdoc/>
	protected override void DisconnectHandler(MauiMediaView platformView)
	{
		platformView.Dispose();
		Dispose();
		base.DisconnectHandler(platformView);
	}
}