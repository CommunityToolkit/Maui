using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui;

/// <inheritdoc cref="IClosePopupResult" />
record ClosePopupResult(bool WasDismissedByTappingOutsideOfPopup, Exception? Exception)
	: PopupResult(WasDismissedByTappingOutsideOfPopup), IClosePopupResult;

record ClosePopupResult<T> : PopupResult<T>, IClosePopupResult<T>
{
	public ClosePopupResult(object? result, bool wasDismissedByTappingOutsideOfPopup, Exception? exception)
		: base(result, wasDismissedByTappingOutsideOfPopup)
	{
		Exception = exception;
	}

	public ClosePopupResult(IPopupResult<T> popupResult, Exception? exception)
		: this(popupResult.WasDismissedByTappingOutsideOfPopup ? null : popupResult.Result,
			popupResult.WasDismissedByTappingOutsideOfPopup,
			exception)
	{
		
	}
	
	public Exception? Exception { get; }
}