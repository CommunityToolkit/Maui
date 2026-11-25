using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Extensions;

namespace CommunityToolkit.Maui;

/// <summary>
/// Represents the result of a popup.
/// </summary>
/// <remarks>
/// Make sure to check the <see cref="WasDismissedByTappingOutsideOfPopup"/> value to determine how the popup was closed.
/// <see cref="Exception"/> is only set by the Try* methods of <see cref="Extensions.PopupExtensions"/>, such as <c>TryShowPopupAsync</c> and <c>TryClosePopupAsync</c>, when the operation failed.
/// </remarks>
public record PopupResult : IPopupResult, IResult
{
	/// <summary>
	/// Initializes a new instance of <see cref="PopupResult"/>.
	/// </summary>
	/// <param name="wasDismissedByTappingOutsideOfPopup">True if the popup was closed without a result.</param>
	/// <param name="exception">The exception if the operation failed.</param>
	internal PopupResult(bool wasDismissedByTappingOutsideOfPopup, Exception? exception = null)
	{
		WasDismissedByTappingOutsideOfPopup = wasDismissedByTappingOutsideOfPopup;
		Exception = exception;
	}

	/// <inheritdoc/>
	public bool WasDismissedByTappingOutsideOfPopup { get; }

	/// <inheritdoc/>
	public Exception? Exception { get; }

	/// <inheritdoc/>
	[MemberNotNullWhen(false, nameof(Exception))]
	public bool IsSuccessful => Exception is null;

	/// <inheritdoc/>
	public bool IsCancelled => Exception is OperationCanceledException;

	/// <inheritdoc/>
	public void EnsureSuccess()
	{
		if (!IsSuccessful)
		{
			ExceptionDispatchInfo.Throw(Exception);
		}
	}
}

/// <summary>
/// Represents the result of a popup that returns a value of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">Popup result type</typeparam>
public sealed record PopupResult<T> : PopupResult, IPopupResult<T>
{
	/// <summary>
	/// Initializes a new instance of <see cref="PopupResult{T}"/>.
	/// </summary>
	/// <param name="result">Popup result</param>
	/// <param name="wasDismissedByTappingOutsideOfPopup">True if the popup was closed without a result.</param>
	/// <param name="exception">The exception if the operation failed.</param>
	internal PopupResult(object? result, bool wasDismissedByTappingOutsideOfPopup, Exception? exception = null) : base(wasDismissedByTappingOutsideOfPopup, exception)
	{
		Result = typeof(T).IsNullable()
			? (T?)result // Nullable types will be cast to `T`, e.g. bool?, object?
			: (T?)(result ?? default(T)); // Non-Nullable types will only be cast to `T` when `result` is not null because .NET will throw an InvalidCastException when casting `null` to a value-type (e.g. bool, int). When result is null and T is a value-type, we will use the default type of T (e.g. default(int) == 0) and defer throwing the InvalidCastException until the developer retrieves the value of Result.
	}

	/// <inheritdoc/>
	public T? Result => WasDismissedByTappingOutsideOfPopup && !typeof(T).IsNullable()
		? throw new PopupResultException($"{nameof(Result)} is null, but {nameof(PopupResult)} type, {typeof(T)}, cannot be converted to null. "
										 + $"Every time {nameof(WasDismissedByTappingOutsideOfPopup)} is {true}, {nameof(Result)} is always null. "
										 + $"When using a non-nullable type (e.g. bool) be sure to first check if {nameof(WasDismissedByTappingOutsideOfPopup)} is {false} before getting the value of {nameof(Result)}")
		: field;
}

sealed class PopupResultException(string message) : InvalidCastException(message);