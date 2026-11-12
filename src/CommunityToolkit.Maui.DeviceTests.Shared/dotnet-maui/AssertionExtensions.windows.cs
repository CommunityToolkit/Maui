// Ported from dotnet/maui @ 2067dd42c6032fbc40d2e46245806679a85df5e6
// Original path: src/TestUtils/src/DeviceTests/AssertionExtensions.Windows.cs
// Copyright (c) .NET Foundation and Contributors. Licensed under the MIT License (see LICENSE-dotnet-maui.txt).
// Toolkit change: namespace changed from Microsoft.Maui.DeviceTests to CommunityToolkit.Maui.DeviceTests; file renamed to the toolkit's .windows.cs suffix.
// Toolkit change: subset with focus helpers, AttachAndRun, bitmap and color assertions; the keyboard, accessibility and tab
//                 helpers were dropped. FocusView is implemented (upstream throws NotImplementedException), colors are converted
//                 with Color.ToRgba instead of MAUI's ToWindowsColor, and IsLoadedOnPlatform was added.
#nullable enable
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Graphics.Canvas;
using Microsoft.Maui;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.DirectX;
using Windows.Storage.Streams;
using Xunit;
using Xunit.Sdk;
using WColor = Windows.UI.Color;
using WHorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment;
using WVerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment;
using WWindow = Microsoft.UI.Xaml.Window;

namespace CommunityToolkit.Maui.DeviceTests
{
	public static partial class AssertionExtensions
	{
		public static bool IsLoadedOnPlatform(this FrameworkElement view) => view.IsLoaded;

		public static async Task WaitForFocused(this FrameworkElement view, int timeout = 1000)
		{
			TaskCompletionSource focusSource = new TaskCompletionSource();

			FocusManager.GotFocus += OnFocused;

			try
			{
				await focusSource.Task.WaitAsync(TimeSpan.FromMilliseconds(timeout));
			}
			finally
			{
				FocusManager.GotFocus -= OnFocused;
			}

			void OnFocused(object? sender, FocusManagerGotFocusEventArgs e)
			{
				if (e.NewFocusedElement == view)
				{
					FocusManager.GotFocus -= OnFocused;
					focusSource.TrySetResult();
				}
			}
		}

		public static async Task WaitForUnFocused(this FrameworkElement view, int timeout = 1000)
		{
			TaskCompletionSource focusSource = new TaskCompletionSource();

			FocusManager.LostFocus += OnUnFocused;

			try
			{
				await focusSource.Task.WaitAsync(TimeSpan.FromMilliseconds(timeout));
			}
			finally
			{
				FocusManager.LostFocus -= OnUnFocused;
			}

			void OnUnFocused(object? sender, FocusManagerLostFocusEventArgs e)
			{
				if (e.OldFocusedElement == view)
				{
					FocusManager.LostFocus -= OnUnFocused;
					focusSource.TrySetResult();
				}
			}
		}

		public static Task FocusView(this FrameworkElement view, int timeout = 1000)
		{
			// Subscribe before focusing so the GotFocus event cannot be missed.
			var focused = view.WaitForFocused(timeout);
			view.Focus(FocusState.Programmatic);
			return focused;
		}

		public static Task<string> CreateColorAtPointError(this CanvasBitmap bitmap, WColor expectedColor, int x, int y) =>
			CreateColorError(bitmap, $"Expected {expectedColor} at point {x},{y} in rendered view.");

		public static async Task<string> CreateColorError(this CanvasBitmap bitmap, string message) =>
			$"{message} This is what it looked like:<img>{await bitmap.ToBase64StringAsync()}</img>";

		public static async Task<string> CreateEqualError(this CanvasBitmap bitmap, CanvasBitmap other, string message) =>
			$"{message} This is what it looked like: <img>{await bitmap.ToBase64StringAsync()}</img> and <img>{await other.ToBase64StringAsync()}</img>";

		public static async Task<string> CreateScreenshotError(this CanvasBitmap bitmap, string message) =>
			$"{message} This is what it looked like:<img>{await bitmap.ToBase64StringAsync()}</img>";

		public static async Task<string> ToBase64StringAsync(this CanvasBitmap bitmap)
		{
			using var ms = new InMemoryRandomAccessStream();
			await bitmap.SaveAsync(ms, CanvasBitmapFileFormat.Png);

			using var ms2 = new MemoryStream();
			await ms.AsStreamForRead().CopyToAsync(ms2);

			return Convert.ToBase64String(ms2.ToArray());
		}

		public static WColor ColorAtPoint(this CanvasBitmap bitmap, int x, int y, bool includeAlpha = false)
		{
			var pixel = bitmap.GetPixelColors(x, y, 1, 1)[0];
			return includeAlpha
				? pixel
				: WColor.FromArgb(255, pixel.R, pixel.G, pixel.B);
		}

		public static Task AttachAndRun(this FrameworkElement view, Action action, IMauiContext mauiContext) =>
			view.AttachAndRun(window => action(), mauiContext);

		public static Task AttachAndRun(this FrameworkElement view, Action<WWindow> action, IMauiContext mauiContext) =>
			view.AttachAndRun((window) =>
			{
				action(window);
				return Task.FromResult(true);
			}, mauiContext);

		public static Task<T> AttachAndRun<T>(this FrameworkElement view, Func<T> action, IMauiContext mauiContext) =>
			view.AttachAndRun(window => action(), mauiContext);

		public static Task AttachAndRun(this FrameworkElement view, Func<Task> action, IMauiContext mauiContext) =>
			view.AttachAndRun(async window =>
			{
				await action();
				return true;
			}, mauiContext);

		public static Task<T> AttachAndRun<T>(this FrameworkElement view, Func<WWindow, T> action, IMauiContext mauiContext) =>
			view.AttachAndRun((window) =>
			{
				var result = action(window);
				return Task.FromResult(result);
			}, mauiContext);

		// Windows does ok running these tests in parallel but there's definitely
		// a limit where it'll eventually be too many windows.
		// So, for now we're limiting this to 10 parallel windows which seems
		// to work fine.
		static SemaphoreSlim _attachAndRunSemaphore = new SemaphoreSlim(10);

		public static async Task<T> AttachAndRun<T>(this FrameworkElement view, Func<WWindow, Task<T>> action, IMauiContext mauiContext)
		{
			if (view.Parent is Border wrapper)
				view = wrapper;

			// If the view has a parent, it's already attached to the UI
			if (view.Parent != null)
			{
				// Window is not a XAML type so is never on the hierarchy
				var window = (WWindow)mauiContext!.Services!.GetService(typeof(WWindow))!;

				Assert.True(window?.Content is not null,
					"Content on Window has not been set. Most likely the window under test isn't being registered against the test service being used. Check if you're passing the right MauiContext in");

				if (window!.Content!.XamlRoot != view.XamlRoot)
					throw new Exception("The window retrieved from the service is different than the window this view is attached to");

				return await Run(() => action(window));
			}

			// If the view has no parent, we need to attach it to the UI by creating a new window and using that as the host
			try
			{
				await _attachAndRunSemaphore.WaitAsync();

				// prepare to wait for element to be in the UI
				var loadedTcs = new TaskCompletionSource();
				var unloadedTcs = new TaskCompletionSource();

				view.Loaded += OnViewLoaded;

				// attach to the UI
				Grid viewContainer;
				var window = (WWindow)mauiContext!.Services!.GetService(typeof(WWindow))!;

				if (window.Content is not null)
					throw new Exception("The window retrieved from the service is already attached to existing content");

				window.Content = new Grid
				{
					HorizontalAlignment = WHorizontalAlignment.Center,
					VerticalAlignment = WVerticalAlignment.Center,
					Children =
					{
						(viewContainer = new Grid
						{
							Width = view.Width,
							Height = view.Height,
							Children =
							{
								view
							}
						})
					}
				};

				window.Activate();

				// wait for element to be loaded
				await loadedTcs.Task;
				view.Unloaded += OnViewUnloaded;

				try
				{
					return await Run(() => action(window));
				}
				finally
				{
					// release all views
					window.Content = null;
					viewContainer.Children.Clear();

					// wait for an unload
					await unloadedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
					await Task.Delay(10);

					// close the window
					window.Close();
				}

				void OnViewLoaded(object sender, RoutedEventArgs e)
				{
					view.Loaded -= OnViewLoaded;
					loadedTcs?.SetResult();
				}

				void OnViewUnloaded(object sender, RoutedEventArgs e)
				{
					view.Unloaded -= OnViewUnloaded;
					unloadedTcs?.SetResult();
				}
			}
			finally
			{
				_attachAndRunSemaphore.Release();
			}

			static async Task<T> Run(Func<Task<T>> action)
			{
				return await action();
			}
		}

		public static Task<CanvasBitmap> ToBitmap(this FrameworkElement view, IMauiContext mauiContext) =>
			view.AttachAndRun(async (window) =>
			{
				if (view.Parent is Border wrapper)
					view = wrapper;

				var device = CanvasDevice.GetSharedDevice();

				// Wait for view to have non-zero dimensions (layout may not have completed after Loaded event)
				if (view.ActualWidth == 0 || view.ActualHeight == 0)
				{
					// Force a layout pass
					view.UpdateLayout();

					// If still zero, wait for SizeChanged event
					if (view.ActualWidth == 0 || view.ActualHeight == 0)
					{
						var sizeChangedTcs = new TaskCompletionSource();
						void OnSizeChanged(object sender, SizeChangedEventArgs e)
						{
							view.SizeChanged -= OnSizeChanged;
							sizeChangedTcs.TrySetResult();
						}
						view.SizeChanged += OnSizeChanged;

						// Wait up to 5 seconds for size to be set
						var completed = await Task.WhenAny(sizeChangedTcs.Task, Task.Delay(5000));
						if (completed != sizeChangedTcs.Task)
						{
							view.SizeChanged -= OnSizeChanged;
						}
					}
				}

				// HELP?
				// The simple act of doing a window capture results in the next render method
				// working on DirectX controls (such as Win2D).
				// We could use this window bitmap directly, but that is extra effort to crop
				// to the view bounds... so until this breaks...
				try
				{
					using var windowBitmap = await CaptureHelper.RenderAsync(window, device);
				}
				catch (PlatformNotSupportedException)
				{
					// On Helix CI VMs, window capture is not supported.
					// Continue without it - this may cause issues with DirectX controls like Win2D,
					// but should work for standard XAML controls.
				}

				var bmp = new RenderTargetBitmap();
				await bmp.RenderAsync(view);
				var pixels = await bmp.GetPixelsAsync();
				var width = bmp.PixelWidth;
				var height = bmp.PixelHeight;

				return CanvasBitmap.CreateFromBytes(device, pixels, width, height, DirectXPixelFormat.B8G8R8A8UIntNormalized);
			}, mauiContext);

		public static CanvasBitmap AssertColorAtPoint(this CanvasBitmap bitmap, WColor expectedColor, uint x, uint y) =>
			bitmap.AssertColorAtPoint(expectedColor, (int)x, (int)y);

		public static CanvasBitmap AssertColorAtPoint(this CanvasBitmap bitmap, WColor expectedColor, int x, int y)
		{
			var actualColor = bitmap.ColorAtPoint(x, y);

			if (!actualColor.IsEquivalent(expectedColor))
				Assert.Equal(expectedColor, actualColor);

			return bitmap;
		}

		public static CanvasBitmap AssertColorAtCenter(this CanvasBitmap bitmap, WColor expectedColor) =>
			bitmap.AssertColorAtPoint(expectedColor, bitmap.SizeInPixels.Width / 2, bitmap.SizeInPixels.Height / 2);

		public static CanvasBitmap AssertColorAtBottomLeft(this CanvasBitmap bitmap, WColor expectedColor) =>
			bitmap.AssertColorAtPoint(expectedColor, 0, 0);

		public static CanvasBitmap AssertColorAtBottomRight(this CanvasBitmap bitmap, WColor expectedColor)
			=> bitmap.AssertColorAtPoint(expectedColor, bitmap.SizeInPixels.Width - 1, 0);

		public static CanvasBitmap AssertColorAtTopLeft(this CanvasBitmap bitmap, WColor expectedColor)
			=> bitmap.AssertColorAtPoint(expectedColor, 0, bitmap.SizeInPixels.Height - 1);

		public static CanvasBitmap AssertColorAtTopRight(this CanvasBitmap bitmap, WColor expectedColor)
			=> bitmap.AssertColorAtPoint(expectedColor, bitmap.SizeInPixels.Width - 1, bitmap.SizeInPixels.Height - 1);

		public static Task<CanvasBitmap> AssertContainsColor(this CanvasBitmap bitmap, Microsoft.Maui.Graphics.Color expectedColor, Func<Microsoft.Maui.Graphics.RectF, Microsoft.Maui.Graphics.RectF>? withinRectModifier = null, double? tolerance = null)
			=> bitmap.AssertContainsColor(ToWindowsColor(expectedColor), withinRectModifier, tolerance: tolerance);

		public static async Task<CanvasBitmap> AssertContainsColor(this CanvasBitmap bitmap, WColor expectedColor, Func<Microsoft.Maui.Graphics.RectF, Microsoft.Maui.Graphics.RectF>? withinRectModifier = null, double? tolerance = null)
		{
			var imageRect = new Microsoft.Maui.Graphics.RectF(0, 0, bitmap.SizeInPixels.Width, bitmap.SizeInPixels.Height);

			if (withinRectModifier is not null)
				imageRect = withinRectModifier.Invoke(imageRect);

			if (imageRect.Width == 0 || imageRect.Height == 0)
			{
				// Detect this case and give a better message instead of letting GetPixelColors throw an IndexOutOfRangeException
				Assert.Fail($"Bitmap must have non-zero width and height.  Width = {(int)imageRect.Width} Height = {(int)imageRect.Height}.");
				return bitmap;
			}

			var colors = bitmap.GetPixelColors((int)imageRect.X, (int)imageRect.Y, (int)imageRect.Width, (int)imageRect.Height);

			foreach (var c in colors)
			{
				if (c.IsEquivalent(expectedColor))
				{
					return bitmap;
				}
			}

			Assert.Fail(await CreateColorError(bitmap, $"Color {expectedColor} not found."));
			return bitmap;
		}

		public static Task<CanvasBitmap> AssertContainsColor(this FrameworkElement view, Microsoft.Maui.Graphics.Color expectedColor, IMauiContext mauiContext, double? tolerance = null) =>
			AssertContainsColor(view, ToWindowsColor(expectedColor), mauiContext, tolerance: tolerance);

		public static async Task<CanvasBitmap> AssertContainsColor(this FrameworkElement view, WColor expectedColor, IMauiContext mauiContext, double? tolerance = null)
		{
			var bitmap = await view.ToBitmap(mauiContext);
			return await AssertContainsColor(bitmap, expectedColor, tolerance: tolerance);
		}

		public static Task<CanvasBitmap> AssertDoesNotContainColor(this CanvasBitmap bitmap, Microsoft.Maui.Graphics.Color unexpectedColor, Func<Microsoft.Maui.Graphics.RectF, Microsoft.Maui.Graphics.RectF>? withinRectModifier = null)
			=> bitmap.AssertDoesNotContainColor(ToWindowsColor(unexpectedColor), withinRectModifier);

		public static async Task<CanvasBitmap> AssertDoesNotContainColor(this CanvasBitmap bitmap, WColor unexpectedColor, Func<Microsoft.Maui.Graphics.RectF, Microsoft.Maui.Graphics.RectF>? withinRectModifier = null)
		{
			var imageRect = new Microsoft.Maui.Graphics.RectF(0, 0, bitmap.SizeInPixels.Width, bitmap.SizeInPixels.Height);

			if (withinRectModifier is not null)
				imageRect = withinRectModifier.Invoke(imageRect);

			if (imageRect.Width == 0 || imageRect.Height == 0)
			{
				// Detect this case and give a better message instead of letting GetPixelColors throw an IndexOutOfRangeException
				Assert.Fail($"Bitmap must have non-zero width and height.  Width = {(int)imageRect.Width} Height = {(int)imageRect.Height}.");
				return bitmap;
			}

			var colors = bitmap.GetPixelColors((int)imageRect.X, (int)imageRect.Y, (int)imageRect.Width, (int)imageRect.Height);

			foreach (var c in colors)
			{
				if (c.IsEquivalent(unexpectedColor))
				{
					Assert.Fail(await CreateColorError(bitmap, $"Color {unexpectedColor} was found."));
				}
			}

			return bitmap;
		}

		public static Task<CanvasBitmap> AssertDoesNotContainColor(this FrameworkElement view, Microsoft.Maui.Graphics.Color unexpectedColor, IMauiContext mauiContext) =>
			AssertDoesNotContainColor(view, ToWindowsColor(unexpectedColor), mauiContext);

		public static async Task<CanvasBitmap> AssertDoesNotContainColor(this FrameworkElement view, WColor unexpectedColor, IMauiContext mauiContext)
		{
			var bitmap = await view.ToBitmap(mauiContext);
			return await AssertDoesNotContainColor(bitmap, unexpectedColor);
		}

		public static async Task<CanvasBitmap> AssertColorAtPointAsync(this FrameworkElement view, WColor expectedColor, int x, int y, IMauiContext mauiContext)
		{
			var bitmap = await view.ToBitmap(mauiContext);
			return bitmap.AssertColorAtPoint(expectedColor, x, y);
		}

		public static async Task<CanvasBitmap> AssertColorsAtPointsAsync(this FrameworkElement view, Microsoft.Maui.Graphics.Color[] colors, Microsoft.Maui.Graphics.Point[] points, IMauiContext mauiContext)
		{
			var bitmap = await view.ToBitmap(mauiContext);

			for (int i = 0; i < points.Length; i++)
			{
				bitmap.AssertColorAtPoint(ToWindowsColor(colors[i]), (int)points[i].X, (int)points[i].Y);
			}

			return bitmap;
		}

		public static async Task<CanvasBitmap> AssertColorAtCenterAsync(this FrameworkElement view, WColor expectedColor, IMauiContext mauiContext)
		{
			var bitmap = await view.ToBitmap(mauiContext);
			return bitmap.AssertColorAtCenter(expectedColor);
		}

		public static async Task AssertEqualAsync(this CanvasBitmap bitmap, CanvasBitmap other)
		{
			Assert.NotNull(bitmap);
			Assert.NotNull(other);

			Assert.Equal(bitmap.SizeInPixels, other.SizeInPixels);

			Assert.True(IsMatching(), await CreateEqualError(bitmap, other, $"Images did not match."));

			bool IsMatching()
			{
				var first = bitmap.GetPixelColors();
				var second = other.GetPixelColors();
				for (int i = 0; i < first.Length; i++)
				{
					if (first[i] != second[i])
						return false;
				}
				return true;
			}
		}

		public static async Task ThrowScreenshot(this FrameworkElement view, IMauiContext mauiContext, string? message = null, Exception? ex = null)
		{
			var bitmap = await view.ToBitmap(mauiContext);
			if (ex is null)
				throw new XunitException(await CreateScreenshotError(bitmap, message ?? "There was an error."));
			else
				throw new XunitException(await CreateScreenshotError(bitmap, message ?? "There was an error: " + ex.Message), ex);
		}

		static WColor ToWindowsColor(Microsoft.Maui.Graphics.Color color)
		{
			color.ToRgba(out byte red, out byte green, out byte blue, out byte alpha);
			return WColor.FromArgb(alpha, red, green, blue);
		}
	}
}
