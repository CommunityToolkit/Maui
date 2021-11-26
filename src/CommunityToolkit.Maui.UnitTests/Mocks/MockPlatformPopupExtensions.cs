using CommunityToolkit.Maui.Views.Popup.Snackbar;
using CommunityToolkit.Maui.Views.Popup.Snackbar.Platforms;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

class MockPlatformPopupExtensions : IPlatformPopupExtensions
{
	public void Dismiss(Snackbar snackbar)
	{
		snackbar.OnDismissed();
	}

	public object Show(Snackbar snackbar)
	{
		return new object();
	}
}