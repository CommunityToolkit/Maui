using Foundation;

namespace CommunityToolkit.Maui.MediaElement.Helpers;

static class ObserverExtensions
{

	public static void DisposeObservers(ref IDisposable? disposable)
	{
		disposable?.Dispose();
		disposable = null;
	}

	public static void DisposeObservers(ref NSObject? disposable)
	{
		disposable?.Dispose();
		disposable = null;
	}
}

