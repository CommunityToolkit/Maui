namespace CommunityToolkit.Maui.Core;

/// <summary>
/// The outcome of showing or closing a popup with a Try* method, such as <c>TryShowPopupAsync</c> or <c>TryClosePopupAsync</c>:
/// either the <see cref="IPopupResult"/> of the closed popup, or the <see cref="System.Exception"/> that the operation failed with.
/// </summary>
/// <remarks>
/// <para>Use pattern matching to handle each case. The <see langword="switch"/> is exhaustive without a discard:</para>
/// <code>
/// var message = await page.TryShowPopupAsync(view) switch
/// {
///     IPopupResult { WasDismissedByTappingOutsideOfPopup: true } => "Dismissed",
///     IPopupResult => "Closed",
///     OperationCanceledException => "Canceled",
///     Exception exception => $"Failed: {exception.Message}"
/// };
/// </code>
/// </remarks>
public union PopupOutcome(IPopupResult, Exception);

/// <summary>
/// The outcome of showing or closing a popup that returns a value of type <typeparamref name="T"/> with a Try* method, such as <c>TryShowPopupAsync&lt;T&gt;</c> or <c>TryClosePopupAsync&lt;T&gt;</c>:
/// either the <see cref="IPopupResult{T}"/> of the closed popup, or the <see cref="System.Exception"/> that the operation failed with.
/// </summary>
/// <typeparam name="T">The type of the value that the popup returns.</typeparam>
/// <remarks>
/// <para>Use pattern matching to handle each case. The <see langword="switch"/> is exhaustive without a discard:</para>
/// <code>
/// var message = await page.TryShowPopupAsync&lt;string&gt;(view) switch
/// {
///     IPopupResult&lt;string&gt; { WasDismissedByTappingOutsideOfPopup: true } => "Dismissed",
///     IPopupResult&lt;string&gt; popupResult => $"Closed with {popupResult.Result}",
///     OperationCanceledException => "Canceled",
///     Exception exception => $"Failed: {exception.Message}"
/// };
/// </code>
/// </remarks>
public union PopupOutcome<T>(IPopupResult<T>, Exception);