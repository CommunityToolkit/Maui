namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Snackbar
/// </summary>
public interface ISnackbar : IAlert
{
	/// <summary>
	/// Action to invoke on action button click
	/// </summary>
	Action? Action { get; }

	/// <summary>
	/// Snackbar action button text
	/// </summary>
	string ActionButtonText { get; }

	/// <summary>
	/// Snackbar anchor. Snackbar appears near this view
	/// </summary>
	IView? Anchor { get; }

	/// <summary>
	/// Snackbar duration
	/// </summary>
	TimeSpan Duration { get; }

	/// <summary>
	/// Snackbar visual options
	/// </summary>
	SnackbarOptions VisualOptions { get; }
}