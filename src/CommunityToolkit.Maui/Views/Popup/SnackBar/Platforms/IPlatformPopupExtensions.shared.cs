namespace CommunityToolkit.Maui.Views.Popup.SnackBar.Platforms;
#if NET6_0_ANDROID
using NativeSnackbar = Google.Android.Material.Snackbar.Snackbar;
#elif NET6_0_IOS || NET6_0_MACCATALYST
using NativeSnackbar = NativeSnackBar;
#else
using NativeSnackbar = System.Object;
#endif

interface IPlatformPopupExtensions
{
	public void Dismiss(Snackbar snackbar);
	public NativeSnackbar Show(Snackbar snackbar);
}