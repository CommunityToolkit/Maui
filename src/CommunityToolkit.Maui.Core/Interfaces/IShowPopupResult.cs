namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Represents a result that can be returned when a popup is opened
/// </summary>
public interface IShowPopupResult : IPopupResult, IResult;

/// <summary>
/// Represents a result that can be returned when a popup is opened.
/// </summary>
public interface IShowPopupResult<out T> : IPopupResult<T>, IResult;