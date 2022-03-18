using System.Windows.Input;
using CommunityToolkit.Maui.Core.Views;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// DrawingView Platform Control
/// </summary>
public partial class MauiDrawingView
{
	readonly WeakEventManager weakeventmanager = new();

	/// <summary>
	/// Event raised when drawing line completed 
	/// </summary>
	public event EventHandler<MauiDrawingLineCompletedEventArgs> DrawingLineCompleted
	{
		add => weakeventmanager.AddEventHandler(value);
		remove => weakeventmanager.RemoveEventHandler(value);
	}

	/// <summary>
	/// Command executed when drawing line completed
	/// </summary>
	public ICommand? DrawingLineCompletedCommand { get; set; }

	void OnDrawingLineCompleted(MauiDrawingLine lastDrawingLine)
	{
		weakeventmanager.HandleEvent(this, new MauiDrawingLineCompletedEventArgs(lastDrawingLine), nameof(DrawingLineCompleted));

		if (DrawingLineCompletedCommand?.CanExecute(lastDrawingLine) ?? false)
		{
			DrawingLineCompletedCommand.Execute(lastDrawingLine);
		}
	}
}