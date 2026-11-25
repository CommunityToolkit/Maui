using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class PopupOutcomeTests : BaseTest
{
	[Fact]
	public void PopupOutcome_ShouldHoldPopupResult_WhenConvertedFromIPopupResult()
	{
		// Arrange
		IPopupResult popupResult = new PopupResult(true);

		// Act
		PopupOutcome outcome = popupResult;

		// Assert
		Assert.Same(popupResult, outcome.Value);
		Assert.Equal("Dismissed", Describe(outcome));
	}

	[Fact]
	public void PopupOutcome_ShouldHoldException_WhenConvertedFromException()
	{
		// Arrange
		var exception = new InvalidOperationException("failed");

		// Act
		PopupOutcome outcome = exception;

		// Assert
		Assert.Same(exception, outcome.Value);
		Assert.Equal("Failed: failed", Describe(outcome));
	}

	[Fact]
	public void PopupOutcome_ShouldMatchOperationCanceledException_BeforeException()
	{
		// Act
		PopupOutcome outcome = new OperationCanceledException();

		// Assert
		Assert.Equal("Canceled", Describe(outcome));
	}

	[Fact]
	public void PopupOutcome_ShouldMatchClosedPopupResult()
	{
		// Act
		PopupOutcome outcome = new PopupResult(false);

		// Assert
		Assert.Equal("Closed", Describe(outcome));
	}

	[Fact]
	public void PopupOutcome_Default_ShouldHoldNull()
	{
		// Act
		PopupOutcome outcome = default;

		// Assert
		Assert.Null(outcome.Value);
		Assert.Equal("Default", outcome switch
		{
			IPopupResult => "PopupResult",
			Exception => "Exception",
			null => "Default"
		});
	}

	[Fact]
	public void PopupOutcomeT_ShouldHoldTypedPopupResult_WhenConvertedFromIPopupResultT()
	{
		// Arrange
		IPopupResult<int> popupResult = new PopupResult<int>(42, false);

		// Act
		PopupOutcome<int> outcome = popupResult;

		// Assert
		Assert.Same(popupResult, outcome.Value);
		Assert.Equal("Closed with 42", Describe(outcome));
	}

	[Fact]
	public void PopupOutcomeT_ShouldMatchDismissed_WithoutReadingResult()
	{
		// Arrange
		// Result throws for a non-nullable T when the popup was dismissed, so the dismissed case must be matched first
		PopupOutcome<int> outcome = new PopupResult<int>(null, true);

		// Act / Assert
		Assert.Equal("Dismissed", Describe(outcome));
	}

	[Fact]
	public void PopupOutcomeT_ShouldHoldException_WhenConvertedFromException()
	{
		// Arrange
		var exception = new PopupNotFoundException();

		// Act
		PopupOutcome<string> outcome = exception;

		// Assert
		Assert.Same(exception, outcome.Value);
		Assert.Equal($"Failed: {exception.Message}", Describe(outcome));
	}

	[Fact]
	public void PopupOutcomeT_ShouldMatchOperationCanceledException_BeforeException()
	{
		// Act
		PopupOutcome<string> outcome = new TaskCanceledException();

		// Assert
		Assert.Equal("Canceled", Describe(outcome));
	}

	// The switch expressions have no discard: they compile only because the compiler knows that a PopupOutcome is either an IPopupResult or an Exception
	static string Describe(PopupOutcome outcome) => outcome switch
	{
		IPopupResult { WasDismissedByTappingOutsideOfPopup: true } => "Dismissed",
		IPopupResult => "Closed",
		OperationCanceledException => "Canceled",
		Exception exception => $"Failed: {exception.Message}"
	};

	static string Describe<T>(PopupOutcome<T> outcome) => outcome switch
	{
		IPopupResult<T> { WasDismissedByTappingOutsideOfPopup: true } => "Dismissed",
		IPopupResult<T> popupResult => $"Closed with {popupResult.Result}",
		OperationCanceledException => "Canceled",
		Exception exception => $"Failed: {exception.Message}"
	};
}