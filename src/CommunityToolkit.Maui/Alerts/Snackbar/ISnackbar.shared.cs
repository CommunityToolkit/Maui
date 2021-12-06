using System;
using System.Threading.Tasks;
using Microsoft.Maui;

namespace CommunityToolkit.Maui.Alerts.Snackbar;

/// <summary>
/// Snackbar
/// </summary>
public interface ISnackbar : IAsyncDisposable
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