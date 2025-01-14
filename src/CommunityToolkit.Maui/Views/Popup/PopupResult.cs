namespace CommunityToolkit.Maui.Core;

/// <summary>
/// 
/// </summary>
public record PopupResult(bool WasDismissedByTappingOutsideOfPopup);

/// <summary>
/// 
/// </summary>
/// <typeparam name="T"></typeparam>
/// <param name="Result"></param>
/// <param name="WasDismissedByTappingOutsideOfPopup"></param>
public record PopupResult<T>(T? Result, bool WasDismissedByTappingOutsideOfPopup) : PopupResult(WasDismissedByTappingOutsideOfPopup);