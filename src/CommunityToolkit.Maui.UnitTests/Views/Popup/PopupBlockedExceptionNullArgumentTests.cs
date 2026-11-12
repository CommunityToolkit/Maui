using CommunityToolkit.Maui.Views;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class PopupBlockedExceptionNullArgumentTests : BaseTest
{
	[Fact]
	public void PopupBlockedException_NullPage_ShouldThrowArgumentNullException()
	{
		Page? page = null;

#pragma warning disable CS8604 // Possible null reference argument.
		NullArgumentAssert.Throws("currentVisibleModalPage", () => _ = new PopupBlockedException(page));
#pragma warning restore CS8604 // Possible null reference argument.
	}
}