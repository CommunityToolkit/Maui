using System;
using System.Threading.Tasks;
using Microsoft.Maui;

namespace CommunityToolkit.Maui.Views.Popup.SnackBar;

/// <summary>
/// Snackbar
/// </summary>
public interface ISnackbar
{
	/// <summary>
	/// Action to invoke to action button click
	/// </summary>
	Action Action { get; set; }

	/// <summary>
	/// Snackbar action button text
	/// </summary>
	string ActionButtonText { get; set; }

	/// <summary>
	/// Snackbar anchor
	/// </summary>
	IView? Anchor { get; set; }

	/// <summary>
	/// Snackbar duration
	/// </summary>
	TimeSpan Duration { get; set; }

	/// <summary>
	/// Returns true if snackbar is shown, otherwise false
	/// </summary>
	bool IsShown { get; }

	/// <summary>
	/// Snackbar message
	/// </summary>
	string Text { get; set; }

	/// <summary>
	/// Snackbar visual options
	/// </summary>
	SnackbarOptions VisualOptions { get; set; }

	/// <summary>
	/// Event invokes when snackbar dismissed
	/// </summary>
	event EventHandler? Dismissed;

	/// <summary>
	/// Event invokes when snackbar is shown
	/// </summary>
	event EventHandler<ShownEventArgs>? Shown;

	/// <summary>
	/// Dismiss the snackbar
	/// </summary>
	Task Dismiss();

	/// <summary>
	/// Show the snackbar
	/// </summary>
	Task Show();
}