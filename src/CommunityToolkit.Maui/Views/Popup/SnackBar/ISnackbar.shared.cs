using System;
using System.Threading.Tasks;
using Microsoft.Maui;

namespace CommunityToolkit.Maui.Views.Popup.SnackBar;

/// <summary>
/// Snackbar
/// </summary>
public interface ISnackbar: IDisposable
{
	/// <summary>
	/// Action to invoke to action button click
	/// </summary>
	Action Action { get; }

	/// <summary>
	/// Snackbar action button text
	/// </summary>
	string ActionButtonText { get; }

	/// <summary>
	/// Snackbar anchor
	/// </summary>
	IView? Anchor { get; }

	/// <summary>
	/// Snackbar duration
	/// </summary>
	TimeSpan Duration { get; }

	/// <summary>
	/// Snackbar message
	/// </summary>
	string Text { get; }

	/// <summary>
	/// Snackbar visual options
	/// </summary>
	SnackbarOptions VisualOptions { get; }

	/// <summary>
	/// Dismiss the snackbar
	/// </summary>
	Task Dismiss();

	/// <summary>
	/// Show the snackbar
	/// </summary>
	Task Show();
}