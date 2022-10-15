using System.ComponentModel;
using CommunityToolkit.Maui.Core.Handlers;
using Microsoft.Maui.Platform;
using ObjCRuntime;
using UIKit;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// TBD
/// </summary>
public class MauiSemanticOrderView : UIView, IUIAccessibilityContainer
{
	internal Func<double, double, Microsoft.Maui.Graphics.Size>? CrossPlatformMeasure { get; set; }
	internal Func<Microsoft.Maui.Graphics.Rect, Microsoft.Maui.Graphics.Size>? CrossPlatformArrange { get; set; }
	internal ISemanticOrderView VirtualView { get; init; } = null!;

	// TODO somehow measure all the things
}
