using System.Runtime.CompilerServices;
using CoreGraphics;
using UIKit;

namespace CommunityToolkit.Maui.Views;

/// <summary>
/// Extension methods to support <see cref="DrawingView"/>
/// </summary>
public static class Extensions
{
	/// <summary>
	/// Move path to point
	/// </summary>
	/// <param name="path">UI Bezier Path</param>
	/// <param name="x">Target Point X</param>
	/// <param name="y">Target Point Y</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void MoveTo(this UIBezierPath path, double x, double y) => path.MoveTo(new CGPoint(x, y));

	/// <summary>
	/// Add Line to path
	/// </summary>
	/// <param name="path">UI Bezier path</param>
	/// <param name="x">Target point X</param>
	/// <param name="y">Target point Y</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void LineTo(this UIBezierPath path, double x, double y) => path.AddLineTo(new CGPoint(x, y));
}