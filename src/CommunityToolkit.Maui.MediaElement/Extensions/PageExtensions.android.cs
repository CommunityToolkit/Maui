using Android.Views;
using Activity = Android.App.Activity;

namespace CommunityToolkit.Maui.Extensions;
partial class PageExtensions
{
	public record struct CurrentPlatformActivity()
	{
		public static Activity CurrentActivity
		{
			get
			{
				if (Platform.CurrentActivity is null)
				{
					throw new InvalidOperationException("CurrentActivity cannot be null");
				}
				return Platform.CurrentActivity;
			}
		}
		public static Android.Views.Window CurrentWindow
		{
			get
			{
				if (Platform.CurrentActivity?.Window is null)
				{
					throw new InvalidOperationException("Window cannot be null");
				}
				return Platform.CurrentActivity.Window;
			}
		}
		public static Android.Views.ViewGroup CurrentViewGroup
		{
			get
			{
				if (Platform.CurrentActivity?.Window?.DecorView is not ViewGroup viewGroup)
				{
					throw new InvalidOperationException("DecorView cannot be null");
				}
				return viewGroup;
			}
		}
	}
}
