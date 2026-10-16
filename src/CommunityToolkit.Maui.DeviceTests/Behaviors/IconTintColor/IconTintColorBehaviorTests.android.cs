using System.ComponentModel;
using System.Reflection;
using Android.Graphics.Drawables;
using CommunityToolkit.Maui.Behaviors;
using Google.Android.Material.Button;
using Microsoft.Maui.Platform;
using static CommunityToolkit.Maui.DeviceTests.AssertHelpers;
using AImageView = Android.Widget.ImageView;
using AWidgetButton = Android.Widget.Button;
using PlatformView = Android.Views.View;
using PorterDuff = Android.Graphics.PorterDuff;
using PorterDuffColorFilter = Android.Graphics.PorterDuffColorFilter;

namespace CommunityToolkit.Maui.DeviceTests;

// Android: an ImageView (Image, ImageButton) is tinted with a PorterDuffColorFilter (SrcIn), a MaterialButton (Button) through
// IconTint + IconTintMode, and a plain Android.Widget.Button by tinting its compound drawables. The binding does not expose the
// color of a PorterDuffColorFilter, so the color of an ImageView is verified through the rendered pixels (see the shared tests).
public partial class IconTintColorBehaviorTests
{
	static bool CanAssertTintPixels => true;

	// The MaterialButton is drawn with its own background, ripple and text color, none of which is a pure red, lime or blue.
	static bool CanAssertButtonTintPixels => true;

	static bool CanAssertPlatformViewIsCollected => false;

	// The object that stands for the image the platform view shows: a new image source gives a new drawable.
	static object? GetNativeImageIdentity(PlatformView platformView) => platformView switch
	{
		AImageView imageView => imageView.Drawable,
		MaterialButton button => button.Icon,
		_ => null
	};

	static bool HasNativeImage(PlatformView platformView) => platformView switch
	{
		AImageView imageView => imageView.Drawable is not null,
		MaterialButton button => button.Icon is not null,
		_ => false
	};

	static bool IsNativeTinted(PlatformView platformView, Color color) => platformView switch
	{
		AImageView imageView => imageView.ColorFilter is PorterDuffColorFilter,
		MaterialButton button => button.IconTint is Android.Content.Res.ColorStateList tint
			&& tint.DefaultColor == color.ToPlatform().ToArgb()
			&& button.IconTintMode is Android.Graphics.PorterDuff.Mode mode
			&& mode.Equals(PorterDuff.Mode.SrcIn),
		_ => false
	};

	// A MaterialButton always has an IconTint from MAUI (transparent, PorterDuff.Mode.Add), so "untinted" means: none of the colors
	// these tests tint with.
	static bool IsNativeUntinted(PlatformView platformView) => platformView switch
	{
		AImageView imageView => imageView.ColorFilter is null,
		MaterialButton button => button.IconTint is null || (!IsNativeTinted(platformView, Colors.Blue) && !IsNativeTinted(platformView, Colors.Green)),
		_ => false
	};

	// Puts the native view into the state of a view that was never tinted, without going through the behavior.
	static void ResetNativeTint(PlatformView platformView)
	{
		switch (platformView)
		{
			case AImageView imageView:
				imageView.ClearColorFilter();
				break;
			case MaterialButton button:
				button.IconTint = null;
				break;
			default:
				throw new InvalidOperationException($"{platformView.GetType()} cannot be reset.");
		}
	}

	static string DescribeNativeTint(PlatformView platformView) => platformView switch
	{
		AImageView imageView => $"{imageView.GetType().Name}: drawable {(imageView.Drawable is null ? "none" : imageView.Drawable.GetType().Name)}, colorFilter {(imageView.ColorFilter is null ? "none" : imageView.ColorFilter.GetType().Name)}",
		MaterialButton button => $"{button.GetType().Name}: icon {(button.Icon is null ? "none" : button.Icon.GetType().Name)}, iconTint {(button.IconTint is null ? "none" : button.IconTint.DefaultColor.ToString("X8"))}, iconTintMode {button.IconTintMode}",
		var other => other.GetType().Name
	};

	[Theory]
	[InlineData(IconKind.Image)]
	[InlineData(IconKind.ImageButton)]
	public async Task TintIsAppliedAgainWhenTheImageSourceChanges(IconKind kind)
	{
		SetupBuilder();

		var behavior = new IconTintColorBehavior { TintColor = Colors.Blue };
		var view = CreateIconView(kind);
		view.Behaviors.Add(behavior);

		await AttachAndRun(view, async handler =>
		{
			await view.OnLoadedAsync();
			await WaitForNativeImageAsync(handler);
			await AssertTintedAsync(handler, kind, Colors.Blue);

			// An ImageView keeps its color filter when the drawable changes, so take it away first: only the behavior can put it back.
			ResetNativeTint(handler.PlatformView!);
			Assert.True(IsNativeUntinted(handler.PlatformView!), DescribeNativeTint(handler.PlatformView!));

			SetIconSource(view, greenSource);

			Assert.True(IsNativeTinted(handler.PlatformView!, Colors.Blue), $"The Source property change did not apply the tint: {DescribeNativeTint(handler.PlatformView!)}");
		});
	}

	[Fact]
	public async Task TintIsNotAppliedAgainWhenAnUnrelatedImagePropertyChanges()
	{
		SetupBuilder();

		var behavior = new IconTintColorBehavior { TintColor = Colors.Blue };
		var image = new Image { Source = iconSource, WidthRequest = 40, HeightRequest = 40 };
		image.Behaviors.Add(behavior);

		await AttachAndRun(image, async handler =>
		{
			await image.OnLoadedAsync();
			await WaitForNativeImageAsync(handler);
			await AssertTintedAsync(handler, IconKind.Image, Colors.Blue);

			ResetNativeTint(handler.PlatformView!);

			image.Aspect = Aspect.Fill;
			image.Opacity = 0.5;

			Assert.True(IsNativeUntinted(handler.PlatformView!), $"An unrelated property change applied the tint: {DescribeNativeTint(handler.PlatformView!)}");
		});
	}

	[Fact]
	public async Task TintIsNotAppliedAgainWhenAnUnrelatedBehaviorPropertyChanges()
	{
		SetupBuilder();

		var behavior = new IconTintColorBehavior { TintColor = Colors.Blue };
		var image = new Image { Source = iconSource, WidthRequest = 40, HeightRequest = 40 };
		image.Behaviors.Add(behavior);

		await AttachAndRun(image, async handler =>
		{
			await image.OnLoadedAsync();
			await WaitForNativeImageAsync(handler);
			await AssertTintedAsync(handler, IconKind.Image, Colors.Blue);

			ResetNativeTint(handler.PlatformView!);

			behavior.BindingContext = new object();

			Assert.True(IsNativeUntinted(handler.PlatformView!), $"A BindingContext change applied the tint: {DescribeNativeTint(handler.PlatformView!)}");
		});
	}

	[Fact]
	public async Task ImageSourceChangesAfterTheBehaviorDetachedDoNotTintTheView()
	{
		SetupBuilder();

		var behavior = new IconTintColorBehavior { TintColor = Colors.Blue };
		var image = new Image { Source = iconSource, WidthRequest = 40, HeightRequest = 40 };
		image.Behaviors.Add(behavior);

		await AttachAndRun(image, async handler =>
		{
			await image.OnLoadedAsync();
			await WaitForNativeImageAsync(handler);
			await AssertTintedAsync(handler, IconKind.Image, Colors.Blue);

			image.Behaviors.Remove(behavior);
			Assert.True(IsNativeUntinted(handler.PlatformView!), DescribeNativeTint(handler.PlatformView!));

			image.Source = greenSource;

			Assert.True(IsNativeUntinted(handler.PlatformView!), $"The detached behavior tinted the view: {DescribeNativeTint(handler.PlatformView!)}");
		});
	}

	[Fact]
	public async Task ChangesRaisedWithoutAPropertyNameAreIgnored()
	{
		SetupBuilder();

		var behavior = new IconTintColorBehavior { TintColor = Colors.Blue };
		var image = new NamelessPropertyChangedImage { Source = iconSource, WidthRequest = 40, HeightRequest = 40 };
		image.Behaviors.Add(behavior);

		await AttachAndRun(image, async handler =>
		{
			await image.OnLoadedAsync();
			await WaitForNativeImageAsync(handler);
			await AssertTintedAsync(handler, IconKind.Image, Colors.Blue);

			ResetNativeTint(handler.PlatformView!);

			// MAUI raises PropertyChanged first and then fails to map a null property name in the image handler.
			var exception = Record.Exception(image.RaisePropertyChangedWithoutAName);

			Assert.True(exception is null or ArgumentNullException, $"Unexpected exception: {exception}");
			Assert.True(IsNativeUntinted(handler.PlatformView!), $"A change without a property name applied the tint: {DescribeNativeTint(handler.PlatformView!)}");
		});
	}

	// OnElementPropertyChanged is subscribed to the PropertyChanged event of the view the behavior is attached to. MAUI always raises that
	// event with the view as its sender, and a view whose handler is removed unloads, which detaches the behavior before the handler
	// guard can matter. The two guards are defensive: the private handler is called directly to reach them.
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ElementPropertyChangeIsIgnoredWhenItsSenderIsNotAViewWithAHandler(bool senderIsAView)
	{
		SetupBuilder();

		var behavior = new IconTintColorBehavior { TintColor = Colors.Blue };
		var image = new Image { Source = iconSource, WidthRequest = 40, HeightRequest = 40 };
		image.Behaviors.Add(behavior);

		await AttachAndRun(image, async handler =>
		{
			await image.OnLoadedAsync();
			await WaitForNativeImageAsync(handler);
			await AssertTintedAsync(handler, IconKind.Image, Colors.Blue);

			ResetNativeTint(handler.PlatformView!);

			// A Source change that does not come from a view, or that comes from a view without a handler: nothing to tint.
			object sender = senderIsAView ? new Image { Source = greenSource } : new object();
			InvokeElementPropertyChanged(behavior, sender, nameof(Image.Source));

			Assert.True(IsNativeUntinted(handler.PlatformView!), $"The change of an unusable sender applied the tint: {DescribeNativeTint(handler.PlatformView!)}");

			// The same call with the attached view reaches the tint, so the handler was invoked correctly.
			InvokeElementPropertyChanged(behavior, image, nameof(Image.Source));

			Assert.True(IsNativeTinted(handler.PlatformView!, Colors.Blue), DescribeNativeTint(handler.PlatformView!));
		});
	}

	[Fact]
	public async Task DisposedPlatformViewIsIgnoredWhenTheBehaviorAttachesAndDetaches()
	{
		SetupBuilder();

		var probe = new IconTintColorBehaviorProbe { TintColor = Colors.Blue };
		var image = new Image();

		await InvokeOnMainThreadAsync(() =>
		{
			// Both ApplyTintColor and ClearTintColor swallow the ObjectDisposedException of a platform view that was disposed.
			var imageView = new AImageView(MauiContext.Context!);
			imageView.Dispose();
			var attachException = Record.Exception(() => probe.AttachTo(image, imageView));
			var detachException = Record.Exception(() => probe.DetachFrom(image, imageView));

			Assert.Null(attachException);
			Assert.Null(detachException);

			var materialButton = new MaterialButton(MauiContext.Context!);
			materialButton.Dispose();
			var buttonAttachException = Record.Exception(() => probe.AttachTo(image, materialButton));
			var buttonDetachException = Record.Exception(() => probe.DetachFrom(image, materialButton));

			Assert.Null(buttonAttachException);
			Assert.Null(buttonDetachException);

			var widgetButton = new AWidgetButton(MauiContext.Context!);
			widgetButton.Dispose();
			var widgetAttachException = Record.Exception(() => probe.AttachTo(image, widgetButton));
			var widgetDetachException = Record.Exception(() => probe.DetachFrom(image, widgetButton));

			Assert.Null(widgetAttachException);
			Assert.Null(widgetDetachException);
		});
	}

	[Fact]
	public async Task MissingPlatformViewIsIgnoredWhenTheBehaviorAttachesAndDetaches()
	{
		SetupBuilder();

		var probe = new IconTintColorBehaviorProbe { TintColor = Colors.Blue };
		var image = new Image();

		await InvokeOnMainThreadAsync(() =>
		{
			// ApplyTintColor and ClearTintColor return when they have no platform view to work on.
			var attachException = Record.Exception(() => probe.AttachTo(image, null!));
			var detachException = Record.Exception(() => probe.DetachFrom(image, null!));

			Assert.Null(attachException);
			Assert.Null(detachException);
		});
	}

	[Fact]
	public async Task UnsupportedPlatformViewIsIgnoredWhenTheBehaviorDetaches()
	{
		SetupBuilder();

		var label = new Label { Text = "Not an icon" };
		var handler = (IPlatformViewHandler)await CreateHandlerAsync(label);
		var probe = new IconTintColorBehaviorProbe { TintColor = Colors.Blue };

		await InvokeOnMainThreadAsync(() =>
		{
			// Unlike attaching, ClearTintColor has no default case: a view it does not know is left alone.
			var exception = Record.Exception(() => probe.DetachFrom(label, handler.PlatformView!));

			Assert.Null(exception);
		});
	}

	// MAUI never creates a plain Android.Widget.Button (it uses MaterialButton), but a custom handler can, and the behavior has a
	// separate code path for it that tints the compound drawables.
	[Fact]
	public async Task PlainButtonCompoundDrawablesAreTinted()
	{
		SetupBuilder();

		var probe = new IconTintColorBehaviorProbe { TintColor = Colors.Blue };

		await InvokeOnMainThreadAsync(async () =>
		{
			var button = CreatePlainButtonWithFourDrawables();

			await button.AttachAndRun(async () =>
			{
				await WaitForPixelsAsync(button, iconColor, Colors.Blue);

				probe.AttachTo(new Button(), button);

				await WaitForPixelsAsync(button, Colors.Blue, iconColor);
			});
		});
	}

	// Whatever color the icons get when the TintColor is cleared (see PlainButtonCompoundDrawablesShowTheirOriginalColorWhenTheTintColorIsNull),
	// the behavior has to keep them on the button, and clearing the tint of a button that has all four drawables must not throw.
	[Fact]
	public async Task PlainButtonKeepsItsCompoundDrawablesWhenTheTintColorIsNull()
	{
		SetupBuilder();

		var probe = new IconTintColorBehaviorProbe { TintColor = Colors.Blue };

		await InvokeOnMainThreadAsync(async () =>
		{
			var button = CreatePlainButtonWithFourDrawables();

			await button.AttachAndRun(async () =>
			{
				probe.AttachTo(new Button(), button);
				await WaitForPixelsAsync(button, Colors.Blue, iconColor);

				var exception = Record.Exception(() => probe.TintColor = null);

				Assert.Null(exception);
				Assert.All(button.GetCompoundDrawables(), static drawable => Assert.NotNull(drawable));
			});
		});
	}

	[Fact]
	public async Task PlainButtonKeepsItsCompoundDrawablesWhenTheBehaviorDetaches()
	{
		SetupBuilder();

		var probe = new IconTintColorBehaviorProbe { TintColor = Colors.Blue };

		await InvokeOnMainThreadAsync(async () =>
		{
			var button = CreatePlainButtonWithFourDrawables();

			await button.AttachAndRun(async () =>
			{
				probe.AttachTo(new Button(), button);
				await WaitForPixelsAsync(button, Colors.Blue, iconColor);

				var exception = Record.Exception(() => probe.DetachFrom(new Button(), button));

				Assert.Null(exception);
				Assert.All(button.GetCompoundDrawables(), static drawable => Assert.NotNull(drawable));
			});
		});
	}

	[Fact]
	public async Task PlainButtonCompoundDrawablesShowTheirOriginalColorWhenTheTintColorIsNull()
	{
		SetupBuilder();

		var probe = new IconTintColorBehaviorProbe { TintColor = Colors.Blue };

		await InvokeOnMainThreadAsync(async () =>
		{
			var button = CreatePlainButtonWithFourDrawables();

			await button.AttachAndRun(async () =>
			{
				probe.AttachTo(new Button(), button);
				await WaitForPixelsAsync(button, Colors.Blue, iconColor);

				probe.TintColor = null;

				await WaitForPixelsAsync(button, iconColor, Colors.Blue);
			});
		});
	}

	[Fact]
	public async Task PlainButtonCompoundDrawablesShowTheirOriginalColorWhenTheBehaviorDetaches()
	{
		SetupBuilder();

		var probe = new IconTintColorBehaviorProbe { TintColor = Colors.Blue };

		await InvokeOnMainThreadAsync(async () =>
		{
			var button = CreatePlainButtonWithFourDrawables();

			await button.AttachAndRun(async () =>
			{
				probe.AttachTo(new Button(), button);
				await WaitForPixelsAsync(button, Colors.Blue, iconColor);

				probe.DetachFrom(new Button(), button);

				await WaitForPixelsAsync(button, iconColor, Colors.Blue);
			});
		});
	}

	[Fact]
	public async Task PlainButtonWithMissingCompoundDrawablesCanBeTinted()
	{
		SetupBuilder();

		var probe = new IconTintColorBehaviorProbe { TintColor = Colors.Blue };

		await InvokeOnMainThreadAsync(async () =>
		{
			// getCompoundDrawables() returns null for the sides that have no drawable.
			var button = new AWidgetButton(MauiContext.Context!) { Text = "Icon" };
			var drawable = new ColorDrawable(Android.Graphics.Color.Red);
			drawable.SetBounds(0, 0, 20, 20);
			button.SetCompoundDrawables(drawable, null, null, null);

			await button.AttachAndRun(async () =>
			{
				var exception = Record.Exception(() => probe.AttachTo(new Button(), button));

				Assert.Null(exception);
				await WaitForPixelsAsync(button, Colors.Blue, iconColor);
			});
		});
	}

	// The detach side of PlainButtonWithMissingCompoundDrawablesCanBeTinted: ClearTintColor walks getCompoundDrawables() too, and catches
	// only ObjectDisposedException. The behavior detaches without attaching first, so that the attach path cannot hide a failure of this one.
	[Fact]
	public async Task PlainButtonWithMissingCompoundDrawablesDetachesWithoutAnException()
	{
		SetupBuilder();

		var probe = new IconTintColorBehaviorProbe { TintColor = Colors.Blue };

		await InvokeOnMainThreadAsync(() =>
		{
			// getCompoundDrawables() returns null for the sides that have no drawable.
			var button = new AWidgetButton(MauiContext.Context!) { Text = "Icon" };
			var drawable = new ColorDrawable(Android.Graphics.Color.Red);
			drawable.SetBounds(0, 0, 20, 20);
			button.SetCompoundDrawables(drawable, null, null, null);

			var exception = Record.Exception(() => probe.DetachFrom(new Button(), button));

			Assert.Null(exception);

			// The button keeps its one drawable, and its other sides stay empty.
			var drawables = button.GetCompoundDrawables();
			Assert.NotNull(drawables[0]);
			Assert.All(drawables.Skip(1), static side => Assert.Null(side));
		});
	}

	[Fact]
	public async Task MaterialButtonWithoutTintColorDoesNotThrowWhenTheBehaviorAttaches()
	{
		SetupBuilder();

		var probe = new IconTintColorBehaviorProbe();
		var button = (Button)CreateIconView(IconKind.Button);

		await AttachAndRun(button, async handler =>
		{
			await button.OnLoadedAsync();
			await WaitForNativeImageAsync(handler);

			// The behavior is not added to the Button: MAUI would call it from the Loaded callback, where an exception ends the app.
			var exception = Record.Exception(() => probe.AttachTo(button, handler.PlatformView!));

			Assert.Null(exception);
			Assert.True(IsNativeUntinted(handler.PlatformView!), DescribeNativeTint(handler.PlatformView!));
		});
	}

	[Fact]
	public async Task ImageViewIsTintedBeforeItHasAnImage()
	{
		SetupBuilder();

		var behavior = new IconTintColorBehavior { TintColor = Colors.Blue };
		var image = new Image { WidthRequest = 40, HeightRequest = 40 };
		image.Behaviors.Add(behavior);

		await AttachAndRun(image, async handler =>
		{
			await image.OnLoadedAsync();

			// Unlike the Apple implementation, the color filter does not need an image: it stays when the image arrives.
			Assert.False(HasNativeImage(handler.PlatformView!));
			Assert.True(IsNativeTinted(handler.PlatformView!, Colors.Blue), DescribeNativeTint(handler.PlatformView!));

			image.Source = iconSource;

			await WaitForNativeImageAsync(handler);
			await AssertTintedAsync(handler, IconKind.Image, Colors.Blue, checkPixels: true);
		});
	}

	// The color filter (PorterDuff SrcIn) replaces the colors of the image and keeps its shape. behavior_disc.png is an opaque red disc with
	// transparent corners: with a half-transparent tint, the disc shows the tint with its alpha and none of the red (SrcAtop would blend the
	// two), and the corners stay transparent (Src would fill them).
	[Fact]
	public async Task HalfTransparentTintReplacesTheColorsOfTheImageAndKeepsItsShape()
	{
		SetupBuilder();

		var tint = Colors.Blue.WithAlpha(0.5f);
		var image = new Image { Source = iconSource, WidthRequest = 40, HeightRequest = 40 };
		image.Behaviors.Add(new IconTintColorBehavior { TintColor = tint });

		await AttachAndRun(image, async handler =>
		{
			await image.OnLoadedAsync();
			await WaitForNativeImageAsync(handler);

			var platformView = handler.PlatformView!;
			var expected = tint.ToPlatform();
			int center = 0, corner = 0;

			// The image loads and is laid out asynchronously: wait until the disc is drawn.
			await AssertEventually(() =>
			{
				(center, corner) = DrawCenterAndCorner(platformView);
				return Android.Graphics.Color.GetAlphaComponent(center) > 0;
			}, timeout: 3000, message: "The image was never drawn.");

			Assert.True(IsCloseTo(center, expected), $"The disc is drawn as {center:X8}, not in the tint {expected.ToArgb():X8}.");
			Assert.Equal(0, Android.Graphics.Color.GetAlphaComponent(corner));
		});
	}

	// The pixel in the middle of the view (inside the disc) and the one in its top left corner (outside the disc), as non-premultiplied ARGB.
	static (int Center, int Corner) DrawCenterAndCorner(PlatformView platformView)
	{
		if (platformView.Width <= 0 || platformView.Height <= 0)
		{
			return (0, 0);
		}

		using var bitmap = Android.Graphics.Bitmap.CreateBitmap(platformView.Width, platformView.Height, Android.Graphics.Bitmap.Config.Argb8888!)!;
		using (var canvas = new Android.Graphics.Canvas(bitmap))
		{
			platformView.Draw(canvas);
		}

		return (bitmap.GetPixel(bitmap.Width / 2, bitmap.Height / 2), bitmap.GetPixel(0, 0));
	}

	// Blending a half-transparent color goes through premultiplied values, which can be one or two steps off per channel.
	static bool IsCloseTo(int pixel, Android.Graphics.Color expected) =>
		Math.Abs(Android.Graphics.Color.GetAlphaComponent(pixel) - expected.A) <= 3
		&& Math.Abs(Android.Graphics.Color.GetRedComponent(pixel) - expected.R) <= 3
		&& Math.Abs(Android.Graphics.Color.GetGreenComponent(pixel) - expected.G) <= 3
		&& Math.Abs(Android.Graphics.Color.GetBlueComponent(pixel) - expected.B) <= 3;

	// The behavior keeps the platform view it attached to in a field. A detached behavior that is still referenced (a shared instance, a
	// StaticResource, a view model) must not keep that platform view, and with it the Activity, alive. The field is read directly: when a
	// Java peer is collected is up to the Java garbage collector, so waiting for the platform view to be collected proves nothing here.
	[Fact]
	public async Task DetachedBehaviorReleasesItsPlatformView()
	{
		SetupBuilder();

		var behavior = new IconTintColorBehavior { TintColor = Colors.Blue };
		var image = CreateIconView(IconKind.Image);
		image.Behaviors.Add(behavior);
		var nativeViewField = typeof(IconTintColorBehavior).GetField("nativeView", BindingFlags.Instance | BindingFlags.NonPublic);
		Assert.True(nativeViewField is not null, "IconTintColorBehavior.nativeView no longer exists: update this test.");

		await AttachAndRun(image, async handler =>
		{
			await image.OnLoadedAsync();
			await WaitForNativeImageAsync(handler);
			await AssertTintedAsync(handler, IconKind.Image, Colors.Blue);
			Assert.Same(handler.PlatformView, nativeViewField.GetValue(behavior));

			image.Behaviors.Remove(behavior);

			Assert.True(IsNativeUntinted(handler.PlatformView!), DescribeNativeTint(handler.PlatformView!));
			Assert.Null(nativeViewField.GetValue(behavior));
		});
	}

	static void InvokeElementPropertyChanged(IconTintColorBehavior behavior, object? sender, string propertyName)
	{
		var handler = typeof(IconTintColorBehavior).GetMethod("OnElementPropertyChanged", BindingFlags.Instance | BindingFlags.NonPublic);
		Assert.True(handler is not null, "IconTintColorBehavior.OnElementPropertyChanged no longer exists: update the tests that call it.");

		handler.Invoke(behavior, [sender, new PropertyChangedEventArgs(propertyName)]);
	}

	AWidgetButton CreatePlainButtonWithFourDrawables()
	{
		var button = new AWidgetButton(MauiContext.Context!) { Text = "Icon" };

		Drawable CreateDrawable()
		{
			var drawable = new ColorDrawable(Android.Graphics.Color.Red);
			drawable.SetBounds(0, 0, 20, 20);
			return drawable;
		}

		button.SetCompoundDrawables(CreateDrawable(), CreateDrawable(), CreateDrawable(), CreateDrawable());
		return button;
	}

	// Raises PropertyChanged on an Image without a property name, which OnElementPropertyChanged has to ignore.
	sealed class NamelessPropertyChangedImage : Image
	{
		public void RaisePropertyChangedWithoutAName() => OnPropertyChanged(null);
	}
}