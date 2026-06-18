using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui;

/// <summary>
/// Result of TryShowPopup
/// </summary>
/// <param name="Exception">Exception if operation failed</param>
record ShowPopupResult(bool WasDismissedByTappingOutsideOfPopup, Exception? Exception) : PopupResult(WasDismissedByTappingOutsideOfPopup), IShowPopupResult;

/// <summary>
/// Result of TryShowPopup
/// </summary>
record ShowPopupResult<T> : PopupResult<T>, IShowPopupResult<T>
{
	public ShowPopupResult(object? result, bool wasDismissedByTappingOutsideOfPopup, Exception? exception)
		: base(result, wasDismissedByTappingOutsideOfPopup)
	{
		Exception = exception;
	}

	public ShowPopupResult(IPopupResult<T> popupResult, Exception? exception)
		: this(popupResult.WasDismissedByTappingOutsideOfPopup ? null : popupResult.Result,
			popupResult.WasDismissedByTappingOutsideOfPopup,
			exception)
	{
		
	}
	
	public Exception? Exception { get; }
}