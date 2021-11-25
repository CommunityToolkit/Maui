namespace CommunityToolkit.Maui.Views.Popup.SnackBar.Platforms;

class PlatformPopupExtensions : IPlatformPopupExtensions
{
	public void Dismiss(Snackbar snackbar)
	{
		snackbar.OnDismissed();
	}

	public object Show(Snackbar snackBar)
	{
		return new object();
	}
}
