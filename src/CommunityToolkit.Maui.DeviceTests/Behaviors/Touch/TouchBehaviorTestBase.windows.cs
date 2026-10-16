using System.Reflection;
using CommunityToolkit.Maui.Behaviors;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using SolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using Stretch = Microsoft.UI.Xaml.Media.Stretch;

namespace CommunityToolkit.Maui.DeviceTests;

// The native readers follow what MAUI 10.0.90's Windows handlers write (TransformationExtensions.UpdateTransformation): without a x/y rotation the RenderTransform is a
// CompositeTransform (rotation, scale, translation); with one it is a ScaleTransform and a PlaneProjection (rotations, translation).
public abstract partial class TouchBehaviorTestBase
{
	public const string PointerDownAnimationKey = "PointerDownAnimation";

	public const string PointerUpAnimationKey = "PointerUpAnimation";

	protected static bool SupportsNativePointerInput => false;

	protected static string NativePointerInputUnavailableReason =>
		"WinUI's PointerRoutedEventArgs has no public constructor and real pointer input cannot be injected into the test window; the pointer handlers that do not read the event arguments are covered through reflection.";

	protected static FrameworkElement GetPlatformView(IPlatformViewHandler handler) => (FrameworkElement)handler.PlatformView!;

	// ---- lifecycle accessors (used by the shared lifecycle tests)

	/// <summary>The behavior named the element and added the pointer-down and pointer-up storyboards to its resources.</summary>
	protected static void AssertNativeHooksAttached(FrameworkElement view)
	{
		Assert.False(string.IsNullOrEmpty(view.Name), "The behavior must give the element a name (the storyboards target it).");

		var down = Assert.IsType<Storyboard>(view.Resources[PointerDownAnimationKey]);
		var downAnimation = Assert.IsType<PointerDownThemeAnimation>(Assert.Single(down.Children));
		Assert.Equal(view.Name, Storyboard.GetTargetName(downAnimation));

		var up = Assert.IsType<Storyboard>(view.Resources[PointerUpAnimationKey]);
		var upAnimation = Assert.IsType<PointerUpThemeAnimation>(Assert.Single(up.Children));
		Assert.Equal(view.Name, Storyboard.GetTargetName(upAnimation));
	}

	/// <summary>Windows: the behavior changes nothing about how the element takes input (it only names it and adds storyboards).</summary>
	protected static string GetNativeInteractionState(IPlatformViewHandler handler) =>
		$"IsHitTestVisible={GetPlatformView(handler).IsHitTestVisible}";

	/// <summary>No-op on Windows: managed objects with a native peer are released by the normal collections.</summary>
	protected static Task ReleaseNativeReferencesAsync() => Task.CompletedTask;

	/// <summary>No-op on Windows: MAUI's Windows ticker animates whatever the system's animation setting (not read by MAUI 10).</summary>
	protected Task EnableAnimationsAsync(IPlatformViewHandler handler) => Task.CompletedTask;

	ValueTask PlatformDisposeAsync() => ValueTask.CompletedTask;

	/// <summary>A test-built platform view (a Grid inside a Grid) for <see cref="DirectTouchBehavior"/>.</summary>
	protected static (FrameworkElement Parent, FrameworkElement Child) CreateDetachedPlatformViews()
	{
		var parent = new Microsoft.UI.Xaml.Controls.Grid();
		var child = new Microsoft.UI.Xaml.Controls.Grid { Width = HostSize, Height = HostSize };
		parent.Children.Add(child);
		return (parent, child);
	}

	/// <summary>How the platform's image element stretches its image, as a MAUI <see cref="Aspect"/>.</summary>
	protected static Aspect GetNativeImageAspect(IPlatformViewHandler handler) =>
		((Microsoft.UI.Xaml.Controls.Image)handler.PlatformView!).Stretch switch
		{
			Stretch.Uniform => Aspect.AspectFit,
			Stretch.UniformToFill => Aspect.AspectFill,
			Stretch.Fill => Aspect.Fill,
			Stretch.None => Aspect.Center,
			var other => throw new InvalidOperationException($"Unexpected stretch {other}.")
		};

	// ---- native state

	/// <summary>True when MAUI made the element's platform view ignore pointer input (<c>InputTransparent</c> maps to hit testing).</summary>
	protected static bool IsNativeInputTransparent(VisualElement element) =>
		!((FrameworkElement)((IPlatformViewHandler)element.Handler!).PlatformView!).IsHitTestVisible;

	protected static double GetNativeOpacity(IPlatformViewHandler handler) => GetPlatformView(handler).Opacity;

	protected static double GetNativeScale(IPlatformViewHandler handler) =>
		GetPlatformView(handler).RenderTransform switch
		{
			CompositeTransform composite => composite.ScaleX,
			ScaleTransform scale => scale.ScaleX,
			_ => 1
		};

	protected static double GetNativeTranslationX(IPlatformViewHandler handler)
	{
		var view = GetPlatformView(handler);
		return view.RenderTransform is CompositeTransform composite ? composite.TranslateX : view.Projection is PlaneProjection projection ? projection.GlobalOffsetX : 0;
	}

	protected static double GetNativeTranslationY(IPlatformViewHandler handler)
	{
		var view = GetPlatformView(handler);
		return view.RenderTransform is CompositeTransform composite ? composite.TranslateY : view.Projection is PlaneProjection projection ? projection.GlobalOffsetY : 0;
	}

	protected static double GetNativeRotation(IPlatformViewHandler handler)
	{
		var view = GetPlatformView(handler);
		return view.RenderTransform is CompositeTransform composite ? composite.Rotation : view.Projection is PlaneProjection projection ? -projection.RotationZ : 0;
	}

	protected static double GetNativeRotationX(IPlatformViewHandler handler) =>
		GetPlatformView(handler).Projection is PlaneProjection projection ? -projection.RotationX : 0;

	protected static double GetNativeRotationY(IPlatformViewHandler handler) =>
		GetPlatformView(handler).Projection is PlaneProjection projection ? -projection.RotationY : 0;

	/// <summary>The colour the platform view paints as its background; <see langword="null"/> when it has none.</summary>
	protected static Color? GetNativeBackgroundColor(IPlatformViewHandler handler) =>
		GetPlatformView(handler) is Panel { Background: SolidColorBrush brush }
			? Color.FromRgba(brush.Color.R, brush.Color.G, brush.Color.B, brush.Color.A)
			: null;

	/// <summary>
	/// The colour the platform view shows at its centre, with its alpha: what the view looks like there, also when it paints its background
	/// in another way than a Panel's SolidColorBrush (a Button's).
	/// </summary>
	protected static async Task<Color> GetNativeCenterColorAsync(IPlatformViewHandler handler)
	{
		using var bitmap = await GetPlatformView(handler).ToBitmap(handler.MauiContext!);
		var color = bitmap.ColorAtPoint((int)(bitmap.SizeInPixels.Width / 2), (int)(bitmap.SizeInPixels.Height / 2), includeAlpha: true);

		return Color.FromRgba(color.R, color.G, color.B, color.A);
	}

	// ---- native input
	//
	// WinUI cannot construct PointerRoutedEventArgs and a headless/CI desktop cannot be relied on to deliver injected pointer input, so
	// there is no way to feed real pointer events to the platform view. Tests that need a pointer sequence call
	// RequireNativePointerInput first and are skipped visibly.

	protected void NativePress(IPlatformViewHandler handler, double x = HostCenter, double y = HostCenter) => RequireNativePointerInput();

	protected void NativeMove(IPlatformViewHandler handler, double x, double y) => RequireNativePointerInput();

	protected void NativeRelease(IPlatformViewHandler handler, double x = HostCenter, double y = HostCenter) => RequireNativePointerInput();

	protected Task NativeTapAsync(IPlatformViewHandler handler)
	{
		RequireNativePointerInput();
		return Task.CompletedTask;
	}

	protected void NativeCancel(IPlatformViewHandler handler) => RequireNativePointerInput();

	protected Task NativeHoverEnterAsync(IPlatformViewHandler handler)
	{
		RequireNativePointerInput();
		return Task.CompletedTask;
	}

	protected Task NativeHoverExitAsync(IPlatformViewHandler handler)
	{
		RequireNativePointerInput();
		return Task.CompletedTask;
	}

	/// <summary>
	/// Invokes one of the behavior's private pointer handlers (declared <c>(object? sender, PointerRoutedEventArgs e)</c>) with
	/// <see langword="null"/> event arguments. Only the handlers that never read <c>e</c> can be driven this way:
	/// <c>OnPointerEntered</c>, <c>OnPointerExited</c>, <c>OnPointerCanceled</c> and <c>OnPointerCaptureLost</c>.
	/// </summary>
	protected static void InvokePointerHandler(TouchBehavior behavior, string handlerName, FrameworkElement sender)
	{
		var method = typeof(TouchBehavior).GetMethod(handlerName, BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new MissingMethodException(nameof(TouchBehavior), handlerName);

		method.Invoke(behavior, [sender, null]);
	}

	protected static T GetPrivateField<T>(TouchBehavior behavior, string fieldName) =>
		(T)(GetField(fieldName).GetValue(behavior) ?? throw new InvalidOperationException($"{fieldName} is null."));

	protected static void SetPrivateField(TouchBehavior behavior, string fieldName, object value) =>
		GetField(fieldName).SetValue(behavior, value);

	static FieldInfo GetField(string fieldName) =>
		typeof(TouchBehavior).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new MissingFieldException(nameof(TouchBehavior), fieldName);

	/// <summary>Disposes an attached <see cref="DirectTouchBehavior"/> the way an app does (<c>Dispose()</c> without a detach first).</summary>
	protected static void DisposeWhileAttached(DirectTouchBehavior behavior) => behavior.DisposeDirect(true);

	/// <summary>
	/// A TouchBehavior that can be attached to a platform view that MAUI knows nothing about: the native element is built by the
	/// test, so the test controls its resources and name.
	/// </summary>
	protected sealed class DirectTouchBehavior : TouchBehavior
	{
		public void AttachDirect(VisualElement bindable, FrameworkElement platformView) => OnAttachedTo(bindable, platformView);

		public void DetachDirect(VisualElement bindable, FrameworkElement platformView) => OnDetachedFrom(bindable, platformView);

		public void DisposeDirect(bool disposing) => Dispose(disposing);

		/// <summary>Sets the behavior's view without running the effects that <c>Element</c> triggers.</summary>
		public void SetViewOnly(Microsoft.Maui.Controls.VisualElement? view) => View = view;
	}
}