using System;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Views.Popup.Snackbar;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Extensions;

/// <summary>
/// Extension methods for <see cref="VisualElement"/>.
/// </summary>
public static class VisualElementExtension
{
	/// <summary>
	/// Display snackbar with the anchor
	/// </summary>
	/// <param name="visualElement">Anchor element</param>
	/// <param name="message">Text of the snackbar</param>
	/// <param name="actionButtonText">Text of the snackbar button</param>
	/// <param name="action">Action of the snackbar button</param>
	/// <param name="duration">Snackbar duration</param>
	/// <param name="visualOptions">Snackbar visual options</param>
	/// <returns><see cref="Snackbar"/></returns>
	public static Task DisplaySnackbar(
		this VisualElement? visualElement,
		string message,
		Action action,
		string actionButtonText = "OK",
		TimeSpan? duration = null,
		SnackbarOptions? visualOptions = null) => Snackbar.Make(message, action, actionButtonText, duration, visualOptions, visualElement).Show();
}