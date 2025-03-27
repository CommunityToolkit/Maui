using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class PopupResultTests : BaseTest
{
	[Fact]
	public void PopupResult_Constructor_SetsProperties()
	{
		// Arrange
		bool wasDismissedByTappingOutside = true;

		// Act
		var result = new PopupResult(wasDismissedByTappingOutside);

		// Assert
		Assert.True(result.WasDismissedByTappingOutsideOfPopup);
	}

	[Fact]
	public void PopupResultT_Constructor_SetsProperties()
	{
		// Arrange
		var expectedResult = "Test Result";
		bool wasDismissedByTappingOutside = true;

		// Act
		var result = new PopupResult<string>(expectedResult, wasDismissedByTappingOutside);

		// Assert
		Assert.True(result.WasDismissedByTappingOutsideOfPopup);
		Assert.Equal(expectedResult, result.Result);
	}

	[Fact]
	public void PopupResultT_Constructor_AllowsNullResult()
	{
		// Arrange
		string? expectedResult = null;
		bool wasDismissedByTappingOutside = true;

		// Act
		var result = new PopupResult<string?>(expectedResult, wasDismissedByTappingOutside);

		// Assert
		Assert.True(result.WasDismissedByTappingOutsideOfPopup);
		Assert.Null(result.Result);
	}

	[Fact]
	public void PopupResultT_Constructor_AllowsValueTypeResult()
	{
		// Arrange
		int expectedResult = 42;
		bool wasDismissedByTappingOutside = false;

		// Act
		var result = new PopupResult<int>(expectedResult, wasDismissedByTappingOutside);

		// Assert
		Assert.False(result.WasDismissedByTappingOutsideOfPopup);
		Assert.Equal(expectedResult, result.Result);
	}

	[Fact]
	public void PopupResultT_Constructor_AllowsNullableValueTypeResult()
	{
		// Arrange
		int? expectedResult = null;
		bool wasDismissedByTappingOutside = true;

		// Act
		var result = new PopupResult<int?>(expectedResult, wasDismissedByTappingOutside);

		// Assert
		Assert.True(result.WasDismissedByTappingOutsideOfPopup);
		Assert.Null(result.Result);
	}

	[Fact]
	public void PopupResultT_Constructor_AllowsReferenceTypeResult()
	{
		// Arrange
		var expectedResult = new object();
		bool wasDismissedByTappingOutside = false;

		// Act
		var result = new PopupResult<object>(expectedResult, wasDismissedByTappingOutside);

		// Assert
		Assert.False(result.WasDismissedByTappingOutsideOfPopup);
		Assert.Equal(expectedResult, result.Result);
	}
}