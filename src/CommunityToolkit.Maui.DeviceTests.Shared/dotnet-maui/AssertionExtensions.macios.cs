// Ported from dotnet/maui @ 2067dd42c6032fbc40d2e46245806679a85df5e6
// Original path: src/TestUtils/src/DeviceTests/AssertionExtensions.iOS.cs
// Copyright (c) .NET Foundation and Contributors. Licensed under the MIT License (see LICENSE-dotnet-maui.txt).
// Toolkit change: namespace changed from Microsoft.Maui.DeviceTests to CommunityToolkit.Maui.DeviceTests; file renamed to the toolkit's .macios.cs suffix.
// Toolkit change: subset that uses public MAUI API only: focus helpers, AttachAndRun, bitmap and color assertions. The keyboard,
//                 accessibility, navigation-bar and tab-bar helpers (internal MAUI API) were dropped and the internal
//                 IMauiContext.CreateLogger calls removed. Added IsLoadedOnPlatform.
// Toolkit change: when the attached view belongs to a view controller that has no parent (e.g. a Page's PageViewController),
//                 AttachAndRun attaches that controller's view as a child view controller of the window's root view controller,
//                 so UIKit view-controller containment stays consistent.
// Toolkit change: ToBitmap throws when there is no current graphics context or image: UIGraphics.GetCurrentContext and
//                 GetImageFromCurrentImageContext return nullable types in the iOS and Mac Catalyst bindings of .NET 11 and of
//                 26.4 or later on .NET 10 (the 26.0 bindings that net10.0-ios and net10.0-maccatalyst compile against return
//                 non-null types), so the upstream code fails CS8604 and CS8619 (nullable warnings are errors).
#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using CoreGraphics;
using Foundation;
using Microsoft.Maui;
using Microsoft.Maui.Platform;
using UIKit;
using Xunit;
using Xunit.Sdk;
using static CommunityToolkit.Maui.DeviceTests.AssertHelpers;

namespace CommunityToolkit.Maui.DeviceTests
{
	public static partial class AssertionExtensions
	{
		public static bool IsLoadedOnPlatform(this UIView view) => view.Window is not null;

		public static async Task WaitForFocused(this UIView view, int timeout = 1000)
		{
			await AssertEventually(view.IsFocused, timeout: timeout, message: $"Timed out waiting for {view} to become focused").ConfigureAwait(false);
		}

		public static async Task WaitForUnFocused(this UIView view, int timeout = 1000)
		{
			await AssertEventually(() => !view.IsFocused(), timeout: timeout, message: $"Timed out waiting for {view} to become unfocused").ConfigureAwait(false);
		}

		static bool IsFocused(this UIView view) => view.Focused || view.IsFirstResponder;

		public static Task FocusView(this UIView view, int timeout = 1000)
		{
			view.Focus(new FocusRequest());
			return WaitForFocused(view, timeout);
		}

		public static string CreateColorAtPointError(this UIImage bitmap, UIColor expectedColor, int x, int y) =>
			CreateColorError(bitmap, $"Expected {expectedColor} at point {x},{y} in renderered view.");

		public static string CreateColorError(this UIImage bitmap, string message) =>
			$"{message} This is what it looked like:<img>{bitmap.ToBase64String()}</img>";

		public static string CreateEqualError(this UIImage bitmap, UIImage other, string message) =>
			$"{message} This is what it looked like: <img>{bitmap.ToBase64String()}</img> and <img>{other.ToBase64String()}</img>";

		public static string CreateScreenshotError(this UIImage bitmap, string message) =>
			$"{message} This is what it looked like:<img>{bitmap.ToBase64String()}</img>";

		public static string ToBase64String(this UIImage bitmap)
		{
			var data = bitmap.AsPNG();

			ArgumentNullException.ThrowIfNull(data);

			return data.GetBase64EncodedString(NSDataBase64EncodingOptions.None);
		}

		public static Task AttachAndRun(this UIView view, Action action) =>
			view.AttachAndRun(() =>
			{
				action();
				return Task.FromResult(true);
			});

		public static Task<T> AttachAndRun<T>(this UIView view, Func<T> action) =>
			view.AttachAndRun(() =>
			{
				var result = action();
				return Task.FromResult(result);
			});

		public static Task AttachAndRun(this UIView view, Func<Task> action) =>
			view.AttachAndRun(async () =>
			{
				await action();
				return true;
			});

		private static int _zindex = 1000;

		private class TestWrapperView : UIView
		{
			private readonly TaskCompletionSource _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

			public Task WaitForLayoutPassAsync() => _tcs.Task;

			public override void LayoutSubviews()
			{
				base.LayoutSubviews();
				_tcs.TrySetResult();
			}
		}

		public static async Task<T> AttachAndRun<T>(this UIView view, Func<Task<T>> action)
		{
			var currentView = await FindContentView();

			// MauiView has optimization code that won't fire a remeasure of the child view
			// Check LayoutSubviews inside MauiView.cs for more details.
			// If the parent is a MauiView, the expectation is that the parent will call
			// measure on all the children. But this view that we're "attaching" is unknown to MauiView
			// so the optimization code causes the attached view to not remeasure when it actually should.
			// So we add a UIView in the middle to force our attached view to not optimize itself and actually
			// remeasure when requested
			// This middle view is also helpful so we can make sure the attached view isn't inside the safe area
			// which can have some unexpected results
			var safeAreaInsets = currentView.SafeAreaInsets;
			var attachedView = new TestWrapperView
			{
				Frame = new CGRect(
					safeAreaInsets.Right,
					safeAreaInsets.Top,
					currentView.Frame.Width - safeAreaInsets.Right,
					currentView.Frame.Height - safeAreaInsets.Top),
			};

			attachedView.Layer.ZPosition = Interlocked.Increment(ref _zindex);
			currentView.AddSubview(attachedView);

			// A Page's platform view lives in the view of a view controller (PageViewController). Attaching only the platform view
			// breaks UIKit's view-controller containment (UIViewControllerHierarchyInconsistency once a child controller, e.g.
			// MediaElement's AVPlayerViewController, moves to the window), so attach the controller as a child controller instead.
			var hostViewController = currentView.NextResponder as UIViewController;
			var ownerViewController = FindOwnerViewController(view);
			var containOwner = hostViewController is not null
				&& ownerViewController?.View is not null
				&& ownerViewController != hostViewController
				&& ownerViewController.ParentViewController is null;
			var viewToAttach = containOwner ? ownerViewController!.View! : view;

			if (containOwner)
			{
				hostViewController!.AddChildViewController(ownerViewController!);
				viewToAttach.Frame = attachedView.Bounds;
			}

			attachedView.AddSubview(viewToAttach);

			if (containOwner)
				ownerViewController!.DidMoveToParentViewController(hostViewController);

			// Layout pass happens only after Window has been set on the view
			await attachedView.WaitForLayoutPassAsync();

			// We're not sure that the view has been successfully rendered, but give it time to settle
			await Task.Delay(100);

			T result;

			try
			{
				result = await action();
			}
			finally
			{
				if (containOwner)
					ownerViewController!.WillMoveToParentViewController(null);

				viewToAttach.RemoveFromSuperview();
				attachedView.RemoveFromSuperview();

				if (containOwner)
					ownerViewController!.RemoveFromParentViewController();

				// Give the UI time to refresh
				await Task.Delay(100);
			}

			return result;
		}

		static UIViewController? FindOwnerViewController(UIView view)
		{
			for (var responder = view.NextResponder; responder is not null; responder = responder.NextResponder)
			{
				if (responder is UIViewController viewController)
					return viewController;
			}

			return null;
		}

		private static UIView? _contentView;
		private static readonly object _contentViewLock = new();

		public static Task<UIView> FindContentView() => FindContentView(0);

		public static async Task<UIView> FindContentView(int retryCount)
		{
			if (_contentView is not null)
			{
				return _contentView;
			}

			if (retryCount > 0)
				await Task.Delay(1000);
			else
				await Task.Yield();

			UIWindow? window = null;

			lock (_contentViewLock)
			{
				if (_contentView is not null)
				{
					return _contentView;
				}

				window = GetKeyWindow(UIApplication.SharedApplication);

				if (retryCount > 4 || window is not null)
				{
					if (window is null)
					{
						throw new InvalidOperationException($"Could not attach view - unable to find UIWindow.");
					}

					if (window?.RootViewController is not UIViewController viewController)
					{
						throw new InvalidOperationException("Could not attach view - unable to find RootViewController");
					}

					var rootView = viewController.View ?? throw new InvalidOperationException("Could not attach view - root view is null");

					return _contentView = rootView;
				}
			}

			// If we don't have a window yet, wait a bit and try again
			Console.WriteLine($"Retrying to find content view. Retry count: {retryCount}");

			return await FindContentView(retryCount + 1).ConfigureAwait(false);
		}

		public static Task<UIImage> ToBitmap(this UIView view, IMauiContext mauiContext)
		{
			if (view.Superview is WrapperView wrapper)
				view = wrapper;

			var imageRect = new CGRect(0, 0, view.Frame.Width, view.Frame.Height);

			if (view.Frame.Width == 0 && view.Frame.Height == 0)
			{
				UIGraphicsImageRenderer renderer = new UIGraphicsImageRenderer(imageRect.Size);
				return Task.FromResult(renderer.CreateImage(c =>
				{
					view.Layer.RenderInContext(c.CGContext);
				}));
			}

#pragma warning disable CA1416, CA1422 // Validate platform compatibility
			UIGraphics.BeginImageContext(imageRect.Size);
			var context = UIGraphics.GetCurrentContext() ?? throw new InvalidOperationException("Could not create bitmap - unable to find the current graphics context");
			view.Layer.RenderInContext(context);
			var image = UIGraphics.GetImageFromCurrentImageContext();
			UIGraphics.EndImageContext();
#pragma warning restore CA1416, CA1422 // Validate platform compatibility

			return Task.FromResult(image ?? throw new InvalidOperationException("Could not create bitmap - unable to get the image from the current image context"));
		}

		public static UIColor ColorAtPoint(this UIImage bitmap, int x, int y)
		{
			var pixel = bitmap.GetPixel(x, y);

			var color = new UIColor(
				pixel[0] / 255.0f,
				pixel[1] / 255.0f,
				pixel[2] / 255.0f,
				pixel[3] / 255.0f);

			return color;
		}

		public static byte[] GetPixel(this UIImage bitmap, int x, int y)
		{
			var cgImage = bitmap.CGImage!;
			var width = cgImage.Width;
			var height = cgImage.Height;
			var colorSpace = CGColorSpace.CreateDeviceRGB();
			var bitsPerComponent = 8;
			var bytesPerRow = 4 * width;
			var componentCount = 4;

			var dataBytes = new byte[width * height * componentCount];

			using var context = new CGBitmapContext(
				dataBytes,
				width, height,
				bitsPerComponent, bytesPerRow,
				colorSpace,
				CGBitmapFlags.ByteOrder32Big | CGBitmapFlags.PremultipliedLast);

			context.DrawImage(new CGRect(0, 0, width, height), cgImage);

			var pixelLocation = (bytesPerRow * y) + componentCount * x;

			var pixel = new byte[]
			{
				dataBytes[pixelLocation],
				dataBytes[pixelLocation + 1],
				dataBytes[pixelLocation + 2],
				dataBytes[pixelLocation + 3],
			};

			return pixel;
		}

		public static UIImage AssertColorAtPoint(this UIImage bitmap, UIColor expectedColor, int x, int y, double? tolerance = null)
		{
			var cap = bitmap.ColorAtPoint(x, y);

			if (!ColorComparison.ARGBEquivalent(cap, expectedColor, tolerance))
				throw new XunitException(CreateColorAtPointError(bitmap, expectedColor, x, y));

			return bitmap;
		}

		public static UIImage AssertColorAtCenter(this UIImage bitmap, UIColor expectedColor)
		{
			AssertColorAtPoint(bitmap, expectedColor, (int)bitmap.Size.Width / 2, (int)bitmap.Size.Height / 2);
			return bitmap;
		}

		public static UIImage AssertColorAtBottomLeft(this UIImage bitmap, UIColor expectedColor)
		{
			return bitmap.AssertColorAtPoint(expectedColor, 0, 0);
		}

		public static UIImage AssertColorAtBottomRight(this UIImage bitmap, UIColor expectedColor)
		{
			return bitmap.AssertColorAtPoint(expectedColor, (int)bitmap.Size.Width - 1, 0);
		}

		public static UIImage AssertColorAtTopLeft(this UIImage bitmap, UIColor expectedColor)
		{
			return bitmap.AssertColorAtPoint(expectedColor, 0, (int)bitmap.Size.Height - 1);
		}

		public static UIImage AssertColorAtTopRight(this UIImage bitmap, UIColor expectedColor)
		{
			return bitmap.AssertColorAtPoint(expectedColor, (int)bitmap.Size.Width - 1, (int)bitmap.Size.Height - 1);
		}

		public static async Task<UIImage> AssertColorAtPointAsync(this UIView view, UIColor expectedColor, int x, int y, IMauiContext mauiContext)
		{
			var bitmap = await view.ToBitmap(mauiContext).ConfigureAwait(false);
			return bitmap.AssertColorAtPoint(expectedColor, x, y);
		}

		public static async Task<UIImage> AssertColorsAtPointsAsync(this UIView view, Microsoft.Maui.Graphics.Color[] colors, Microsoft.Maui.Graphics.Point[] points, IMauiContext mauiContext)
		{
			var bitmap = await view.ToBitmap(mauiContext).ConfigureAwait(false);

			for (int i = 0; i < points.Length; i++)
			{
				bitmap.AssertColorAtPoint(colors[i].ToPlatform(), (int)points[i].X, (int)points[i].Y);
			}

			return bitmap;
		}

		public static async Task<UIImage> AssertColorAtCenterAsync(this UIView view, UIColor expectedColor, IMauiContext mauiContext)
		{
			var bitmap = await view.ToBitmap(mauiContext).ConfigureAwait(false);
			return bitmap.AssertColorAtCenter(expectedColor);
		}

		public static async Task<UIImage> AssertContainsColor(this UIView view, UIColor expectedColor, IMauiContext mauiContext, double? tolerance = null)
		{
			var bitmap = await view.ToBitmap(mauiContext).ConfigureAwait(false);
			return await bitmap.AssertContainsColor(expectedColor, tolerance: tolerance).ConfigureAwait(false);
		}

		public static async Task<UIImage> AssertDoesNotContainColor(this UIView view, UIColor unexpectedColor, IMauiContext mauiContext)
		{
			var bitmap = await view.ToBitmap(mauiContext).ConfigureAwait(false);
			return await bitmap.AssertDoesNotContainColor(unexpectedColor).ConfigureAwait(false);
		}

		public static Task<UIImage> AssertContainsColor(this UIView view, Microsoft.Maui.Graphics.Color expectedColor, IMauiContext mauiContext, double? tolerance = null) =>
			AssertContainsColor(view, expectedColor.ToPlatform(), mauiContext, tolerance: tolerance);

		public static Task<UIImage> AssertDoesNotContainColor(this UIView view, Microsoft.Maui.Graphics.Color unexpectedColor, IMauiContext mauiContext) =>
			AssertDoesNotContainColor(view, unexpectedColor.ToPlatform(), mauiContext);

		public static Task<UIImage> AssertContainsColor(this UIImage image, Microsoft.Maui.Graphics.Color expectedColor, Func<Microsoft.Maui.Graphics.RectF, Microsoft.Maui.Graphics.RectF>? withinRectModifier = null, double? tolerance = null)
			=> image.AssertContainsColor(expectedColor.ToPlatform(), withinRectModifier, tolerance: tolerance);

		public static Task<UIImage> AssertContainsColor(this UIImage bitmap, UIColor expectedColor, Func<Microsoft.Maui.Graphics.RectF, Microsoft.Maui.Graphics.RectF>? withinRectModifier = null, double? tolerance = null)
		{
			return Task.Run(() =>
			{
				var imageRect = new Microsoft.Maui.Graphics.RectF(0, 0, (float)bitmap.Size.Width.Value, (float)bitmap.Size.Height.Value);

				if (withinRectModifier is not null)
					imageRect = withinRectModifier.Invoke(imageRect);

				for (int x = (int)imageRect.X; x < (int)imageRect.Width; x++)
				{
					for (int y = (int)imageRect.Y; y < (int)imageRect.Height; y++)
					{
						if (ColorComparison.ARGBEquivalent(bitmap.ColorAtPoint(x, y), expectedColor, tolerance))
						{
							return bitmap;
						}
					}
				}

				throw new XunitException(CreateColorError(bitmap, $"Color {expectedColor} not found."));
			});
		}

		public static Task<UIImage> AssertDoesNotContainColor(this UIImage bitmap, Microsoft.Maui.Graphics.Color unexpectedColor, Func<Microsoft.Maui.Graphics.RectF, Microsoft.Maui.Graphics.RectF>? withinRectModifier = null)
			=> bitmap.AssertDoesNotContainColor(unexpectedColor.ToPlatform(), withinRectModifier);

		public static Task<UIImage> AssertDoesNotContainColor(this UIImage bitmap, UIColor unexpectedColor, Func<Microsoft.Maui.Graphics.RectF, Microsoft.Maui.Graphics.RectF>? withinRectModifier = null)
		{
			return Task.Run(() =>
			{
				var imageRect = new Microsoft.Maui.Graphics.RectF(0, 0, (float)bitmap.Size.Width.Value, (float)bitmap.Size.Height.Value);

				if (withinRectModifier is not null)
					imageRect = withinRectModifier.Invoke(imageRect);

				for (int x = (int)imageRect.X; x < (int)imageRect.Width; x++)
				{
					for (int y = (int)imageRect.Y; y < (int)imageRect.Height; y++)
					{
						if (ColorComparison.ARGBEquivalent(bitmap.ColorAtPoint(x, y), unexpectedColor))
						{
							throw new XunitException(CreateColorError(bitmap, $"Color {unexpectedColor} was found at point {x}, {y}."));
						}
					}
				}

				return bitmap;
			});
		}

		public static Task AssertEqualAsync(this UIImage bitmap, UIImage other)
		{
			Assert.NotNull(bitmap);
			Assert.NotNull(other);

			Assert.Equal(bitmap.Size, other.Size);

			Assert.True(IsMatching(), CreateEqualError(bitmap, other, $"Images did not match."));

			return Task.CompletedTask;

			bool IsMatching()
			{
				for (int x = 0; x < bitmap.Size.Width; x++)
				{
					for (int y = 0; y < bitmap.Size.Height; y++)
					{
						var first = bitmap.ColorAtPoint(x, y);
						var second = other.ColorAtPoint(x, y);

						if (!ColorComparison.ARGBEquivalent(first, second))
							return false;
					}
				}
				return true;
			}
		}

		public static async Task ThrowScreenshot(this UIView view, IMauiContext mauiContext, string? message = null, Exception? ex = null)
		{
			var bitmap = await view.ToBitmap(mauiContext).ConfigureAwait(false);
			if (ex is null)
				throw new XunitException(CreateScreenshotError(bitmap, message ?? "There was an error."));
			else
				throw new XunitException(CreateScreenshotError(bitmap, message ?? "There was an error: " + ex.Message), ex);
		}

		static UIWindow? GetKeyWindow(UIApplication application)
		{
			if (OperatingSystem.IsIOSVersionAtLeast(15))
			{
				foreach (var scene in application.ConnectedScenes)
				{
					if (scene is UIWindowScene windowScene
						&& windowScene.ActivationState == UISceneActivationState.ForegroundActive)
					{
						foreach (var window in windowScene.Windows)
						{
#if MACCATALYST
							// When running headless (on CI or local) Mac Catalyst has trouble finding the window through the method below.
							// Added an env variable to accommodate for this and just return the first window found.
							if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("headlessrunner")))
							{
								return window;
							}
#endif
							if (window.IsKeyWindow)
							{
								return window;
							}
						}
					}
				}

				return null;
			}

			var windows = application.Windows;

			for (int i = 0; i < windows.Length; i++)
			{
				var window = windows[i];
				if (window.IsKeyWindow)
					return window;
			}

			return null;
		}
	}
}
