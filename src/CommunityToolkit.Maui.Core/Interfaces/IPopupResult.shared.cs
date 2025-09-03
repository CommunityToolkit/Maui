namespace CommunityToolkit.Maui.Core;

/// <inheritdoc/>
public interface IPopupResult<out TResult> : IPopupResult
{
	/// <summary>
	/// The result returned when the popup is closed programmatically.
	/// </summary>
	/// <remarks>
	/// Make sure to check the <see cref="IPopupResult.WasDismissed"/> value to determine how the popup was closed.
	/// This will always return <c>null</c> when <see cref="IPopupResult.WasDismissed"/> is <c>true</c>.
	/// </remarks>
	TResult? Result { get; }
}

/// <summary>
/// Represents a result that can be returned when a popup is closed.
/// </summary>
/// <remarks>
/// Make sure to check the <see cref="IPopupResult.WasDismissed"/> value to determine how the popup was closed.
/// </remarks>
public interface IPopupResult
{
	/// <summary>
	/// Gets whether the popup was closed by tapping outside the popup.
	/// </summary>
	bool WasDismissed { get; }

	/// <summary>
	/// Gets whether the popup was closed by tapping outside the popup.
	/// </summary>
	[Obsolete($"Please use {nameof(WasDismissed)} instead")]
	bool WasDismissedByTappingOutsideOfPopup => WasDismissed;
}