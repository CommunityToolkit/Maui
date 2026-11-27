namespace CommunityToolkit.Maui.Core;

/// <summary>
/// The result or exception of showing or closing a popup with a Try* method, such as <c>TryShowPopupAsync</c> or <c>TryClosePopupAsync</c>:
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
/// <para>The Try methods never return a default value. A default <see cref="PopupResultOrException"/> holds a <see langword="null"/> <c>Value</c>, so add a <see langword="null"/> arm when a value can be default, such as an unassigned field.</para>
/// <para>Pattern matching on the union needs C# 15, the default for net11.0. On an older <c>LangVersion</c>, switch on <c>Value</c> and add a discard (<c>_</c>) arm.</para>
/// </remarks>
public union PopupResultOrException(IPopupResult, Exception);

/// <summary>
/// The result or exception of showing or closing a popup that returns a value of type <typeparamref name="T"/> with a Try* method, such as <c>TryShowPopupAsync&lt;T&gt;</c> or <c>TryClosePopupAsync&lt;T&gt;</c>:
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
/// <para>The Try methods never return a default value. A default <see cref="PopupResultOrException{T}"/> holds a <see langword="null"/> <c>Value</c>, so add a <see langword="null"/> arm when a value can be default, such as an unassigned field.</para>
/// <para>Pattern matching on the union needs C# 15, the default for net11.0. On an older <c>LangVersion</c>, switch on <c>Value</c> and add a discard (<c>_</c>) arm.</para>
/// </remarks>
public union PopupResultOrException<T>(IPopupResult<T>, Exception);