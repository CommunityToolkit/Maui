using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class PopupResultOrExceptionTests : BaseTest
{
	[Fact]
	public void PopupResultOrException_ShouldHoldPopupResult_WhenConvertedFromIPopupResult()
	{
		// Arrange
		IPopupResult popupResult = new PopupResult(true);

		// Act
		PopupResultOrException resultOrException = popupResult;

		// Assert
		Assert.Same(popupResult, resultOrException.Value);
		Assert.Equal("Dismissed", Describe(resultOrException));
	}

	[Fact]
	public void PopupResultOrException_ShouldHoldException_WhenConvertedFromException()
	{
		// Arrange
		var exception = new InvalidOperationException("failed");

		// Act
		PopupResultOrException resultOrException = exception;

		// Assert
		Assert.Same(exception, resultOrException.Value);
		Assert.Equal("Failed: failed", Describe(resultOrException));
	}

	[Fact]
	public void PopupResultOrException_ShouldMatchOperationCanceledException_BeforeException()
	{
		// Act
		PopupResultOrException resultOrException = new OperationCanceledException();

		// Assert
		Assert.Equal("Canceled", Describe(resultOrException));
	}

	[Fact]
	public void PopupResultOrException_ShouldMatchClosedPopupResult()
	{
		// Act
		PopupResultOrException resultOrException = new PopupResult(false);

		// Assert
		Assert.Equal("Closed", Describe(resultOrException));
	}

	[Fact]
	public void PopupResultOrException_Default_ShouldHoldNull()
	{
		// Act
		PopupResultOrException resultOrException = default;

		// Assert
		Assert.Null(resultOrException.Value);
		Assert.Equal("Default", resultOrException switch
		{
			IPopupResult => "PopupResult",
			Exception => "Exception",
			null => "Default"
		});
	}

	[Fact]
	public void PopupResultOrExceptionT_Default_ShouldHoldNull()
	{
		// Act
		PopupResultOrException<string> resultOrException = default;

		// Assert
		Assert.Null(resultOrException.Value);
		Assert.Equal("Default", resultOrException switch
		{
			IPopupResult<string> => "PopupResult",
			Exception => "Exception",
			null => "Default"
		});
	}

	[Fact]
	public void PopupResultOrExceptionT_ShouldHoldTypedPopupResult_WhenConvertedFromIPopupResultT()
	{
		// Arrange
		IPopupResult<int> popupResult = new PopupResult<int>(42, false);

		// Act
		PopupResultOrException<int> resultOrException = popupResult;

		// Assert
		Assert.Same(popupResult, resultOrException.Value);
		Assert.Equal("Closed with 42", Describe(resultOrException));
	}

	[Fact]
	public void PopupResultOrExceptionT_ShouldMatchDismissed_WithoutReadingResult()
	{
		// Arrange
		// Result throws for a non-nullable T when the popup was dismissed, so the dismissed case must be matched first
		PopupResultOrException<int> resultOrException = new PopupResult<int>(null, true);

		// Act / Assert
		Assert.Equal("Dismissed", Describe(resultOrException));
	}

	[Fact]
	public void PopupResultOrExceptionT_ShouldHoldException_WhenConvertedFromException()
	{
		// Arrange
		var exception = new PopupNotFoundException();

		// Act
		PopupResultOrException<string> resultOrException = exception;

		// Assert
		Assert.Same(exception, resultOrException.Value);
		Assert.Equal($"Failed: {exception.Message}", Describe(resultOrException));
	}

	[Fact]
	public void PopupResultOrExceptionT_ShouldMatchOperationCanceledException_BeforeException()
	{
		// Act
		PopupResultOrException<string> resultOrException = new TaskCanceledException();

		// Assert
		Assert.Equal("Canceled", Describe(resultOrException));
	}

	// No discard: the build reports CS8509 if these switches stop covering every case of PopupResultOrException
	static string Describe(PopupResultOrException resultOrException) => resultOrException switch
	{
		IPopupResult { WasDismissedByTappingOutsideOfPopup: true } => "Dismissed",
		IPopupResult => "Closed",
		OperationCanceledException => "Canceled",
		Exception exception => $"Failed: {exception.Message}"
	};

	static string Describe<T>(PopupResultOrException<T> resultOrException) => resultOrException switch
	{
		IPopupResult<T> { WasDismissedByTappingOutsideOfPopup: true } => "Dismissed",
		IPopupResult<T> popupResult => $"Closed with {popupResult.Result}",
		OperationCanceledException => "Canceled",
		Exception exception => $"Failed: {exception.Message}"
	};
}