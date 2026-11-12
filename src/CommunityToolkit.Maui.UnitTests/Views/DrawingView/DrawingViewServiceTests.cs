using CommunityToolkit.Maui.Core.Views;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class DrawingViewServiceTests : BaseTest
{

	[Fact]
	public void GetImageStream_NullPointOptions_ThrowsArgumentNullException()
	{
#pragma warning disable CS8600, CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => DrawingViewService.GetImageStream((ImagePointOptions)null, TestContext.Current.CancellationToken));
#pragma warning restore CS8600, CS8625 // Cannot convert null literal to non-nullable reference type.

		Assert.Equal("options", exception.ParamName);
	}

	[Fact]
	public void GetImageStream_NullLineOptions_ThrowsArgumentNullException()
	{
#pragma warning disable CS8600, CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => DrawingViewService.GetImageStream((ImageLineOptions)null, TestContext.Current.CancellationToken));
#pragma warning restore CS8600, CS8625 // Cannot convert null literal to non-nullable reference type.

		Assert.Equal("options", exception.ParamName);
	}

	[Fact]
	public void GetPlatformImageStream_NullPointOptions_ThrowsArgumentNullException()
	{
#pragma warning disable CS8600, CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => DrawingViewService.GetPlatformImageStream((ImagePointOptions)null, TestContext.Current.CancellationToken));
#pragma warning restore CS8600, CS8625 // Cannot convert null literal to non-nullable reference type.

		Assert.Equal("options", exception.ParamName);
	}

	[Fact]
	public void GetPlatformImageStream_NullLineOptions_ThrowsArgumentNullException()
	{
#pragma warning disable CS8600, CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => DrawingViewService.GetPlatformImageStream((ImageLineOptions)null, TestContext.Current.CancellationToken));
#pragma warning restore CS8600, CS8625 // Cannot convert null literal to non-nullable reference type.

		Assert.Equal("options", exception.ParamName);
	}
}