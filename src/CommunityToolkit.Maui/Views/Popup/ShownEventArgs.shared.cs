using System;

namespace CommunityToolkit.Maui.Views.Popup;

public class ShownEventArgs : EventArgs
{
	public ShownEventArgs(bool isShown)
	{
		IsShown = isShown;
	}

	public bool IsShown { get; }
}