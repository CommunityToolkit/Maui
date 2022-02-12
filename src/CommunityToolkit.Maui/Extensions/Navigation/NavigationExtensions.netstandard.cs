using System;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Extensions;

public static partial class NavigationExtensions
{
	static void PlatformShowPopup(BasePopup popup, IMauiContext mauiContext) =>
		throw new NotSupportedException($"The current platform '{Device.RuntimePlatform}' does not support CommunityToolkit.Maui.Core.BasePopup");

	static Task<object?> PlatformShowPopupAsync(Popup popup, IMauiContext mauiContext) =>
		throw new NotSupportedException($"The current platform '{Device.RuntimePlatform}' does not support CommunityToolkit.Maui.Core.Popup.");
}
