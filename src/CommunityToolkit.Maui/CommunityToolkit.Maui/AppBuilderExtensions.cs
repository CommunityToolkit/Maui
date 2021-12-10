using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.LifecycleEvents;

namespace CommunityToolkit.Maui;
public static class AppBuilderExtensions
{
#if ANDROID
	internal static Android.App.Activity? Activity { get; private set; }
#endif
	public static MauiAppBuilder UseCommunityToolkit(this MauiAppBuilder builder)
	{
		builder.ConfigureLifecycleEvents(events =>
		{
#if ANDROID
			events.AddAndroid(android =>
			{
				android.OnCreate((a, b) =>
				{
					Activity = a;
				});
			});
#endif
		});



		return builder;
	}
}
