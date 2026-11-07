using System.Reflection;
using CommunityToolkit.Maui.Behaviors;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using SolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using Stretch = Microsoft.UI.Xaml.Media.Stretch;

namespace CommunityToolkit.Maui.DeviceTests;

// The native readers follow what MAUI 10.0.90's Windows handlers write (TransformationExtensions.UpdateTransformation): without an x/y
// rotation the RenderTransform is a CompositeTransform (rotation, scale, translation); with one it is a ScaleTransform and a
// PlaneProjection (rotations, translation).
public abstract partial class TouchBehaviorTestBase
{
	public const string PointerDownAnimationKey = "PointerDownAnimation";

	public const string PointerUpAnimationKey = "PointerUpAnimation";

	protected static bool SupportsNativePointerInput => false;

	protected static string NativePointerInputUnavailableReason =>
		"WinUI's PointerRoutedEventArgs has no public constructor and real pointer input cannot be injected into the test window; the pointer handlers that do not read the event arguments are covered through reflection.";

	protected static FrameworkElement GetPlatformView(IPlatformViewHandler handler) => (FrameworkElement)handler.PlatformView!;

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

	// The behavior changes nothing about how an element takes input on Windows (it only names the element and adds storyboards).
	protected static string GetNativeInteractionState(IPlatformViewHandler handler) =>
		$"IsHitTestVisible={GetPlatformView(handler).IsHitTestVisible}";

	// Managed objects with a native peer are released by the normal collections.
	protected static Task ReleaseNativeReferencesAsync() => Task.CompletedTask;

	// MAUI's Windows ticker animates whatever the system's animation setting (MAUI 10 does not read it).
	protected Task EnableAnimationsAsync(IPlatformViewHandler handler) => Task.CompletedTask;

	ValueTask PlatformDisposeAsync() => ValueTask.CompletedTask;

	protected static (FrameworkElement Parent, FrameworkElement Child) CreateDetachedPlatformViews()
	{
		var parent = new Microsoft.UI.Xaml.Controls.Grid();
		var child = new Microsoft.UI.Xaml.Controls.Grid { Width = HostSize, Height = HostSize };
		parent.Children.Add(child);
		return (parent, child);
	}

	protected static Aspect GetNativeImageAspect(IPlatformViewHandler handler) =>
		((Microsoft.UI.Xaml.Controls.Image)handler.PlatformView!).Stretch switch
		{
			Stretch.Uniform => Aspect.AspectFit,
			Stretch.UniformToFill => Aspect.AspectFill,
			Stretch.Fill => Aspect.Fill,
			Stretch.None => Aspect.Center,
			var other => throw new InvalidOperationException($"Unexpected stretch {other}.")
		};

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

	protected static Color? GetNativeBackgroundColor(IPlatformViewHandler handler) =>
		GetPlatformView(handler) is Panel { Background: SolidColorBrush brush }
			? Color.FromRgba(brush.Color.R, brush.Color.G, brush.Color.B, brush.Color.A)
			: null;

	// Unlike GetNativeBackgroundColor, this also works when the view paints its background in another way than a Panel's
	// SolidColorBrush (a Button's).
	protected static async Task<Color> GetNativeCenterColorAsync(IPlatformViewHandler handler)
	{
		using var bitmap = await GetPlatformView(handler).ToBitmap(handler.MauiContext!);
		var color = bitmap.ColorAtPoint((int)(bitmap.SizeInPixels.Width / 2), (int)(bitmap.SizeInPixels.Height / 2), includeAlpha: true);

		return Color.FromRgba(color.R, color.G, color.B, color.A);
	}

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
	/// The handlers are invoked with <see langword="null"/> event arguments, so each runs only up to where it reads them:
	/// <c>OnPointerEntered</c>, <c>OnPointerExited</c>, <c>OnPointerCanceled</c> and <c>OnPointerCaptureLost</c> never do.
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

	protected static void DisposeWhileAttached(DirectTouchBehavior behavior) => behavior.DisposeDirect(true);

	// The native element is built by the test, so the test controls its resources and name.
	protected sealed class DirectTouchBehavior : TouchBehavior
	{
		public void AttachDirect(VisualElement bindable, FrameworkElement platformView) => OnAttachedTo(bindable, platformView);

		public void DetachDirect(VisualElement bindable, FrameworkElement platformView) => OnDetachedFrom(bindable, platformView);

		public void DisposeDirect(bool disposing) => Dispose(disposing);

		/// <summary>Sets the behavior's view without running the effects that <c>Element</c> triggers.</summary>
		public void SetViewOnly(Microsoft.Maui.Controls.VisualElement? view) => View = view;
	}
}