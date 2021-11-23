using System;
using System.Threading.Tasks;
using Microsoft.Maui;

namespace CommunityToolkit.Maui.Views.Popup.SnackBar;

public interface ISnackbar
{
	Action Action { get; set; }
	string ActionButtonText { get; set; }
	IView? Anchor { get; set; }
	TimeSpan Duration { get; set; }
	bool IsShown { get; }
	string Text { get; set; }
	SnackbarOptions VisualOptions { get; set; }

	event EventHandler? Dismissed;
	event EventHandler<ShownEventArgs>? Shown;

	Task Dismiss();
	Task Show();
}