using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Extensions;

namespace CommunityToolkit.Maui;

/// <summary>
/// Represents the result of a popup.
/// </summary>
/// <param name="WasDismissed">True if Popup is closed by tapping outside popup</param>
record PopupResult(bool WasDismissed) : IPopupResult
{
	[Obsolete($"Please use {nameof(WasDismissed)} instead")]
	public bool WasDismissedByTappingOutsideOfPopup => WasDismissed;
}

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
	/// <param name="wasDismissed">True if Popup is closed by tapping outside the popup</param>
	public PopupResult(object? result, bool wasDismissed) : base(wasDismissed)
	{
		Result = typeof(T).IsNullable()
			? (T?)result // Nullable types will be cast to `T`, e.g. bool?, object?
			: (T?)(result ?? default(T)); // Non-Nullable types will only be cast to `T` when `result` is not null because .NET will throw an InvalidCastException when casting `null` to a value-type (e.g. bool, int). When result is null and T is a value-type, we will use the default type of T (e.g. default(int) == 0) and defer throwing the InvalidCastException until the developer retrieves the value of Result. 
	}

	public T? Result => WasDismissed && !typeof(T).IsNullable()
		? throw new PopupResultException($"{nameof(Result)} is null, but {nameof(PopupResult)} type, {typeof(T)}, cannot be converted to null. Every time {nameof(WasDismissed)} is {true}, {nameof(Result)} is always null. When using a non-nullable type (e.g. bool) be sure to first check if {nameof(WasDismissed)} is {false} before getting the value of {nameof(Result)}")
		: field;
}

sealed class PopupResultException(string message) : InvalidCastException(message);