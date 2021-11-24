using System;

namespace CommunityToolkit.Maui.Views.Popup;

/// <summary>
/// Shown Event arguments
/// </summary>
public class ShownEventArgs : EventArgs
{
	/// <summary>
	/// Initialize a new instance of <see cref="ShownEventArgs"/>
	/// </summary>
	public ShownEventArgs(bool isShown)
	{
		IsShown = isShown;
	}

	/// <summary>
	/// Is shown
	/// </summary>
	public bool IsShown { get; }
}