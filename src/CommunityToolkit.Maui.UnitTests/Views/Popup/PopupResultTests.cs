using CommunityToolkit.Maui.Core;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class PopupResultTests : BaseTest
{
	[Fact]
	public void PopupResultT_NonNullableConstraint_ThrowsArgumentException()
	{
		Assert.Throws<TypeInitializationException>(() => new PopupResult<bool>(true, true));
	}
	
	[Fact]
	public void PopupResultT_NullableValueTypeConstraint_NoExceptionThrown()
	{
		var popupResult = new PopupResult<bool?>(true, true);
		
		Assert.True(popupResult.WasDismissedByTappingOutsideOfPopup);
		Assert.True(popupResult.Result);
	}
}