// Ported from dotnet/maui @ 2067dd42c6032fbc40d2e46245806679a85df5e6
// Original path: src/TestUtils/src/DeviceTests/AssertionExtensions.Android.cs
// Copyright (c) .NET Foundation and Contributors. Licensed under the MIT License (see LICENSE-dotnet-maui.txt).
// Toolkit change: namespace changed from Microsoft.Maui.DeviceTests to CommunityToolkit.Maui.DeviceTests; file renamed to the toolkit's .android.cs suffix.
// Toolkit change: subset that uses public MAUI API only: focus and layout waits, AttachAndRun, bitmap and color assertions.
//                 The keyboard, accessibility and tab helpers (internal MAUI API) were dropped, the internal
//                 Java.Lang.Object.IsAlive() is replaced by a Handle check and the SearchView special case of WaitForFocused
//                 was removed. Added IsLoadedOnPlatform.
// Toolkit change: WaitForLayout disposes its timeout once the view was laid out. The pending timeout kept the view alive for a second,
//                 about as long as the 40 collections of WaitForGC take.
#nullable enable
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views;
using Android.Widget;
using AndroidX.Core.View;
using Microsoft.Maui;
using Microsoft.Maui.Platform;
using Xunit;
using Xunit.Sdk;
using static CommunityToolkit.Maui.DeviceTests.AssertHelpers;
using AColor = Android.Graphics.Color;
using AView = Android.Views.View;

namespace CommunityToolkit.Maui.DeviceTests
{
	public static partial class AssertionExtensions
	{
		public static bool IsLoadedOnPlatform(this AView view) => view.IsAttachedToWindow;

		public static async Task WaitForFocused(this AView view, int timeout = 1000, string message = "")
		{
			try
			{
				if (!view.IsFocused)
				{
					TaskCompletionSource focusSource = new TaskCompletionSource();
					view.FocusChange += OnFocused;

					try
					{
						await focusSource.Task.WaitAsync(TimeSpan.FromMilliseconds(timeout));
					}
					catch
					{
						view.FocusChange -= OnFocused;

						if (!view.IsFocused)
							throw;
					}

					// Even though the event fires focus hasn't fully been achieved
					await Task.Yield();

					void OnFocused(object? sender, AView.FocusChangeEventArgs e)
					{
						if (!e.HasFocus)
							return;

						view.FocusChange -= OnFocused;
						focusSource.SetResult();
					}
				}
			}
			catch (Exception ex)
			{
				if (!string.IsNullOrEmpty(message))
					throw new Exception(message, ex);
				else
					throw;
			}
		}

		public static async Task WaitForUnFocused(this AView view, int timeout = 1000)
		{
			if (view.IsFocused)
			{
				TaskCompletionSource focusSource = new TaskCompletionSource();
				view.FocusChange += OnUnFocused;

				try
				{
					await focusSource.Task.WaitAsync(TimeSpan.FromMilliseconds(timeout));
				}
				catch
				{
					view.FocusChange -= OnUnFocused;

					if (view.IsFocused)
						throw;
				}

				// Even though the event fires unfocus hasn't fully been achieved
				await Task.Delay(10);

				void OnUnFocused(object? sender, AView.FocusChangeEventArgs e)
				{
					if (e.HasFocus)
						return;

					view.FocusChange -= OnUnFocused;
					focusSource.TrySetResult();
				}
			}
		}

		public static Task FocusView(this AView view, int timeout = 1000)
		{
			if (!view.IsFocused)
			{
				view.Focus(new FocusRequest());
				return view.WaitForFocused(timeout);
			}

			return Task.CompletedTask;
		}

		public static Task WaitForLayoutOrNonZeroSize(this AView view, int timeout = 1000) =>
			Task.WhenAll(
				view.WaitForLayout(timeout),
#pragma warning disable CS0618 // Obsolete
				AssertEventually(() => ViewCompat.IsLaidOut(view), timeout,
#pragma warning restore CS0618 // Obsolete
					message: $"Timed out waiting for {view} to have {nameof(view.Width)} and {nameof(view.Height)} greater than zero"));

		public static Task<bool> WaitForLayout(this AView view, int timeout = 1000)
		{
			var tcs = new TaskCompletionSource<bool>();
			var cts = new CancellationTokenSource();

			view.LayoutChange += OnLayout;
			cts.Token.Register(() => OnLayout(view), true);
			cts.CancelAfter(timeout);

			return tcs.Task;

			void OnLayout(object? sender = null, AView.LayoutChangeEventArgs? e = null)
			{
				var view = (AView)sender!;

				if (view.Handle != IntPtr.Zero)
					view.LayoutChange -= OnLayout;

				if (e is not null)
					cts.Dispose();

				// let the layout resolve after changing
				tcs.TrySetResult(e != null);
			}
		}

		public static string ToBase64String(this Bitmap bitmap)
		{
			using var ms = new MemoryStream();
			bitmap.Compress(Bitmap.CompressFormat.Png!, 0, ms);
			return Convert.ToBase64String(ms.ToArray());
		}

		public static string CreateColorAtPointError(this Bitmap bitmap, AColor expectedColor, int x, int y) =>
			CreateColorError(bitmap, $"Expected {expectedColor} at point {x},{y} in rendered view.");

		public static string CreateColorError(this Bitmap bitmap, string message) =>
			$"{message} This is what it looked like:<img>{bitmap.ToBase64String()}</img>";

		public static string CreateEqualError(this Bitmap bitmap, Bitmap other, string message) =>
			$"{message} This is what it looked like: <img>{bitmap.ToBase64String()}</img> and <img>{other.ToBase64String()}</img>";

		public static string CreateScreenshotError(this Bitmap bitmap, string message) =>
			$"{message} This is what it looked like:<img>{bitmap.ToBase64String()}</img>";

		public static AColor ColorAtPoint(this Bitmap bitmap, int x, int y, bool includeAlpha = false)
		{
			int pixel = bitmap.GetPixel(x, y);

			int red = AColor.GetRedComponent(pixel);
			int blue = AColor.GetBlueComponent(pixel);
			int green = AColor.GetGreenComponent(pixel);

			if (includeAlpha)
			{
				int alpha = AColor.GetAlphaComponent(pixel);
				return AColor.Argb(alpha, red, green, blue);
			}
			else
			{
				return AColor.Rgb(red, green, blue);
			}
		}

		public static Task AttachAndRun(this AView view, Action action) =>
			view.AttachAndRun(() =>
			{
				action();
				return Task.FromResult(true);
			});

		public static Task<T> AttachAndRun<T>(this AView view, Func<T> action) =>
			view.AttachAndRun(() =>
			{
				var result = action();
				return Task.FromResult(result);
			});

		public static Task AttachAndRun(this AView view, Func<Task> action) =>
			view.AttachAndRun(async () =>
			{
				await action();
				return true;
			});

		// Android doesn't handle adding and removing views in parallel very well
		// If a view is removed while a different test triggers a layout then you hit
		// a NRE exception
		static SemaphoreSlim _attachAndRunSemaphore = new SemaphoreSlim(1);
		public static async Task<T> AttachAndRun<T>(this AView view, Func<Task<T>> action)
		{
			if (view.Parent is WrapperView wrapper)
				view = wrapper;

			if (view.Parent == null)
			{
				var context = view.Context!;
				var layout = new FrameLayout(context)
				{
					LayoutParameters = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
				};

				var width = view.Width;
				var height = view.Height;

				if (width <= 0)
					width = FrameLayout.LayoutParams.WrapContent;

				if (height <= 0)
					height = FrameLayout.LayoutParams.WrapContent;

				view.LayoutParameters = new FrameLayout.LayoutParams(width, height)
				{
					Gravity = GravityFlags.Center
				};

				// Ensure that the view gets _some_ width/height; the combination of WrapContent and Center above means that
				// the default Fill layout options will have no effect, so unless the root test object or its Content has a
				// WidthRequest/HeightRequest, the actual layout height and width may be zero

				view.SetMinimumWidth(40);
				view.SetMinimumHeight(40);

				var act = context.GetActivity()
					?? throw new InvalidOperationException($"The view's context ({context}) is not an Activity; create handlers with the test's MauiContext.");
				var rootView = act.FindViewById<FrameLayout>(global::Android.Resource.Id.Content)!;

				view.Id = AView.GenerateViewId();
				layout.Id = AView.GenerateViewId();

				var acquired = false;
				try
				{
					await _attachAndRunSemaphore.WaitAsync();
					acquired = true;
					layout.AddView(view);
					rootView.AddView(layout);
					return await Run(view, action);
				}
				finally
				{
					if (rootView.Handle != IntPtr.Zero && layout.Handle != IntPtr.Zero)
					{
						rootView.RemoveView(layout);
					}

					if (layout.Handle != IntPtr.Zero && view.Handle != IntPtr.Zero)
					{
						layout.RemoveView(view);
					}

					if (acquired)
						_attachAndRunSemaphore.Release();
				}
			}
			else
			{
				return await Run(view, action);
			}

			static async Task<T> Run(AView view, Func<Task<T>> action)
			{
				await view.WaitForLayoutOrNonZeroSize();

				return await action();
			}
		}

		public static Task<Bitmap> ToBitmap(this AView view, IMauiContext mauiContext)
		{
			return view.AttachAndRun(() =>
			{
				if (view.Parent is WrapperView wrapper)
					view = wrapper;

				var bitmap = Bitmap.CreateBitmap(view.Width, view.Height, Bitmap.Config.Argb8888!)!;
				using (var canvas = new Canvas(bitmap))
				{
					view.Draw(canvas);
				}
				return bitmap;
			});
		}

		public static Bitmap AssertColorAtPoint(this Bitmap bitmap, AColor expectedColor, int x, int y)
		{
			var actualColor = bitmap.ColorAtPoint(x, y);

			if (!actualColor.IsEquivalent(expectedColor))
				throw new XunitException(CreateColorAtPointError(bitmap, expectedColor, x, y));

			return bitmap;
		}

		public static Bitmap AssertColorAtCenter(this Drawable drawable, AColor expectedColor)
		{
			var bitmapDrawable = Assert.IsType<BitmapDrawable>(drawable);
			var bitmap = bitmapDrawable.Bitmap;
			Assert.NotNull(bitmap);

			return bitmap!.AssertColorAtCenter(expectedColor);
		}

		public static Bitmap AssertColorAtCenter(this Bitmap bitmap, AColor expectedColor)
		{
			return bitmap.AssertColorAtPoint(expectedColor, bitmap.Width / 2, bitmap.Height / 2);
		}

		public static Bitmap AssertColorAtBottomLeft(this Bitmap bitmap, AColor expectedColor)
		{
			return bitmap.AssertColorAtPoint(expectedColor, 0, 0);
		}

		public static Bitmap AssertColorAtBottomRight(this Bitmap bitmap, AColor expectedColor)
		{
			return bitmap.AssertColorAtPoint(expectedColor, bitmap.Width - 1, 0);
		}

		public static Bitmap AssertColorAtTopLeft(this Bitmap bitmap, AColor expectedColor)
		{
			return bitmap.AssertColorAtPoint(expectedColor, 0, bitmap.Height - 1);
		}

		public static Bitmap AssertColorAtTopRight(this Bitmap bitmap, AColor expectedColor)
		{
			return bitmap.AssertColorAtPoint(expectedColor, bitmap.Width - 1, bitmap.Height - 1);
		}

		public static Task<Bitmap> AssertContainsColor(this Bitmap bitmap, Microsoft.Maui.Graphics.Color expectedColor, Func<Microsoft.Maui.Graphics.RectF, Microsoft.Maui.Graphics.RectF>? withinRectModifier = null, double? tolerance = null)
			=> bitmap.AssertContainsColor(expectedColor.ToPlatform(), withinRectModifier, tolerance: tolerance);

		public static Task<Bitmap> AssertDoesNotContainColor(this Bitmap bitmap, Microsoft.Maui.Graphics.Color unexpectedColor, Func<Microsoft.Maui.Graphics.RectF, Microsoft.Maui.Graphics.RectF>? withinRectModifier = null)
			=> bitmap.AssertDoesNotContainColor(unexpectedColor.ToPlatform(), withinRectModifier);

		public static Task<Bitmap> AssertContainsColor(this Bitmap bitmap, AColor expectedColor, Func<Microsoft.Maui.Graphics.RectF, Microsoft.Maui.Graphics.RectF>? withinRectModifier = null, double? tolerance = null)
		{
			return Task.Run(() =>
			{
				var imageRect = new Microsoft.Maui.Graphics.RectF(0, 0, bitmap.Width, bitmap.Height);

				if (withinRectModifier is not null)
					imageRect = withinRectModifier.Invoke(imageRect);

				for (int x = (int)imageRect.X; x < (int)imageRect.Width; x++)
				{
					for (int y = (int)imageRect.Y; y < (int)imageRect.Height; y++)
					{
						if (bitmap.ColorAtPoint(x, y, true).IsEquivalent(expectedColor))
						{
							return bitmap;
						}
					}
				}

				throw new XunitException(CreateColorError(bitmap, $"Color {expectedColor} not found."));
			});
		}

		public static Task<Bitmap> AssertDoesNotContainColor(this Bitmap bitmap, AColor unexpectedColor, Func<Microsoft.Maui.Graphics.RectF, Microsoft.Maui.Graphics.RectF>? withinRectModifier = null)
		{
			return Task.Run(() =>
			{
				var imageRect = new Microsoft.Maui.Graphics.RectF(0, 0, bitmap.Width, bitmap.Height);

				if (withinRectModifier is not null)
					imageRect = withinRectModifier.Invoke(imageRect);

				for (int x = (int)imageRect.X; x < (int)imageRect.Width; x++)
				{
					for (int y = (int)imageRect.Y; y < (int)imageRect.Height; y++)
					{
						if (bitmap.ColorAtPoint(x, y, true).IsEquivalent(unexpectedColor))
						{
							throw new XunitException(CreateColorError(bitmap, $"Color {unexpectedColor} was found at point {x}, {y}."));
						}
					}
				}

				return bitmap;
			});
		}

		public static Task<Bitmap> AssertContainsColor(this AView view, Microsoft.Maui.Graphics.Color expectedColor, IMauiContext mauiContext, double? tolerance = null) =>
			AssertContainsColor(view, expectedColor.ToPlatform(), mauiContext, tolerance: tolerance);

		public static Task<Bitmap> AssertDoesNotContainColor(this AView view, Microsoft.Maui.Graphics.Color unexpectedColor, IMauiContext mauiContext) =>
			AssertDoesNotContainColor(view, unexpectedColor.ToPlatform(), mauiContext);

		public static async Task<Bitmap> AssertContainsColor(this AView view, AColor expectedColor, IMauiContext mauiContext, double? tolerance = null)
		{
			var bitmap = await view.ToBitmap(mauiContext).ConfigureAwait(false);
			return await AssertContainsColor(bitmap, expectedColor, tolerance: tolerance).ConfigureAwait(false);
		}

		public static async Task<Bitmap> AssertDoesNotContainColor(this AView view, AColor unexpectedColor, IMauiContext mauiContext)
		{
			var bitmap = await view.ToBitmap(mauiContext).ConfigureAwait(false);
			return await AssertDoesNotContainColor(bitmap, unexpectedColor).ConfigureAwait(false);
		}

		public static async Task<Bitmap> AssertColorAtPointAsync(this AView view, AColor expectedColor, int x, int y, IMauiContext mauiContext)
		{
			var bitmap = await view.ToBitmap(mauiContext).ConfigureAwait(false);
			return bitmap.AssertColorAtPoint(expectedColor, x, y);
		}

		public static async Task<Bitmap> AssertColorsAtPointsAsync(this AView view, Microsoft.Maui.Graphics.Color[] colors, Microsoft.Maui.Graphics.Point[] points, IMauiContext mauiContext)
		{
			var density = mauiContext.Context!.GetDisplayDensity();
			var bitmap = await view.ToBitmap(mauiContext).ConfigureAwait(false);

			for (int i = 0; i < points.Length; i++)
			{
				bitmap.AssertColorAtPoint(colors[i].ToPlatform(), (int)(points[i].X * density), (int)(points[i].Y * density));
			}

			return bitmap;
		}

		public static async Task<Bitmap> AssertColorAtCenterAsync(this AView view, AColor expectedColor, IMauiContext mauiContext)
		{
			var bitmap = await view.ToBitmap(mauiContext).ConfigureAwait(false);
			return bitmap.AssertColorAtCenter(expectedColor);
		}

		public static Task AssertEqualAsync(this Bitmap bitmap, Bitmap other)
		{
			Assert.NotNull(bitmap);
			Assert.NotNull(other);

			Assert.Equal((bitmap.Width, bitmap.Height), (other.Width, other.Height));

			Assert.True(IsMatching(bitmap, other), CreateEqualError(bitmap, other, $"Images did not match."));

			return Task.CompletedTask;
		}

		static bool IsMatching(Bitmap bitmap1, Bitmap bitmap2)
		{
			for (int x = 0; x < bitmap1.Width; x++)
			{
				for (int y = 0; y < bitmap1.Height; y++)
				{
					var first = bitmap1.ColorAtPoint(x, y, true);
					var second = bitmap2.ColorAtPoint(x, y, true);

					if (!first.IsEquivalent(second))
						return false;
				}
			}

			return true;
		}

		public static async Task ThrowScreenshot(this AView view, IMauiContext mauiContext, string? message = null, Exception? ex = null)
		{
			var bitmap = await view.ToBitmap(mauiContext).ConfigureAwait(false);
			if (ex is null)
				throw new XunitException(CreateScreenshotError(bitmap, message ?? "There was an error."));
			else
				throw new XunitException(CreateScreenshotError(bitmap, message ?? "There was an error: " + ex.Message), ex);
		}
	}
}
