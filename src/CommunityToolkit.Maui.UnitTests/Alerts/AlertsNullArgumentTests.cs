using CommunityToolkit.Maui.Alerts;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Alerts;

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
public class AlertsNullArgumentTests : BaseTest
{
	[Fact]
	public void SnackbarMake_NullMessage_ShouldThrowArgumentNullException()
	{
		NullArgumentAssert.Throws("message", () => Snackbar.Make(null));
	}

	[Fact]
	public void SnackbarMake_NullActionButtonText_ShouldThrowArgumentNullException()
	{
		NullArgumentAssert.Throws("actionButtonText", () => Snackbar.Make(string.Empty, actionButtonText: null));
	}

	[Fact]
	public void DisplaySnackbar_NullMessage_ShouldThrowArgumentNullException()
	{
		NullArgumentAssert.Throws("message", () => { _ = new Button().DisplaySnackbar(null, token: TestContext.Current.CancellationToken); });
	}

	[Fact]
	public void DisplaySnackbar_NullActionButtonText_ShouldThrowArgumentNullException()
	{
		NullArgumentAssert.Throws("actionButtonText", () => { _ = new Button().DisplaySnackbar(string.Empty, actionButtonText: null, token: TestContext.Current.CancellationToken); });
	}

	[Fact]
	public void ToastMake_NullMessage_ShouldThrowArgumentNullException()
	{
		NullArgumentAssert.Throws("message", () => Toast.Make(null));
	}
}
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.