using Android.OS;
using AndroidX.Concurrent.Futures;
using AndroidX.Media3.Session;
using CommunityToolkit.Maui.Media.Services;

namespace CommunityToolkit.Maui.Services;

sealed partial class MediaSessionCallback(MediaControlsService mediaControlsService) : Java.Lang.Object, MediaSession.ICallback
{
	public const string PlayerIdKey = "CommunityToolkit.Maui.MediaElement.PlayerId";
	public const string ReleasePlayer = "ReleasePlayer";
	readonly MediaControlsService mediaControlsService = mediaControlsService;

	public MediaSession.ConnectionResult OnConnect(MediaSession? session, MediaSession.ControllerInfo? controller)
	{
		ArgumentNullException.ThrowIfNull(session);
		ArgumentNullException.ThrowIfNull(controller);

		var sessionCommands = MediaSession.ConnectionResult.DefaultSessionCommands?.BuildUpon()?
			.Add(new SessionCommand(ReleasePlayer, new Bundle()))?
			.Build() ?? throw new InvalidOperationException("Failed to build session commands.");

		return new MediaSession.ConnectionResult.AcceptedResultBuilder(session)
		  .SetAvailableSessionCommands(sessionCommands)?
			.Build() ?? throw new InvalidOperationException("Failed to build connection result.");
	}

	public global::Google.Common.Util.Concurrent.IListenableFuture? OnCustomCommand(MediaSession? session, MediaSession.ControllerInfo? controller, SessionCommand? customCommand, Bundle? args)
	{
        return CallbackToFutureAdapter.GetFuture(new FutureResolver(mediaControlsService, controller, customCommand, args));
	}

	sealed class FutureResolver(
		MediaControlsService mediaControlsService,
		MediaSession.ControllerInfo? controller,
		SessionCommand? customCommand,
		Bundle? args) : Java.Lang.Object, CallbackToFutureAdapter.IResolver
	{
		readonly Bundle? args = args;
		readonly SessionCommand? customCommand = customCommand;
		readonly MediaSession.ControllerInfo? controller = controller;
		readonly MediaControlsService mediaControlsService = mediaControlsService;

		public Java.Lang.Object? AttachCompleter(CallbackToFutureAdapter.Completer? completer)
		{
			ArgumentNullException.ThrowIfNull(completer);

			try
			{
				if (customCommand?.CustomAction?.Equals(ReleasePlayer, StringComparison.Ordinal) == true)
				{
					var playerId = args?.GetString(PlayerIdKey) ?? controller?.ConnectionHints?.GetString(PlayerIdKey);
					mediaControlsService.ReleasePlayer(playerId);
				}

				completer.Set(new SessionResult(SessionResult.ResultSuccess));
			}
			catch (InvalidOperationException exception)
			{
				completer.SetException(new Java.Lang.RuntimeException(exception.Message));
			}

			return customCommand?.CustomAction ?? ReleasePlayer;
		}
	}
}