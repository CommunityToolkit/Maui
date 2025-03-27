﻿using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;

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
		Result = typeof(T).IsNullable()
			? (T?)result // Nullable types will be cast to `T`, e.g. bool?, object?
			: (T?)(result ?? default(T)); // Non-Nullable types will only be cast to `T` when `result` is not null because .NET will throw an InvalidCastException when casting `null` to a value-type, e.g. bool, int. When result is null and T is a value-type, we will use the default type of T (e.g. default(int) == 0) and only throw the InvalidCastException when the developer retrieves the value of Result. 
	}

	public T? Result => WasDismissedByTappingOutsideOfPopup && !typeof(T).IsNullable()
		? throw new PopupResultException($"{nameof(PopupResult)} type, {typeof(T)}, cannot be converted to null. When {nameof(WasDismissedByTappingOutsideOfPopup)} is {true}, {nameof(Result)} is always null. When using a non-nullable type, e.g. bool, be sure to first check if {nameof(WasDismissedByTappingOutsideOfPopup)} is {false} before getting the value of result")
		: field;
}

sealed class PopupResultException(string message) : InvalidCastException(message);