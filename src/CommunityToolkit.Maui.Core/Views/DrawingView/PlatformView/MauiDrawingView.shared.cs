using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// DrawingView Platform Control
/// </summary>
public partial class MauiDrawingView
{
	readonly WeakEventManager weakEventManager = new();

	/// <summary>
	/// Event raised when drawing line completed 
	/// </summary>
	public event EventHandler<MauiDrawingLineCompletedEventArgs> DrawingLineCompleted
	{
		add => weakEventManager.AddEventHandler(value);
		remove => weakEventManager.RemoveEventHandler(value);
	}

	/// <summary>
	/// Drawing Lines
	/// </summary>
	public ObservableCollection<MauiDrawingLine> Lines { get; } = new();

	/// <summary>
	/// Enable or disable multiline mode
	/// </summary>
	public bool MultiLineMode { get; set; }
	/// <summary>
	/// Clear drawing on finish
	/// </summary>
	public bool ClearOnFinish { get; set; }

	void OnDrawingLineCompleted(MauiDrawingLine lastDrawingLine)
	{
		weakEventManager.HandleEvent(this, new MauiDrawingLineCompletedEventArgs(lastDrawingLine), nameof(DrawingLineCompleted));
	}
}