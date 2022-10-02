using Android.Content;
using AndroidX.CoordinatorLayout.Widget;

namespace CommunityToolkit.Maui.MediaElement.PlatformView;

public class MauiMediaElement : CoordinatorLayout
{
	readonly Context context;

	public MauiMediaElement(Context context, MediaElement mediaElement)
		 : base(context)
	{
		// TODO
		this.context = context;
	}

	public void UpdateSource()
	{
	}

	public void UpdateSpeed()
	{
	}

	public void UpdateVolume()
	{
	}
}

