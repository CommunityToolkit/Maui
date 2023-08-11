﻿using CommunityToolkit.Maui.Views;
using Microsoft.UI.Xaml.Controls;
using Grid = Microsoft.UI.Xaml.Controls.Grid;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// The user-interface element that represents the <see cref="MediaElement"/> on Windows.
/// </summary>
public class MauiMediaElement : Grid, IDisposable
{
	readonly MediaPlayerElement mediaElement;

	/// <summary>
	/// Initializes a new instance of the <see cref="MauiMediaElement"/> class.
	/// </summary>
	/// <param name="mediaElementElement"></param>
	public MauiMediaElement(MediaPlayerElement mediaElementElement)
	{
		this.mediaElement = mediaElementElement;
		Children.Add(this.mediaElement);
	}

	/// <summary>
	/// Releases the managed and unmanaged resources used by the <see cref="MauiMediaElement"/>.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Releases the managed and unmanaged resources used by the <see cref="MauiMediaElement"/>.
	/// </summary>
	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			mediaElement.MediaPlayer.Dispose();
		}
	}
}