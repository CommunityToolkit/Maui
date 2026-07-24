namespace CommunityToolkit.Maui.Core;

/// <summary>
/// The result returned when the popup is closed programmatically.
/// </summary>
/// <remarks>
/// Make sure to check the <see cref="IPopupResult.WasDismissedByTappingOutsideOfPopup"/> value to determine how the popup was closed.
/// This will always return <c>null</c> when <see cref="IPopupResult.WasDismissedByTappingOutsideOfPopup"/> is <c>true</c>.
/// </remarks>
public interface IClosePopupResult : IPopupResult, IResult;

/// <summary>
/// The result returned when the popup is closed programmatically.
/// </summary>
/// <remarks>
/// Make sure to check the <see cref="IPopupResult.WasDismissedByTappingOutsideOfPopup"/> value to determine how the popup was closed.
/// This will always return <c>null</c> when <see cref="IPopupResult.WasDismissedByTappingOutsideOfPopup"/> is <c>true</c>.
/// </remarks>
public interface IClosePopupResult<out T> : IPopupResult<T>, IResult;