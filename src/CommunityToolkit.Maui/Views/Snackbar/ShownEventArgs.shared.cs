using System;

namespace CommunityToolkit.Maui.Controls.Snackbar;

public class ShownEventArgs : EventArgs
{
	public ShownEventArgs(bool isShown)
	{
		IsShown = isShown;
	}

	public bool IsShown { get; }
}