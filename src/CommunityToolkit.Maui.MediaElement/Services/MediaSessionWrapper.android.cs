using AndroidX.Media3.ExoPlayer;
using AndroidX.Media3.Session;

namespace CommunityToolkit.Maui.Media.Services;

sealed class MediaSessionWrapper(string id, MediaSession session, IExoPlayer player, int notificationId) : IDisposable
{
	public string Id { get; } = id ?? throw new ArgumentNullException(nameof(id));
	public MediaSession Session { get; } = session ?? throw new ArgumentNullException(nameof(session));
	public IExoPlayer Player { get; } = player ?? throw new ArgumentNullException(nameof(player));
	public int NotificationId { get; set; } = notificationId;

	public void Dispose()
    {
		Session.Release();
		Player.Release();
	}
}