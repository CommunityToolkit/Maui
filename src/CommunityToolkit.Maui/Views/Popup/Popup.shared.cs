using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Views;

/// <summary>
/// Represents a small View that pops up at front the Page. Implements <see cref="PopupContainer"/>.
/// </summary>
public partial class Popup : ContentView
{
	PopupContainer? popupContainer;

	/// <summary>
	/// 
	/// </summary>
	public Action? OnOpened { get; set; }

	/// <summary>
	/// 
	/// </summary>
	public Action? OnClosed { get; set; }

	/// <summary>
	/// 
	/// </summary>
	public void Close()
	{
		popupContainer?.Close(new PopupResult(false));
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="result"></param>
	public void Close<T>(T result)
	{
		popupContainer?.Close(new PopupResult<T>(result, false));
	}

	internal void SetPopup(PopupContainer container, PopupOptions options)
	{
		popupContainer = container;

		OnOpened = options.OnOpened;
		OnClosed = options.OnClosed;
	}
}