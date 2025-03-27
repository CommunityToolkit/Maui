using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui;

/// <summary>
/// Represents the result of a popup.
/// </summary>
/// <param name="WasDismissedByTappingOutsideOfPopup">True if Popup is closed by tapping outside popup</param>
record PopupResult(bool WasDismissedByTappingOutsideOfPopup) : IPopupResult;

/// <summary>
/// Represents the result of a popup when T is a reference type.
/// </summary>
/// <typeparam name="T">Popup result type</typeparam>
sealed record PopupResult<T> : PopupResult, IPopupResult<T>
{
	/// <summary>
	/// Initialize <see cref="PopupResult{T}"/>.
	/// </summary>
	/// <param name="result">Popup result</param>
	/// <param name="wasDismissedByTappingOutsideOfPopup">True if Popup is closed by tapping outside the popup</param>
	public PopupResult(object? result, bool wasDismissedByTappingOutsideOfPopup) : base(wasDismissedByTappingOutsideOfPopup)
	{
		Result = (T?)result;
	}

	public T? Result { get; }
}