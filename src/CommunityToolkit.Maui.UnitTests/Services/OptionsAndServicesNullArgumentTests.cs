using CommunityToolkit.Maui.Services;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Services;

#pragma warning disable CS8604, CS8625 // Possible null reference argument; cannot convert null literal to non-nullable reference type.
public class OptionsAndServicesNullArgumentTests : BaseTest
{
	[Fact]
	public void PopupService_NullServiceProvider_ShouldThrowArgumentNullException()
	{
		NullArgumentAssert.Throws("serviceProvider", () => _ = new PopupService(null));
	}

	[Fact]
	public void Options_NullPopupDefaults_ShouldThrowArgumentNullException()
	{
		var options = new Options(MauiApp.CreateBuilder());

		NullArgumentAssert.Throws("globalPopupSettings", () => options.SetPopupDefaults(null));
		NullArgumentAssert.Throws("globalPopupOptionsSettings", () => options.SetPopupOptionsDefaults(null));
	}
}
#pragma warning restore CS8604, CS8625