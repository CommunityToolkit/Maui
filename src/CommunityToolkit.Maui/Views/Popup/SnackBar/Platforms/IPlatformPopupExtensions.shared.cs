namespace CommunityToolkit.Maui.Views.Popup.SnackBar.Platforms;
#if NET6_0_ANDROID
using NativeSnackbar = Google.Android.Material.Snackbar.Snackbar;
#elif NET6_0_IOS || NET6_0_MACCATALYST
using NativeSnackbar = NativeSnackBar;
#elif NET6_0_WINDOWS10_0_17763_0
using NativeSnackbar = Windows.UI.Notifications.ToastNotification;
#else
using NativeSnackbar = System.Object;
#endif

interface IPlatformPopupExtensions
{
	void Dismiss(Snackbar snackbar);
	NativeSnackbar Show(Snackbar snackbar);
}