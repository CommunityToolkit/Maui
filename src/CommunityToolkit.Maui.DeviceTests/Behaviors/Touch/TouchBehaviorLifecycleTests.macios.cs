using System.Reflection;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using CoreGraphics;
using Foundation;
using ObjCRuntime;
using UIKit;
using Xunit.Sdk;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class TouchBehaviorLifecycleTests
{
	// MAUI turns user interaction off for an input transparent control such as a label, but never for a layout (its layouts implement
	// InputTransparent in their hit testing and keep UserInteractionEnabled on), so the label is what shows the attach turning it on.
	[Fact]
	public async Task AttachAddsTheHoverRecognizerAndEnablesUserInteraction()
	{
		SetupBuilder();

		var behavior = new TouchBehavior();
		var label = new Label { Text = "label", InputTransparent = true, WidthRequest = HostSize, HeightRequest = HostSize };

		await RunLoadedAsync(label, async handler =>
		{
			var view = GetPlatformView(handler);
			Assert.False(view.UserInteractionEnabled, "Precondition: MAUI turns user interaction off for an input transparent label.");

			label.Behaviors.Add(behavior);
			await WaitUntilAsync(() => behavior.Element is not null, "The behavior did not attach.");

			Assert.True(OperatingSystem.IsIOSVersionAtLeast(13), "The hover recognizer needs iOS 13; the apps' minimum is iOS 15.");
			Assert.NotNull(GetHoverRecognizer(view));
			Assert.True(view.UserInteractionEnabled, "The behavior must enable user interaction on a view MAUI made input transparent.");
		});
	}

	[Fact]
	public async Task DetachLeavesUserInteractionAsMauiSetsItFromInputTransparent()
	{
		SetupBuilder();

		var behavior = new TouchBehavior();
		var label = new Label { Text = "label", WidthRequest = HostSize, HeightRequest = HostSize };

		await RunLoadedAsync(label, async handler =>
		{
			var view = GetPlatformView(handler);
			Assert.True(view.UserInteractionEnabled, "Precondition: MAUI turns user interaction on for a label that takes input.");

			label.Behaviors.Add(behavior);
			await WaitUntilAsync(() => behavior.Element is not null, "The behavior did not attach.");

			label.InputTransparent = true;
			label.Behaviors.Remove(behavior);

			Assert.False(view.UserInteractionEnabled, "A label that became input transparent must not take touches once the behavior is removed.");
		});
	}

	// MAUI also turns user interaction off for a disabled view that is not a UIControl (IsEnabled sets UserInteractionEnabled as well as
	// InputTransparent does), so a detach that applied only MAUI's InputTransparent mapping would turn it on for the disabled label.
	[Fact]
	public async Task DetachLeavesADisabledLabelWithoutUserInteraction()
	{
		SetupBuilder();

		var behavior = new TouchBehavior();
		var label = new Label { Text = "label", WidthRequest = HostSize, HeightRequest = HostSize };

		await RunLoadedAsync(label, async handler =>
		{
			var view = GetPlatformView(handler);
			label.IsEnabled = false;
			Assert.False(view.UserInteractionEnabled, "Precondition: MAUI turns user interaction off for a label disabled at run time.");

			label.Behaviors.Add(behavior);
			await WaitUntilAsync(() => behavior.Element is not null, "The behavior did not attach.");
			Assert.True(view.UserInteractionEnabled, "The behavior enables user interaction while it is attached.");

			label.Behaviors.Remove(behavior);

			Assert.False(view.UserInteractionEnabled, "A disabled label must not take touches once the behavior is removed.");
		});
	}

	// A detach that handed user interaction back to MAUI each time would turn it off under the behavior that is still attached.
	[Fact]
	public async Task RemovingOneOfTwoBehaviorsKeepsUserInteractionForTheOther()
	{
		SetupBuilder();

		var first = new TouchBehavior();
		var second = new TouchBehavior();
		var label = new Label { Text = "label", InputTransparent = true, WidthRequest = HostSize, HeightRequest = HostSize };

		await RunLoadedAsync(label, async handler =>
		{
			var view = GetPlatformView(handler);
			Assert.False(view.UserInteractionEnabled, "Precondition: MAUI turns user interaction off for an input transparent label.");

			label.Behaviors.Add(first);
			label.Behaviors.Add(second);
			await WaitUntilAsync(() => first.Element is not null && second.Element is not null, "The behaviors did not attach.");

			label.Behaviors.Remove(first);
			Assert.True(view.UserInteractionEnabled, "The label must keep taking touches for the behavior that is still attached.");

			label.Behaviors.Remove(second);
			Assert.False(view.UserInteractionEnabled, "The input transparent label must not take touches once both behaviors are removed.");
		});
	}

	// The native objects of disposed recognizers outlive their managed peers, and UIKit keeps calling the touch recognizer (a managed
	// subclass) for every touch on the view.
	[Fact]
	public async Task DisposeRemovesTheRecognizersFromTheView()
	{
		SetupBuilder();

		var behavior = new DirectTouchBehavior();
		var element = new Grid();

		await InvokeOnMainThreadAsync(() =>
		{
			using var host = new UIView(new CGRect(0, 0, 100, 100));
			behavior.AttachDirect(element, host);
			Assert.Equal(2, host.GestureRecognizers?.Length);

			behavior.DisposeDirect(true);
			behavior.DetachDirect(element, host);

			int remaining;
			try
			{
				remaining = host.GestureRecognizers?.Length ?? 0;
			}
			catch (Exception ex)
			{
				throw new XunitException($"The view still holds recognizers whose managed peers were disposed: {ex.Message}", ex);
			}

			Assert.True(remaining == 0, $"The behavior must not leave its recognizers on the view after it was disposed ({remaining} left).");
		});
	}

	// The touch recognizer and its delegate are managed subclasses, whose native handle stays valid after Dispose, so their disposed flag is
	// read; the hover recognizer is a plain UIKit object, whose handle is cleared.
	[Fact]
	public async Task DisposeReleasesTheRecognizersOfTheBehavior()
	{
		SetupBuilder();

		var behavior = new DirectTouchBehavior();
		var element = new Grid();

		await InvokeOnMainThreadAsync(() =>
		{
			var (_, host) = CreateDetachedPlatformViews();
			behavior.AttachDirect(element, host);
			var tap = GetTapRecognizer(host);
			var hover = GetHoverRecognizer(host);
			var tapDelegate = (NSObject)(tap.Delegate ?? throw new InvalidOperationException("The touch recognizer has no delegate."));
			Assert.False(IsDisposed(tap));
			Assert.False(IsDisposed(tapDelegate));

			behavior.DisposeDirect(true);

			Assert.Null(GetRecognizerField(behavior, "touchGesture"));
			Assert.Null(GetRecognizerField(behavior, "hoverGesture"));
			Assert.True(IsDisposed(tap), "Dispose must dispose the touch recognizer.");
			Assert.True(IsDisposed(tapDelegate), "Disposing the touch recognizer must dispose its delegate.");
			Assert.True(IsDisposed(hover), "Dispose must dispose the hover recognizer.");
			Assert.Equal(NativeHandle.Zero, hover.Handle);
		});
	}

	// A finalizer cannot be triggered on purpose, so the protected Dispose(false) is called directly, on a recognizer of a test-built view
	// that no touch can reach. A finalizer that disposed the delegate would dispose one UIKit may still call.
	[Fact]
	public async Task RecognizerFinalizerPathKeepsItsDelegate()
	{
		SetupBuilder();

		var behavior = new DirectTouchBehavior();
		var element = new Grid();

		await InvokeOnMainThreadAsync(() =>
		{
			var (_, host) = CreateDetachedPlatformViews();
			behavior.AttachDirect(element, host);
			var tap = GetTapRecognizer(host);
			var tapDelegate = (NSObject)(tap.Delegate ?? throw new InvalidOperationException("The touch recognizer has no delegate."));
			var dispose = tap.GetType().GetMethod("Dispose", BindingFlags.Instance | BindingFlags.NonPublic, [typeof(bool)])
				?? throw new MissingMethodException(tap.GetType().Name, "Dispose(bool)");

			dispose.Invoke(tap, [false]);

			Assert.True(IsDisposed(tap));
			Assert.False(IsDisposed(tapDelegate), "Dispose(false) must not dispose the delegate.");

			behavior.DetachDirect(element, host);
			Assert.Equal(0, CountTapRecognizers(host));
		});
	}

	[Fact]
	public async Task DetachingAfterDisposeDoesNotThrow()
	{
		SetupBuilder();

		var behavior = new DirectTouchBehavior();
		var element = new Grid();

		await InvokeOnMainThreadAsync(() =>
		{
			var (_, host) = CreateDetachedPlatformViews();
			behavior.AttachDirect(element, host);
			behavior.DisposeDirect(true);

			var exception = Record.Exception(() => behavior.DetachDirect(element, host));

			Assert.Null(exception);
			Assert.Null(behavior.Element);
		});
	}

	[Fact]
	public async Task FinalizerPathKeepsTheRecognizers()
	{
		SetupBuilder();

		var behavior = new DirectTouchBehavior();
		var element = new Grid();

		await InvokeOnMainThreadAsync(() =>
		{
			using var host = new UIView(new CGRect(0, 0, 100, 100));
			behavior.AttachDirect(element, host);
			var recognizer = GetTapRecognizer(host);

			behavior.DisposeDirect(false);

			Assert.False(IsDisposed(recognizer), "Dispose(false) must not dispose the recognizer.");
			Assert.NotNull(GetRecognizerField(behavior, "touchGesture"));
			Assert.NotNull(GetRecognizerField(behavior, "hoverGesture"));
			Assert.Equal(2, host.GestureRecognizers?.Length);

			behavior.DetachDirect(element, host);
			behavior.DisposeDirect(true);
		});
	}

	// A hover action UIKit had queued arrives late, after the detach dropped the hover recognizer that OnHover dereferences.
	[Fact]
	public async Task HoverCallbackAfterDetachIsIgnored()
	{
		SetupBuilder();

		var behavior = new TouchBehavior();
		var host = CreateHost();

		await RunAttachedAsync(host, behavior, _ =>
		{
			host.Behaviors.Remove(behavior);
			Assert.Null(GetRecognizerField(behavior, "hoverGesture"));
			Assert.True(behavior.IsEnabled);

			var onHover = typeof(TouchBehavior).GetMethod("OnHover", BindingFlags.Instance | BindingFlags.NonPublic)
				?? throw new MissingMethodException(nameof(TouchBehavior), "OnHover");
			var exception = Record.Exception(() => onHover.Invoke(behavior, null));

			Assert.Null(exception);
			Assert.Equal(HoverStatus.Exited, behavior.CurrentHoverStatus);
			Assert.Equal(HoverState.Default, behavior.CurrentHoverState);
		});
	}

	static UIGestureRecognizer? GetRecognizerField(TouchBehavior behavior, string name) =>
		(UIGestureRecognizer?)(typeof(TouchBehavior).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new MissingFieldException(nameof(TouchBehavior), name)).GetValue(behavior);

	// NSObject keeps whether Dispose ran in a private property (a flag); for managed subclasses Dispose leaves the native handle in place.
	static bool IsDisposed(NSObject nsObject) =>
		(bool)((typeof(NSObject).GetProperty("disposed", BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new MissingMemberException(nameof(NSObject), "disposed")).GetValue(nsObject)
			?? throw new InvalidOperationException("NSObject.disposed has no value."));
}