using CommunityToolkit.Maui.MediaPlayer.PlatformView;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.MediaPlayer;

public partial class MediaPlayerHandler : ViewHandler<Views.MediaPlayer, MauiMediaPlayer>, IDisposable
{
	/// <inheritdoc/>
	/// <exception cref="NullReferenceException">Thrown if <see cref="MauiContext"/> is <see langword="null"/>.</exception>
	protected override MauiMediaPlayer CreatePlatformView()
	{
		mediaManager ??= new(MauiContext ?? throw new InvalidOperationException($"{nameof(MauiContext)} cannot be null"), VirtualView);
		var (_, playerViewController) = mediaManager.CreatePlatformView();
		return new(playerViewController);
	}

	/// <inheritdoc/>
	protected override void ConnectHandler(MauiMediaPlayer platformView)
	{
		base.ConnectHandler(platformView);
	}

	/// <inheritdoc/>
	protected override void DisconnectHandler(MauiMediaPlayer platformView)
	{
		platformView.Dispose();
		Dispose();
		base.DisconnectHandler(platformView);
	}
}