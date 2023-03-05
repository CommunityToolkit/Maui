using System;
using System.Diagnostics.CodeAnalysis;
#if IOS || MACCATALYST
using PlatformView = UIKit.UIView;
#elif ANDROID
using PlatformView = Android.Views.View;
#elif WINDOWS
using PlatformView = Microsoft.UI.Xaml.FrameworkElement;
#elif TIZEN
using PlatformView = Tizen.NUI.BaseComponents.View;
#elif (NETSTANDARD || !PLATFORM) || (NET6_0_OR_GREATER && !IOS && !ANDROID && !TIZEN)
using PlatformView = System.Object;
#endif

namespace CommunityToolkit.Maui.Core.Extensions;

static class ViewExtensions
{
	public static bool TryGetPlatformView(this IView view, [NotNullWhen(true)] out PlatformView? platformView)
	{
		if (view.Handler?.PlatformView is PlatformView platform)
		{
			platformView = platform;
			return true;
		}

		platformView = null;
		return false;
	}
}
