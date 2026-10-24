using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.Maui.Behaviors;
using CoreGraphics;
using Foundation;
using ObjCRuntime;
using UIKit;
using static CommunityToolkit.Maui.DeviceTests.AssertHelpers;
using PlatformView = UIKit.UIView;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class IconTintColorBehaviorTests
{
	public enum TintRemoval
	{
		RemoveBehavior,
		NullTintColor,
		Unload
	}

	public enum TextColorChange
	{
		FromNoneToLime,
		FromLimeToNone
	}

	const string objCLibrary = "/usr/lib/libobjc.dylib";

	static bool CanAssertTintPixels => true;

	static bool CanAssertButtonTintPixels => true;

	static bool CanAssertPlatformViewIsCollected => true;

	static UIImage? GetNativeImage(PlatformView platformView) => platformView switch
	{
		UIImageView imageView => imageView.Image,
		UIButton button => button.CurrentImage,
		_ => null
	};

	static UIColor? GetNativeTintColor(PlatformView platformView) => platformView switch
	{
		UIImageView imageView => imageView.TintColor,
		UIButton button => button.TintColor,
		_ => null
	};

	// A new image source gives a new UIImage.
	static object? GetNativeImageIdentity(PlatformView platformView) => GetNativeImage(platformView);

	static bool HasNativeImage(PlatformView platformView) => GetNativeImage(platformView) is not null;

	static bool IsNativeTinted(PlatformView platformView, Color color) =>
		GetNativeImage(platformView)?.RenderingMode == UIImageRenderingMode.AlwaysTemplate
		&& IsSameColor(GetNativeTintColor(platformView), color)
		&& (platformView is not UIButton button || IsSameColor(button.ImageView.TintColor, color));

	static bool IsNativeUntinted(PlatformView platformView) =>
		GetNativeImage(platformView)?.RenderingMode != UIImageRenderingMode.AlwaysTemplate;

	static void ResetNativeTint(PlatformView platformView)
	{
		switch (platformView)
		{
			case UIImageView { Image: not null } imageView:
				imageView.Image = imageView.Image.ImageWithRenderingMode(UIImageRenderingMode.AlwaysOriginal);
				break;
			case UIButton { CurrentImage: not null } button:
				button.SetImage(button.CurrentImage.ImageWithRenderingMode(UIImageRenderingMode.AlwaysOriginal), UIControlState.Normal);
				break;
			default:
				throw new InvalidOperationException($"{platformView.GetType()} has no image to reset.");
		}
	}

	static string DescribeNativeTint(PlatformView platformView)
	{
		var image = GetNativeImage(platformView);

		return $"{platformView.GetType().Name}: image {(image is null ? "none" : image.RenderingMode.ToString())}, tintColor {DescribeColor(GetNativeTintColor(platformView))}";
	}

	// Every tint color the behavior writes: the view's own and, for a UIButton, the one of its image view.
	static string DescribeNativeTintColors(PlatformView platformView) => platformView switch
	{
		UIButton button => $"button.tintColor {DescribeColor(button.TintColor)}, imageView.tintColor {DescribeColor(button.ImageView.TintColor)}",
		_ => $"tintColor {DescribeColor(GetNativeTintColor(platformView))}"
	};

	// The title colors of the states whose title color MAUI's ButtonHandler sets.
	static string DescribeTitleColors(UIButton button) =>
		string.Join(", ", new[] { UIControlState.Normal, UIControlState.Highlighted, UIControlState.Disabled }.Select(state => $"{state} {DescribeColor(button.TitleColor(state))}"));

	// Every color the behavior writes: the tint colors and, for a UIButton, the title colors.
	static string DescribeNativeColors(PlatformView platformView) => platformView switch
	{
		UIButton button => $"{DescribeNativeTintColors(button)}, title colors {DescribeTitleColors(button)}",
		_ => DescribeNativeTintColors(platformView)
	};

	// The observationInfo (NSKeyValueObserving) of the button's image view describes its key-value observations. The managed UIImageView is
	// only used in here, so that nothing in the test keeps it alive.
	[MethodImpl(MethodImplOptions.NoInlining)]
	static int CountImageObservations(UIButton button)
	{
		var imageView = button.ImageView;
		var observationInfo = MessageReturningIntPtr(imageView.Handle, Selector.GetHandle("observationInfo"));

		if (observationInfo == IntPtr.Zero)
		{
			return 0;
		}

		var description = Runtime.GetNSObject<NSString>(MessageReturningIntPtr(observationInfo, Selector.GetHandle("description")))?.ToString() ?? string.Empty;

		return description.Split("Key path: image").Length - 1;
	}

	[DllImport(objCLibrary, EntryPoint = "objc_msgSend")]
	static extern IntPtr MessageReturningIntPtr(IntPtr receiver, IntPtr selector);

	// A dynamic color is described as it looks in an active window. On Mac Catalyst the colors that follow the window's tint color are
	// dynamic, and look different while the window has the inactive appearance (it is not in front, or it presents an alert).
	static string DescribeColor(UIColor? color)
	{
		if (color is null)
		{
			return "none";
		}

		if (OperatingSystem.IsIOSVersionAtLeast(17) || OperatingSystem.IsMacCatalystVersionAtLeast(17))
		{
			color = color.GetResolvedColor(UITraitCollection.CurrentTraitCollection.GetTraitCollectionByModifyingTraits(traits => traits.ActiveAppearance = UIUserInterfaceActiveAppearance.Active));
		}

		color.GetRGBA(out var red, out var green, out var blue, out var alpha);
		return $"({red:0.##}, {green:0.##}, {blue:0.##}, {alpha:0.##})";
	}

	static bool IsSameColor(UIColor? nativeColor, Color expected)
	{
		if (nativeColor is null)
		{
			return false;
		}

		nativeColor.GetRGBA(out var red, out var green, out var blue, out var alpha);

		return Math.Abs((double)red - expected.Red) < 0.02
			&& Math.Abs((double)green - expected.Green) < 0.02
			&& Math.Abs((double)blue - expected.Blue) < 0.02
			&& Math.Abs((double)alpha - expected.Alpha) < 0.02;
	}

	[Fact]
	public async Task ButtonWithoutAnImageIsIgnored()
	{
		SetupBuilder();

		var behavior = new CountingIconTintColorBehavior { TintColor = Colors.Blue };
		var button = new Button { Text = "No icon", WidthRequest = 80, HeightRequest = 50 };
		button.Behaviors.Add(behavior);

		await AttachAndRun(button, async handler =>
		{
			await button.OnLoadedAsync();
			var uiButton = (UIButton)handler.PlatformView!;

			Assert.Equal(1, behavior.AttachedCount);
			Assert.Null(uiButton.CurrentImage);
			Assert.Null(uiButton.ImageView.Image);
			Assert.False(IsSameColor(uiButton.TintColor, Colors.Blue), "The behavior tinted a button that has no image.");

			var exception = Record.Exception(() => button.Behaviors.Remove(behavior));

			Assert.Null(exception);
			Assert.Equal(1, behavior.DetachedCount);
			Assert.Null(uiButton.CurrentImage);
		});
	}

	[Fact]
	public async Task ImageViewWithoutAnImageIsNotTintedUntilItsImageIsSet()
	{
		SetupBuilder();

		var behavior = new IconTintColorBehavior { TintColor = Colors.Blue };
		var image = new Image { WidthRequest = 40, HeightRequest = 40 };
		image.Behaviors.Add(behavior);

		await AttachAndRun(image, async handler =>
		{
			await image.OnLoadedAsync();

			Assert.False(HasNativeImage(handler.PlatformView!));
			Assert.True(IsNativeUntinted(handler.PlatformView!), DescribeNativeTint(handler.PlatformView!));
			Assert.False(IsSameColor(GetNativeTintColor(handler.PlatformView!), Colors.Blue), $"The behavior tinted an image view that has no image: {DescribeNativeTint(handler.PlatformView!)}");

			image.Source = iconSource;

			await WaitForNativeImageAsync(handler);
			await AssertTintedAsync(handler, IconKind.Image, Colors.Blue);
		});
	}

	[Fact]
	public async Task ImageViewWithoutAnImageIsIgnoredWhenTheBehaviorDetaches()
	{
		SetupBuilder();

		var behavior = new CountingIconTintColorBehavior { TintColor = Colors.Blue };
		var image = new Image { WidthRequest = 40, HeightRequest = 40 };
		image.Behaviors.Add(behavior);

		await AttachAndRun(image, async handler =>
		{
			await image.OnLoadedAsync();
			var imageView = (UIImageView)handler.PlatformView!;

			Assert.Equal(1, behavior.AttachedCount);
			Assert.Null(imageView.Image);

			var exception = Record.Exception(() => image.Behaviors.Remove(behavior));

			Assert.Null(exception);
			Assert.Equal(1, behavior.DetachedCount);
			Assert.Null(imageView.Image);
		});
	}

	[Theory]
	[InlineData(IconKind.Image)]
	[InlineData(IconKind.ImageButton)]
	public async Task TintIsOnlyAppliedAgainOnceTheImageFinishedLoading(IconKind kind)
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

			ResetNativeTint(handler.PlatformView!);
			Assert.True(IsNativeUntinted(handler.PlatformView!), DescribeNativeTint(handler.PlatformView!));

			// IsLoading changes, as MAUI raises them around a load of the image source.
			var imageController = (IImageController)view;
			imageController.SetIsLoading(true);

			Assert.True(IsNativeUntinted(handler.PlatformView!), $"The tint is applied while the image is loading: {DescribeNativeTint(handler.PlatformView!)}");

			imageController.SetIsLoading(false);

			Assert.True(IsNativeTinted(handler.PlatformView!, Colors.Blue), $"The tint is not applied once the image finished loading: {DescribeNativeTint(handler.PlatformView!)}");
		});
	}

	[Theory]
	[InlineData(IconKind.Image)]
	[InlineData(IconKind.ImageButton)]
	public async Task TintIsAppliedAgainWhenTheImageSourcePropertyChanges(IconKind kind)
	{
		SetupBuilder();

		var behavior = new IconTintColorBehaviorProbe { TintColor = Colors.Blue };
		var view = CreateIconView(kind);
		view.Behaviors.Add(behavior);

		await AttachAndRun(view, async handler =>
		{
			await view.OnLoadedAsync();
			await WaitForNativeImageAsync(handler);
			await AssertTintedAsync(handler, kind, Colors.Blue);

			ResetNativeTint(handler.PlatformView!);
			behavior.RaiseViewPropertyChanged(view, nameof(Image.Aspect));
			Assert.True(IsNativeUntinted(handler.PlatformView!), $"An unrelated property change applied the tint: {DescribeNativeTint(handler.PlatformView!)}");

			behavior.RaiseViewPropertyChanged(view, nameof(Image.Source));
			Assert.True(IsNativeTinted(handler.PlatformView!, Colors.Blue), $"The Source property change did not apply the tint: {DescribeNativeTint(handler.PlatformView!)}");
		});
	}

	[Fact]
	public async Task TintIsNotAppliedAgainWhenAnUnrelatedBehaviorPropertyChanges()
	{
		SetupBuilder();

		var behavior = new IconTintColorBehavior { TintColor = Colors.Blue };
		var image = CreateIconView(IconKind.Image);
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

	// Every way the tint goes away runs ClearTintColor, and the behavior wrote the view's tint color, so each of them must give back the
	// color the view had.
	[Theory]
	[InlineData(IconKind.Image, TintRemoval.RemoveBehavior)]
	[InlineData(IconKind.ImageButton, TintRemoval.RemoveBehavior)]
	[InlineData(IconKind.Button, TintRemoval.RemoveBehavior)]
	[InlineData(IconKind.Image, TintRemoval.NullTintColor)]
	[InlineData(IconKind.ImageButton, TintRemoval.NullTintColor)]
	[InlineData(IconKind.Button, TintRemoval.NullTintColor)]
	[InlineData(IconKind.Image, TintRemoval.Unload)]
	[InlineData(IconKind.ImageButton, TintRemoval.Unload)]
	[InlineData(IconKind.Button, TintRemoval.Unload)]
	public async Task NativeTintColorIsRestoredWhenTheTintIsRemoved(IconKind kind, TintRemoval removal)
	{
		SetupBuilder();

		var view = CreateIconView(kind);
		var behavior = new CountingIconTintColorBehavior { TintColor = Colors.Blue };
		string? tintColorsBefore = null;

		await AttachAndRun(view, async handler =>
		{
			await view.OnLoadedAsync();
			await WaitForNativeImageAsync(handler);
			tintColorsBefore = DescribeNativeTintColors(handler.PlatformView!);

			view.Behaviors.Add(behavior);
			await AssertTintedAsync(handler, kind, Colors.Blue);

			switch (removal)
			{
				case TintRemoval.RemoveBehavior:
					view.Behaviors.Remove(behavior);
					await AssertNativeTintColorIsRestoredAsync(handler, kind, tintColorsBefore);
					break;
				case TintRemoval.NullTintColor:
					behavior.TintColor = null;
					await AssertNativeTintColorIsRestoredAsync(handler, kind, tintColorsBefore);
					break;
				case TintRemoval.Unload:
					// The view leaves the window when this action returns, which detaches the behavior (see below).
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(removal), removal, null);
			}
		});

		if (removal is TintRemoval.Unload)
		{
			await view.OnUnloadedAsync();
			await WaitForDetachAsync(behavior, 1);

			// The tint color a view inherits depends on where it is, so it is compared in the window again. The behavior has detached; it
			// is removed before the view goes back, so that it cannot attach again.
			await InvokeOnMainThreadAsync(() =>
			{
				view.Behaviors.Remove(behavior);
			});

			await AttachAndRun(view, async handler =>
			{
				await view.OnLoadedAsync();
				await WaitForNativeImageAsync(handler);

				Assert.Equal(1, behavior.AttachedCount);
				Assert.Equal(1, behavior.DetachedCount);
				await AssertNativeTintColorIsRestoredAsync(handler, kind, tintColorsBefore);
			});
		}
	}

	async Task AssertNativeTintColorIsRestoredAsync(IPlatformViewHandler handler, IconKind kind, string? tintColorsBefore)
	{
		await AssertUntintedAsync(handler, kind);

		// The behavior shows the image in its own colors again (AlwaysOriginal); Automatic, the mode of an image that was never tinted,
		// would let a template image or a button follow the tint color.
		Assert.Equal(UIImageRenderingMode.AlwaysOriginal, GetNativeImage(handler.PlatformView!)?.RenderingMode);
		Assert.Equal(tintColorsBefore, DescribeNativeTintColors(handler.PlatformView!));
	}

	// On Android and Windows the title of a Button keeps its color; a UIButton's title follows its tintColor when the Button has no
	// TextColor, so the title must not get the tint color, neither while the behavior is attached nor after.
	[Fact]
	public async Task ButtonTitleColorIsNotChangedByTheTint()
	{
		SetupBuilder();

		var button = new Button { Text = "Icon", ImageSource = iconSource, WidthRequest = 120, HeightRequest = 50 };

		await AttachAndRun(button, async handler =>
		{
			await button.OnLoadedAsync();
			await WaitForNativeImageAsync(handler);
			var uiButton = (UIButton)handler.PlatformView!;
			var titleColorBefore = DescribeColor(uiButton.CurrentTitleColor);

			var behavior = new IconTintColorBehavior { TintColor = Colors.Blue };
			button.Behaviors.Add(behavior);
			await AssertTintedAsync(handler, IconKind.Button, Colors.Blue);

			Assert.Equal(titleColorBefore, DescribeColor(uiButton.CurrentTitleColor));

			button.Behaviors.Remove(behavior);
			await AssertUntintedAsync(handler, IconKind.Button);

			Assert.Equal(titleColorBefore, DescribeColor(uiButton.CurrentTitleColor));
		});
	}

	[Fact]
	public async Task ButtonTitleColorsOfTheStatesMauiStylesAreNotChangedByTheTint()
	{
		SetupBuilder();

		var button = new Button { Text = "Icon", ImageSource = iconSource, WidthRequest = 120, HeightRequest = 50 };

		await AttachAndRun(button, async handler =>
		{
			await button.OnLoadedAsync();
			await WaitForNativeImageAsync(handler);
			var uiButton = (UIButton)handler.PlatformView!;
			var titleColorsBefore = DescribeTitleColors(uiButton);

			var behavior = new IconTintColorBehavior { TintColor = Colors.Blue };
			button.Behaviors.Add(behavior);
			await AssertTintedAsync(handler, IconKind.Button, Colors.Blue);

			Assert.Equal(titleColorsBefore, DescribeTitleColors(uiButton));

			button.Behaviors.Remove(behavior);
			await AssertUntintedAsync(handler, IconKind.Button);

			Assert.Equal(titleColorsBefore, DescribeTitleColors(uiButton));
		});
	}

	// MAUI maps Button.TextColor to the tint color and the title colors of the UIButton, so a TextColor change gives the button the color of
	// its text as its tint color (ButtonExtensions.UpdateTextColor).
	[Fact]
	public async Task IconKeepsTheTintWhenTheButtonTextColorChanges()
	{
		SetupBuilder();

		var behavior = new IconTintColorBehavior { TintColor = Colors.Blue };
		var button = (Button)CreateIconView(IconKind.Button);
		button.Behaviors.Add(behavior);

		await AttachAndRun(button, async handler =>
		{
			await button.OnLoadedAsync();
			await WaitForNativeImageAsync(handler);
			await AssertTintedAsync(handler, IconKind.Button, Colors.Blue);
			await WaitForPixelsAsync(handler.PlatformView!, Colors.Blue, iconColor);

			button.TextColor = Colors.Lime;

			await AssertTintedAsync(handler, IconKind.Button, Colors.Blue);
			await WaitForPixelsAsync(handler.PlatformView!, Colors.Blue, Colors.Lime, iconColor);
		});
	}

	[Theory]
	[InlineData(TextColorChange.FromNoneToLime)]
	[InlineData(TextColorChange.FromLimeToNone)]
	public async Task ButtonHasTheColorsOfATextColorThatChangedWhileItWasTinted(TextColorChange change)
	{
		SetupBuilder();

		var behavior = new IconTintColorBehavior { TintColor = Colors.Blue };
		var button = new Button
		{
			Text = "Icon",
			ImageSource = iconSource,
			WidthRequest = 120,
			HeightRequest = 50,
			TextColor = change is TextColorChange.FromLimeToNone ? Colors.Lime : null
		};

		await AttachAndRun(button, async handler =>
		{
			await button.OnLoadedAsync();
			await WaitForNativeImageAsync(handler);
			var uiButton = (UIButton)handler.PlatformView!;

			button.Behaviors.Add(behavior);
			await AssertTintedAsync(handler, IconKind.Button, Colors.Blue);

			button.TextColor = change is TextColorChange.FromNoneToLime ? Colors.Lime : null;

			await AssertTintedAsync(handler, IconKind.Button, Colors.Blue);
			Assert.DoesNotContain(DescribeColor(UIColor.Blue), DescribeTitleColors(uiButton), StringComparison.Ordinal);

			button.Behaviors.Remove(behavior);
			await AssertUntintedAsync(handler, IconKind.Button);
			var colorsAfterTheTint = DescribeNativeColors(uiButton);

			// The colors MAUI gives the button for its TextColor.
			handler.UpdateValue(nameof(Button.TextColor));

			Assert.Equal(DescribeNativeColors(uiButton), colorsAfterTheTint);
		});
	}

	[Fact]
	public async Task TitleColorTheButtonGotWhileItWasTintedIsKeptWhenTheTintIsRemoved()
	{
		SetupBuilder();

		var behavior = new IconTintColorBehavior { TintColor = Colors.Blue };
		var button = new Button { Text = "Icon", ImageSource = iconSource, WidthRequest = 120, HeightRequest = 50 };

		await AttachAndRun(button, async handler =>
		{
			await button.OnLoadedAsync();
			await WaitForNativeImageAsync(handler);
			var uiButton = (UIButton)handler.PlatformView!;

			button.Behaviors.Add(behavior);
			await AssertTintedAsync(handler, IconKind.Button, Colors.Blue);

			uiButton.SetTitleColor(UIColor.Green, UIControlState.Normal);

			button.Behaviors.Remove(behavior);
			await AssertUntintedAsync(handler, IconKind.Button);

			Assert.Equal(DescribeColor(UIColor.Green), DescribeColor(uiButton.TitleColor(UIControlState.Normal)));
		});
	}

	// UIKit dims the tint color of the views behind an alert, an action sheet or a popover (TintAdjustmentMode Dimmed): their tintColor, and
	// the title colors that follow it, read dimmed meanwhile.
	[Theory]
	[InlineData(IconKind.Image)]
	[InlineData(IconKind.ImageButton)]
	[InlineData(IconKind.Button)]
	public async Task TintAppliedWhileTheViewIsDimmedLeavesNoDimmedColorBehind(IconKind kind)
	{
		SetupBuilder();

		var behavior = new IconTintColorBehavior { TintColor = Colors.Blue };
		var view = CreateIconView(kind);

		if (view is Button button)
		{
			button.Text = "Icon";
		}

		await AttachAndRun(view, async handler =>
		{
			await view.OnLoadedAsync();
			await WaitForNativeImageAsync(handler);
			var platformView = handler.PlatformView!;
			var dimmedView = platformView.Superview ?? throw new InvalidOperationException("The platform view has no superview.");
			var colorsBefore = DescribeNativeColors(platformView);
			var titleColorsBefore = platformView is UIButton uiButton ? DescribeTitleColors(uiButton) : null;

			dimmedView.TintAdjustmentMode = UIViewTintAdjustmentMode.Dimmed;

			try
			{
				Assert.Equal(UIViewTintAdjustmentMode.Dimmed, platformView.TintAdjustmentMode);

				view.Behaviors.Add(behavior);

				Assert.Equal(UIImageRenderingMode.AlwaysTemplate, GetNativeImage(platformView)?.RenderingMode);
			}
			finally
			{
				dimmedView.TintAdjustmentMode = UIViewTintAdjustmentMode.Automatic;
			}

			await AssertTintedAsync(handler, kind, Colors.Blue);
			Assert.Equal(titleColorsBefore, platformView is UIButton tintedButton ? DescribeTitleColors(tintedButton) : null);

			view.Behaviors.Remove(behavior);
			await AssertUntintedAsync(handler, kind);

			Assert.Equal(colorsBefore, DescribeNativeColors(platformView));
		});
	}

	// The same with a real alert, presented while the behavior attaches to a Button (on Mac Catalyst the alert also gives the button the
	// inactive appearance, which DescribeColor leaves out).
	[Fact]
	public async Task ButtonTintedWhileAnAlertIsPresentedGetsItsOwnColorsBackOnceTheAlertIsDismissed()
	{
		SetupBuilder();

		var behavior = new IconTintColorBehavior { TintColor = Colors.Blue };
		var button = new Button { Text = "Icon", ImageSource = iconSource, WidthRequest = 120, HeightRequest = 50 };

		await AttachAndRun(button, async handler =>
		{
			await button.OnLoadedAsync();
			await WaitForNativeImageAsync(handler);
			var uiButton = (UIButton)handler.PlatformView!;
			var colorsBefore = DescribeNativeColors(uiButton);
			var titleColorsBefore = DescribeTitleColors(uiButton);

			var presenter = uiButton.Window?.RootViewController ?? throw new InvalidOperationException("The button is not in a window with a root view controller.");

			while (presenter.PresentedViewController is UIViewController presentedViewController)
			{
				presenter = presentedViewController;
			}

			var alert = UIAlertController.Create(nameof(IconTintColorBehavior), "An alert dims the views behind it.", UIAlertControllerStyle.Alert);
			alert.AddAction(UIAlertAction.Create("OK", UIAlertActionStyle.Default, null));

			await presenter.PresentViewControllerAsync(alert, false);

			try
			{
				await AssertEventually(() => uiButton.TintAdjustmentMode is UIViewTintAdjustmentMode.Dimmed, timeout: 3000, message: $"The alert did not dim the button: TintAdjustmentMode {uiButton.TintAdjustmentMode}.");

				button.Behaviors.Add(behavior);

				Assert.Equal(UIImageRenderingMode.AlwaysTemplate, uiButton.CurrentImage?.RenderingMode);
			}
			finally
			{
				await alert.DismissViewControllerAsync(false);
			}

			await AssertEventually(() => uiButton.TintAdjustmentMode is not UIViewTintAdjustmentMode.Dimmed, timeout: 3000, message: "The button stayed dimmed after the alert was dismissed.");
			await AssertTintedAsync(handler, IconKind.Button, Colors.Blue);
			Assert.Equal(titleColorsBefore, DescribeTitleColors(uiButton));

			button.Behaviors.Remove(behavior);
			await AssertUntintedAsync(handler, IconKind.Button);

			Assert.Equal(colorsBefore, DescribeNativeColors(uiButton));
		});
	}

	// The behavior reads the colors of a dimmed UIButton undimmed, which changes the button's TintAdjustmentMode for that time.
	[Theory]
	[InlineData(IconKind.ImageButton, true)]
	[InlineData(IconKind.ImageButton, false)]
	[InlineData(IconKind.Button, true)]
	[InlineData(IconKind.Button, false)]
	public async Task DimmedButtonKeepsItsTintAdjustmentModeWhenItIsTintedAndUntinted(IconKind kind, bool isDimmedByItsSuperview)
	{
		SetupBuilder();

		var behavior = new IconTintColorBehavior { TintColor = Colors.Blue };
		var view = CreateIconView(kind);

		await AttachAndRun(view, async handler =>
		{
			await view.OnLoadedAsync();
			await WaitForNativeImageAsync(handler);
			var platformView = handler.PlatformView!;
			var superview = platformView.Superview ?? throw new InvalidOperationException("The platform view has no superview.");
			var dimmedView = isDimmedByItsSuperview ? superview : platformView;

			dimmedView.TintAdjustmentMode = UIViewTintAdjustmentMode.Dimmed;

			try
			{
				view.Behaviors.Add(behavior);

				Assert.Equal(UIImageRenderingMode.AlwaysTemplate, GetNativeImage(platformView)?.RenderingMode);
				Assert.Equal(UIViewTintAdjustmentMode.Dimmed, platformView.TintAdjustmentMode);

				view.Behaviors.Remove(behavior);

				Assert.True(IsNativeUntinted(platformView), DescribeNativeTint(platformView));
				Assert.Equal(UIViewTintAdjustmentMode.Dimmed, platformView.TintAdjustmentMode);

				// A button that its superview dims follows it again: it is not dimmed of its own.
				superview.TintAdjustmentMode = UIViewTintAdjustmentMode.Automatic;

				Assert.Equal(isDimmedByItsSuperview ? UIViewTintAdjustmentMode.Normal : UIViewTintAdjustmentMode.Dimmed, platformView.TintAdjustmentMode);
			}
			finally
			{
				dimmedView.TintAdjustmentMode = UIViewTintAdjustmentMode.Automatic;
			}
		});
	}

	// MAUI never sets the tint color of an Image's UIImageView, which follows the tint color of its superview.
	[Fact]
	public async Task ImageViewFollowsTheTintColorOfItsSuperviewAgainAfterTheTintIsRemoved()
	{
		SetupBuilder();

		var behavior = new IconTintColorBehavior { TintColor = Colors.Blue };
		var image = CreateIconView(IconKind.Image);

		await AttachAndRun(image, async handler =>
		{
			await image.OnLoadedAsync();
			await WaitForNativeImageAsync(handler);
			var imageView = (UIImageView)handler.PlatformView!;
			var superview = imageView.Superview ?? throw new InvalidOperationException("The image view has no superview.");

			image.Behaviors.Add(behavior);
			await AssertTintedAsync(handler, IconKind.Image, Colors.Blue);

			image.Behaviors.Remove(behavior);
			await AssertUntintedAsync(handler, IconKind.Image);

			superview.TintColor = UIColor.Red;

			try
			{
				Assert.True(IsSameColor(imageView.TintColor, Colors.Red), $"The image view does not follow the tint color of its superview: {DescribeNativeTintColors(imageView)}");
			}
			finally
			{
				superview.TintColor = null;
			}
		});
	}

	// The managed UIImageView that the behavior added its key-value observation through can be collected while the behavior is attached, as
	// it is by the collections an app makes: the observations of the attaches that come with each load must not add up.
	[Fact]
	public async Task ButtonImageObservationIsRemovedWhenTheBehaviorDetaches()
	{
		SetupBuilder();

		var behavior = new CountingIconTintColorBehavior { TintColor = Colors.Blue };
		var button = (Button)CreateIconView(IconKind.Button);
		button.Behaviors.Add(behavior);
		var observationsWhileAttached = new List<int>();
		UIButton? uiButton = null;

		for (var load = 1; load <= 3; load++)
		{
			await AttachAndRun(button, async handler =>
			{
				await button.OnLoadedAsync();
				await WaitForNativeImageAsync(handler);
				await AssertTintedAsync(handler, IconKind.Button, Colors.Blue);
				uiButton = (UIButton)handler.PlatformView!;

				await AssertionExtensions.Collect();
				await AssertionExtensions.Collect();

				observationsWhileAttached.Add(CountImageObservations(uiButton));
			});

			await button.OnUnloadedAsync();
			await WaitForDetachAsync(behavior, load);
		}

		Assert.NotNull(uiButton);
		var observationsAfterTheDetach = await InvokeOnMainThreadAsync(() => CountImageObservations(uiButton));

		Assert.True(observationsWhileAttached.All(count => count is 1) && observationsAfterTheDetach is 0,
			$"Observations of the image of the button while the behavior was attached: {string.Join(", ", observationsWhileAttached)} (expected 1 each time), after it detached: {observationsAfterTheDetach} (expected 0).");
	}

	// A StreamImageSource is loaded off the UI thread: MAUI gives the UIButton the new image after the ImageSource change was handled, as an
	// AlwaysOriginal image.
	[Fact]
	public async Task TintIsKeptWhenTheButtonImageSourceChangesToAnImageThatLoadsAsynchronously()
	{
		SetupBuilder();

		var greenPng = await InvokeOnMainThreadAsync(CreateGreenPng);
		var behavior = new IconTintColorBehavior { TintColor = Colors.Blue };
		var button = (Button)CreateIconView(IconKind.Button);
		button.Behaviors.Add(behavior);

		await AttachAndRun(button, async handler =>
		{
			await button.OnLoadedAsync();
			await WaitForNativeImageAsync(handler);
			await AssertTintedAsync(handler, IconKind.Button, Colors.Blue);
			var imageBefore = GetNativeImageIdentity(handler.PlatformView!);

			button.ImageSource = ImageSource.FromStream(() => new MemoryStream(greenPng));

			await AssertHelpers.AssertEventually(() => !ReferenceEquals(imageBefore, GetNativeImageIdentity(handler.PlatformView!)), timeout: 3000, message: $"The button never showed its new image: {DescribeNativeTint(handler.PlatformView!)}");
			await AssertTintedAsync(handler, IconKind.Button, Colors.Blue);
			await WaitForPixelsAsync(handler.PlatformView!, Colors.Blue, Colors.Lime, iconColor);
		});

		// A lime square, in the sRGB range that the pixel assertions compare.
		static byte[] CreateGreenPng()
		{
			var format = UIGraphicsImageRendererFormat.DefaultFormat;
			format.PreferredRange = UIGraphicsImageRendererFormatRange.Standard;

			using var renderer = new UIGraphicsImageRenderer(new CGSize(20, 20), format);
			using var png = renderer.CreatePng(context =>
			{
				UIColor.FromRGB(0, 255, 0).SetFill();
				context.FillRect(new CGRect(0, 0, 20, 20));
			});

			return png.ToArray();
		}
	}

	// UIButton.CurrentImage is the image of setImage:forState:. A button that gets its image from its configuration shows an image in
	// its image view while CurrentImage is null, which is the second condition of SetUIButtonTintColor's guard. MAUI never creates such
	// a button; a custom handler can.
	static UIButton CreateButtonWithConfigurationImage()
	{
		using var renderer = new UIGraphicsImageRenderer(new CGSize(20, 20));
		var image = renderer.CreateImage(context =>
		{
			UIColor.Red.SetFill();
			context.FillRect(new CGRect(0, 0, 20, 20));
		});
		var configuration = UIButtonConfiguration.PlainButtonConfiguration;
		configuration.Image = image;

		var button = new UIButton(UIButtonType.System)
		{
			Configuration = configuration
		};

		return button;
	}

	[Fact]
	public async Task ButtonWhoseImageComesFromItsConfigurationIsNotLeftHalfTintedWhenTheBehaviorAttaches()
	{
		SetupBuilder();

		var probe = new IconTintColorBehaviorProbe { TintColor = Colors.Blue };
		var button = new Button();

		await InvokeOnMainThreadAsync(() =>
		{
			var uiButton = CreateButtonWithConfigurationImage();
			var shownImage = uiButton.ImageView.Image;
			Skip.If(uiButton.CurrentImage is not null, "This UIKit version reports the configuration image as the current image of the button.");
			Skip.If(shownImage is null, "This UIKit version does not show the configuration image in the image view of the button.");
			var renderingModeBefore = shownImage.RenderingMode;
			var tintColorsBefore = DescribeNativeTintColors(uiButton);

			var exception = Record.Exception(() => probe.AttachTo(button, uiButton));

			Assert.Null(exception);

			// Either nothing changed (the behavior found no image to tint) or the shown image is fully tinted.
			var untouched = uiButton.ImageView.Image?.RenderingMode == renderingModeBefore && DescribeNativeTintColors(uiButton) == tintColorsBefore;
			var tinted = uiButton.ImageView.Image?.RenderingMode == UIImageRenderingMode.AlwaysTemplate && IsSameColor(uiButton.TintColor, Colors.Blue) && IsSameColor(uiButton.ImageView.TintColor, Colors.Blue);
			Assert.True(untouched || tinted, $"The button is half tinted: image {uiButton.ImageView.Image?.RenderingMode}, {DescribeNativeTintColors(uiButton)} (before: image {renderingModeBefore}, {tintColorsBefore}).");
		});
	}

	[Fact]
	public async Task ButtonWhoseImageComesFromItsConfigurationDoesNotThrowWhenTheBehaviorDetaches()
	{
		SetupBuilder();

		var probe = new IconTintColorBehaviorProbe { TintColor = Colors.Blue };
		var button = new Button();

		await InvokeOnMainThreadAsync(() =>
		{
			var uiButton = CreateButtonWithConfigurationImage();
			Skip.If(uiButton.CurrentImage is not null, "This UIKit version reports the configuration image as the current image of the button.");
			Skip.If(uiButton.ImageView.Image is null, "This UIKit version does not show the configuration image in the image view of the button.");
			probe.AttachTo(button, uiButton);

			var exception = Record.Exception(() => probe.DetachFrom(button, uiButton));

			Assert.Null(exception);
		});
	}

	[Fact]
	public async Task ViewPropertyChangeOfAViewThatIsNotAnImageDoesNotApplyTheTint()
	{
		SetupBuilder();

		var behavior = new IconTintColorBehaviorProbe { TintColor = Colors.Blue };
		var image = CreateIconView(IconKind.Image);
		var button = CreateIconView(IconKind.Button);
		var layout = new VerticalStackLayout { image, button };
		image.Behaviors.Add(behavior);

		await AttachAndRun(layout, async _ =>
		{
			await layout.OnLoadedAsync();
			var imageHandler = (IPlatformViewHandler)image.Handler!;
			var buttonHandler = (IPlatformViewHandler)button.Handler!;
			await WaitForNativeImageAsync(imageHandler);
			await WaitForNativeImageAsync(buttonHandler);
			await AssertTintedAsync(imageHandler, IconKind.Image, Colors.Blue);
			Assert.True(IsNativeUntinted(buttonHandler.PlatformView!), DescribeNativeTint(buttonHandler.PlatformView!));

			// A Button has a UIButton platform view, but only Image and ImageButton are tinted when their source changes.
			behavior.RaiseViewPropertyChanged(button, nameof(Image.Source));

			Assert.True(IsNativeUntinted(buttonHandler.PlatformView!), $"The tint was applied to a Button: {DescribeNativeTint(buttonHandler.PlatformView!)}");
		});
	}

	[Fact]
	public async Task ViewPropertyChangeOfAViewWithoutAHandlerIsIgnored()
	{
		SetupBuilder();

		var behavior = new IconTintColorBehaviorProbe { TintColor = Colors.Blue };
		var image = new Image { Source = iconSource };

		await InvokeOnMainThreadAsync(() =>
		{
			var exception = Record.Exception(() => behavior.RaiseViewPropertyChanged(image, nameof(Image.Source)));

			Assert.Null(exception);
			Assert.Null(image.Handler);
		});
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task UnsupportedPlatformViewThrowsNotSupportedExceptionForAnyTintColor(bool hasTintColor)
	{
		SetupBuilder();

		var label = new Label { Text = "Not an icon" };
		var handler = (IPlatformViewHandler)await CreateHandlerAsync(label);
		var probe = new IconTintColorBehaviorProbe { TintColor = hasTintColor ? Colors.Blue : null };

		await InvokeOnMainThreadAsync(() =>
		{
			// A null tint color means "clear the tint", and clearing only supports UIImageView and UIButton too.
			var exception = Assert.Throws<NotSupportedException>(() => probe.AttachTo(label, handler.PlatformView!));

			Assert.Contains(nameof(IconTintColorBehavior), exception.Message, StringComparison.Ordinal);
		});
	}

	[Fact]
	public async Task UnsupportedPlatformViewThrowsNotSupportedExceptionWhenTheBehaviorDetaches()
	{
		SetupBuilder();

		var label = new Label { Text = "Not an icon" };
		var handler = (IPlatformViewHandler)await CreateHandlerAsync(label);
		var probe = new IconTintColorBehaviorProbe { TintColor = Colors.Blue };

		await InvokeOnMainThreadAsync(() =>
		{
			var exception = Assert.Throws<NotSupportedException>(() => probe.DetachFrom(label, handler.PlatformView!));

			Assert.Contains(nameof(IconTintColorBehavior), exception.Message, StringComparison.Ordinal);
		});
	}

	sealed partial class IconTintColorBehaviorProbe
	{
		public void RaiseViewPropertyChanged(View sender, string propertyName) => OnViewPropertyChanged(sender, new PropertyChangedEventArgs(propertyName));
	}
}