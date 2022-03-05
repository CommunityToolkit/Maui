using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using WSolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using WBrush = Microsoft.UI.Xaml.Media.Brush;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// DrawingView Native Control
/// </summary>
public class DrawingNativeView : FrameworkElement
{
	/// <summary>
	/// Event raised when drawing line completed 
	/// </summary>
	public event EventHandler<DrawingNativeLineCompletedEventArgs>? DrawingLineCompleted;

	/// <summary>
	/// Line color
	/// </summary>
	public WBrush LineColor { get; set; }

	/// <summary>
	/// Line width
	/// </summary>
	public float LineWidth { get; set; }

	/// <summary>
	/// Command executed when drawing line completed
	/// </summary>
	public ICommand? DrawingLineCompletedCommand { get; set; }

	/// <summary>
	/// Drawing Lines
	/// </summary>
	public ObservableCollection<DrawingNativeLine> Lines { get; }

	/// <summary>
	/// Enable or disable multiline mode
	/// </summary>
	public bool MultiLineMode { get; set; }

	/// <summary>
	/// Clear drawing on finish
	/// </summary>
	public bool ClearOnFinish { get; set; }

	/// <summary>
	/// Initialize a new instance of <see cref="DrawingNativeView" />.
	/// </summary>
	public DrawingNativeView()
	{
		Lines = new ObservableCollection<DrawingNativeLine>();
		LineColor = new WSolidColorBrush();
		LineWidth = 5;
	}

	/// <summary>
	/// Initialize resources
	/// </summary>
	public void Initialize()
	{
	}

	/// <summary>
	/// Clean up resources
	/// </summary>
	public void CleanUp()
	{
	}
}