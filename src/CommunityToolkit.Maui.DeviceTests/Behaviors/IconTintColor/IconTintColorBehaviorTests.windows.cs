using System.ComponentModel;
using System.Reflection;
using CommunityToolkit.Maui.Behaviors;
using Microsoft.Maui.Platform;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media.Imaging;
using static CommunityToolkit.Maui.DeviceTests.AssertHelpers;
using LoadedImageSurface = Microsoft.UI.Xaml.Media.LoadedImageSurface;
using WButton = Microsoft.UI.Xaml.Controls.Button;
using WImage = Microsoft.UI.Xaml.Controls.Image;
using WImageSource = Microsoft.UI.Xaml.Media.ImageSource;
using WStackPanel = Microsoft.UI.Xaml.Controls.StackPanel;
using WTextBlock = Microsoft.UI.Xaml.Controls.TextBlock;
using PlatformView = Microsoft.UI.Xaml.FrameworkElement;

namespace CommunityToolkit.Maui.DeviceTests;

// Windows: the behavior hides the image behind a SpriteVisual (a mask brush that holds the tint color) that it adds as the child
// visual of the Image (or of the Image inside the Button), and replaces Image.Source with a blank WriteableBitmap until it detaches.
// RenderTargetBitmap does not capture composition visuals, so the tint is verified through the visual, not through pixels.
public partial class IconTintColorBehaviorTests
{
	public enum UnusableElementChange
	{
		NoPropertyName,
		SenderIsNotAView,
		ViewWithoutAHandler
	}

	static bool CanAssertTintPixels => false;

	static bool CanAssertButtonTintPixels => false;

	// Not verified on a device: the WinUI element is only reachable through COM wrappers, so the toolkit-owned managed objects are what the leak tests require.
	static bool CanAssertPlatformViewIsCollected => false;

	static WImage? GetNativeImage(PlatformView platformView) => platformView switch
	{
		WImage image => image,
		WButton button => button.Content switch
		{
			WImage buttonImage => buttonImage,
			Microsoft.UI.Xaml.Controls.Panel panel => panel.Children.OfType<WImage>().FirstOrDefault(),
			_ => null
		},
		_ => null
	};

	static SpriteVisual? GetTintVisual(PlatformView platformView) =>
		GetNativeImage(platformView) is WImage image ? ElementCompositionPreview.GetElementChildVisual(image) as SpriteVisual : null;

	// The object that stands for the image the platform view shows: a new image source gives a new WinUI image source.
	static object? GetNativeImageIdentity(PlatformView platformView) => GetNativeImage(platformView)?.Source;

	static bool HasNativeImage(PlatformView platformView) => GetNativeImage(platformView)?.Source is not null;

	static bool IsNativeTinted(PlatformView platformView, Color color) =>
		GetTintVisual(platformView)?.Brush is CompositionMaskBrush { Source: CompositionColorBrush colorBrush }
		&& colorBrush.Color == color.ToWindowsColor()
		&& GetNativeImage(platformView)?.Source is WriteableBitmap;

	static bool IsNativeUntinted(PlatformView platformView) =>
		GetTintVisual(platformView) is null
		&& GetNativeImage(platformView)?.Source is not WriteableBitmap;

	// Puts the native view into the state of a view that was never tinted, without going through the behavior.
	static void ResetNativeTint(PlatformView platformView)
	{
		var image = GetNativeImage(platformView) ?? throw new InvalidOperationException($"{platformView.GetType()} has no image to reset.");
		ElementCompositionPreview.SetElementChildVisual(image, null);
	}

	static string DescribeNativeTint(PlatformView platformView)
	{
		var image = GetNativeImage(platformView);
		var visual = GetTintVisual(platformView);
		var tint = visual?.Brush is CompositionMaskBrush { Source: CompositionColorBrush colorBrush } ? colorBrush.Color.ToString() : "none";

		return $"{platformView.GetType().Name}: image source {image?.Source?.GetType().Name ?? "none"}, tint visual {(visual is null ? "none" : "present")}, tint color {tint}";
	}

	// The brush that holds the tint color (the source of the visual's mask brush).
	static CompositionColorBrush? GetTintColorBrush(PlatformView platformView) =>
		GetTintVisual(platformView)?.Brush is CompositionMaskBrush { Source: CompositionColorBrush colorBrush } ? colorBrush : null;

	// The tint shows the icon only once the mask brush has loaded the image of the icon: an empty mask shows nothing at all.
	static bool HasLoadedMask(PlatformView platformView) =>
		GetTintVisual(platformView)?.Brush is CompositionMaskBrush { Mask: CompositionSurfaceBrush { Surface: LoadedImageSurface surface } }
		&& surface.NaturalSize is { Width: > 0, Height: > 0 };

	// The bytes of behavior_disc.png (Resources/Images). A MauiImage is not a file of the app package on Windows (MRT gives it scale
	// qualifiers), and these tests load the image from a stream, as MAUI does for a StreamImageSource or a rooted file path.
	static byte[] DiscPngBytes => Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAEAAAABACAYAAACqaXHeAAAAtUlEQVR42u3byxWAMAgEwPTftFqCnwQhzD4tYOekgYwhIhKU43ruvO0KbwUyq3Q5jNXFU0NEl0+D8FfxFBBZyv+CkK18KELW8iEI2csvRahSfglCtfLTEVoDVC0/BaF6+c8IrQF2Kf8aAUBngN3KP0YAAKAxwK7lbyMAAAAAAAAAAAD4GvQvAACAUyFnggD6ApgMATAcNR4HYEXGkpQ1OYuSVmUtS1uXd2HClRmXpkJBhohIUE4OPj3OYEXn9wAAAABJRU5ErkJggg==");

	// What MAUI's StreamImageSourceService (and FileImageSourceService for a rooted path) gives the WinUI Image: a BitmapImage loaded from a
	// stream, which has no UriSource.
	static async Task<BitmapImage> LoadDiscFromAStreamAsync()
	{
		var bitmap = new BitmapImage();
		using var stream = new MemoryStream(DiscPngBytes);

		await bitmap.SetSourceAsync(stream.AsRandomAccessStream());

		Assert.Null(bitmap.UriSource);
		return bitmap;
	}

	static Task WaitForLoadedAndMeasuredAsync(WImage image) =>
		AssertEventually(() => image.IsLoaded && image.ActualWidth > 0 && image.ActualHeight > 0, timeout: 3000, message: $"The WinUI Image was never loaded and measured: IsLoaded {image.IsLoaded}, size {image.ActualWidth}x{image.ActualHeight}.");

	// Sets the source of a hosted WinUI Image and returns once it has opened. The handlers that were subscribed before (the behavior's) run
	// before this method returns.
	static async Task SetSourceAndWaitUntilItOpensAsync(WImage image, WImageSource source)
	{
		var imageOpened = new TaskCompletionSource();

		void OnImageOpened(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => imageOpened.TrySetResult();

		image.ImageOpened += OnImageOpened;

		try
		{
			image.Source = source;

			await imageOpened.Task.WaitAsync(TimeSpan.FromSeconds(3));
		}
		finally
		{
			image.ImageOpened -= OnImageOpened;
		}
	}

	// OnElementPropertyChanged only reacts to the Source property: any other property of the view leaves the tint (and its visual) alone.
	[Fact]
	public async Task TintIsNotAppliedAgainWhenAnUnrelatedImagePropertyChanges()
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
			var tintVisual = GetTintVisual(handler.PlatformView!);

			((Image)image).Aspect = Aspect.Fill;
			image.Opacity = 0.5;

			// Applying the tint again would first remove the visual, which happens at once.
			Assert.Same(tintVisual, GetTintVisual(handler.PlatformView!));
			Assert.True(IsNativeTinted(handler.PlatformView!, Colors.Blue), DescribeNativeTint(handler.PlatformView!));
		});
	}

	// OnIconTintColorBehaviorPropertyChanged only reacts to TintColor: another bindable property of the behavior leaves the tint alone.
	// The brush color is changed behind the behavior's back first: handling the change as a TintColor change would only write TintColor
	// into the same brush again (the visual would stay), so the brush color is what tells the two apart.
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
			var tintVisual = GetTintVisual(handler.PlatformView!);
			var colorBrush = GetTintColorBrush(handler.PlatformView!);
			Assert.NotNull(colorBrush);
			colorBrush.Color = Colors.Red.ToWindowsColor();

			behavior.BindingContext = new object();

			// Applying the tint again would first remove the visual, which happens at once.
			Assert.Same(tintVisual, GetTintVisual(handler.PlatformView!));
			Assert.Equal(Colors.Red.ToWindowsColor(), colorBrush.Color);
		});
	}

	// An image that has no size yet when the behavior applies the tint is tinted once SizeChanged reports both a width and a height:
	// a SizeChanged with one of them still 0 is ignored. The WinUI Image is hosted alone, so its size is set by hand.
	[Fact]
	public async Task ImageThatIsNotMeasuredYetIsTintedOnceItHasAWidthAndAHeight()
	{
		SetupBuilder();

		var probe = new IconTintColorBehaviorProbe { TintColor = Colors.Blue };
		var image = new Image { Source = ImageSource.FromUri(new Uri("ms-appx:///behavior_disc.png")) };

		await InvokeOnMainThreadAsync(async () =>
		{
			var wImage = new WImage { Width = 0, Height = 0 };

			await wImage.AttachAndRun(async () =>
			{
				await AssertEventually(() => wImage.IsLoaded, timeout: 3000, message: "The WinUI Image never loaded.");

				probe.AttachTo(image, wImage);

				Assert.Null(ElementCompositionPreview.GetElementChildVisual(wImage));

				wImage.Width = 40;
				wImage.UpdateLayout();

				// Only the width is set, and WinUI lays out an Image whose Height is 0 with no size at all (ActualWidth stays 0, DesiredSize 0x0): it
				// is still not measured, so still no tint.
				Assert.Null(ElementCompositionPreview.GetElementChildVisual(wImage));

				wImage.Height = 40;

				await AssertEventually(() => IsNativeTinted(wImage, Colors.Blue), timeout: 3000, message: $"The image was not tinted once it had a size: {DescribeNativeTint(wImage)}");
			}, MauiContext);
		});
	}

	// RemoveTintColor has no other platform view to restore than Image and Button: a tint that was applied to an Image cannot be removed
	// from anything else.
	[Fact]
	public async Task UnsupportedPlatformViewThrowsNotSupportedExceptionWhenTheBehaviorDetachesWhileTinted()
	{
		SetupBuilder();

		var probe = new IconTintColorBehaviorProbe { TintColor = Colors.Blue };
		var image = CreateIconView(IconKind.Image);
		image.Behaviors.Add(probe);

		await AttachAndRun(image, async handler =>
		{
			await image.OnLoadedAsync();
			await WaitForNativeImageAsync(handler);
			await AssertTintedAsync(handler, IconKind.Image, Colors.Blue);

			var exception = Assert.Throws<NotSupportedException>(() => probe.DetachFrom(image, new Microsoft.UI.Xaml.Controls.TextBlock()));

			Assert.Contains(nameof(IconTintColorBehavior), exception.Message, StringComparison.Ordinal);
		});
	}

	// A UriImageSource does not need the network: an ms-appx URI points into the app package. LoadAndApplyImageTintColor loads such a
	// source itself (a BitmapImage, or an SvgImageSource for an .svg URI) and puts that source back when the tint is removed. The .svg
	// row uses Resources/Raw/behavior_disc.svg, a MauiAsset packaged as is (MauiImage would convert an SVG to PNG).
	[Theory]
	[InlineData("ms-appx:///behavior_disc.png", false)]
	[InlineData("ms-appx:///behavior_disc.svg", true)]
	public async Task TintIsAppliedToAnImageWithAPackagedUriImageSource(string uri, bool isSvg)
	{
		SetupBuilder();

		var behavior = new IconTintColorBehavior { TintColor = Colors.Blue };
		var image = new Image { Source = ImageSource.FromUri(new Uri(uri)), WidthRequest = 40, HeightRequest = 40 };
		image.Behaviors.Add(behavior);

		await AttachAndRun(image, async handler =>
		{
			await image.OnLoadedAsync();
			await AssertTintedAsync(handler, IconKind.Image, Colors.Blue);

			image.Behaviors.Remove(behavior);

			var wImage = (WImage)handler.PlatformView!;
			var restoredUri = isSvg ? Assert.IsType<SvgImageSource>(wImage.Source).UriSource : Assert.IsType<BitmapImage>(wImage.Source).UriSource;
			Assert.Equal(new Uri(uri), restoredUri);
			Assert.True(IsNativeUntinted(wImage), DescribeNativeTint(wImage));
		});
	}

	// MAUI's Button always holds an Image, so the WinUI buttons that TryGetButtonImage cannot find an Image in are hand-made: a text
	// content and a panel without an Image. ApplyTintColor returns without a tint for both.
	[Fact]
	public async Task ButtonWithoutAnImageIsIgnoredWhenTheBehaviorAttaches()
	{
		SetupBuilder();

		var probe = new IconTintColorBehaviorProbe { TintColor = Colors.Blue };

		await InvokeOnMainThreadAsync(() =>
		{
			var textButton = new WButton { Content = "No icon" };
			var panelButton = new WButton { Content = new Microsoft.UI.Xaml.Controls.StackPanel() };

			Assert.Null(Record.Exception(() => probe.AttachTo(new Button(), textButton)));
			Assert.Null(Record.Exception(() => probe.AttachTo(new Button(), panelButton)));
			Assert.Null(ElementCompositionPreview.GetElementChildVisual(textButton));
			Assert.Null(ElementCompositionPreview.GetElementChildVisual(panelButton));
		});
	}

	// RemoveTintColor with a tint applied, handed a WinUI Button that holds no Image: nothing to restore, and the tint state is cleared.
	[Fact]
	public async Task DetachingFromAButtonWithoutAnImageWhileTintedDoesNotThrow()
	{
		SetupBuilder();

		var probe = new IconTintColorBehaviorProbe { TintColor = Colors.Blue };
		var image = CreateIconView(IconKind.Image);
		image.Behaviors.Add(probe);

		await AttachAndRun(image, async handler =>
		{
			await image.OnLoadedAsync();
			await WaitForNativeImageAsync(handler);
			await AssertTintedAsync(handler, IconKind.Image, Colors.Blue);

			Assert.Null(Record.Exception(() => probe.DetachFrom(image, new WButton { Content = "No icon" })));
		});
	}

	// An Image that is not loaded yet (not in the visual tree) is tinted only once it raises ImageOpened: attaching creates no tint, and
	// neither does being hosted (Loaded, SizeChanged) before it has an image. The image MAUI would set for the FileImageSource is then set
	// by hand: the tint appears once it opened, and detaching puts that image back.
	[Fact]
	public async Task ImageThatIsNotLoadedIsNotTintedBeforeItOpens()
	{
		SetupBuilder();

		var probe = new IconTintColorBehaviorProbe { TintColor = Colors.Blue };
		var image = new Image { Source = iconSource };

		await InvokeOnMainThreadAsync(async () =>
		{
			var wImage = new WImage { Width = 40, Height = 40 };

			Assert.Null(Record.Exception(() => probe.AttachTo(image, wImage)));
			Assert.Null(ElementCompositionPreview.GetElementChildVisual(wImage));
			Assert.Null(wImage.Source);

			await wImage.AttachAndRun(async () =>
			{
				await WaitForLoadedAndMeasuredAsync(wImage);

				Assert.Null(ElementCompositionPreview.GetElementChildVisual(wImage));

				var source = new BitmapImage(new Uri("ms-appx:///behavior_disc.png"));
				await SetSourceAndWaitUntilItOpensAsync(wImage, source);

				await AssertEventually(() => IsNativeTinted(wImage, Colors.Blue), timeout: 3000, message: $"The image was not tinted once it opened: {DescribeNativeTint(wImage)}");

				probe.DetachFrom(image, wImage);

				Assert.True(IsNativeUntinted(wImage), DescribeNativeTint(wImage));
				Assert.Same(source, wImage.Source);
			}, MauiContext);
		});
	}

	// LoadAndApplyImageTintColor waits for ImageOpened when the WinUI Image still shows another image than the view's FileImageSource
	// (MAUI loads the new image after the Source change). A behavior that detaches before that image opens must leave it alone: nothing
	// would ever remove a tint applied after the detach.
	[Fact]
	public async Task ImageThatOpensAfterTheBehaviorDetachedIsNotTinted()
	{
		SetupBuilder();

		var probe = new IconTintColorBehaviorProbe { TintColor = Colors.Blue };
		var image = new Image { Source = greenSource };

		await InvokeOnMainThreadAsync(async () =>
		{
			var wImage = new WImage { Width = 40, Height = 40 };

			await wImage.AttachAndRun(async () =>
			{
				await WaitForLoadedAndMeasuredAsync(wImage);
				await SetSourceAndWaitUntilItOpensAsync(wImage, new BitmapImage(new Uri("ms-appx:///behavior_disc.png")));

				// The WinUI Image still shows the previous image: the behavior waits for the green one to open.
				probe.AttachTo(image, wImage);
				Assert.True(IsNativeUntinted(wImage), DescribeNativeTint(wImage));

				probe.DetachFrom(image, wImage);

				await SetSourceAndWaitUntilItOpensAsync(wImage, new BitmapImage(new Uri("ms-appx:///behavior_green.png")));

				await VisualBehaviorHelpers.AssertStaysFalseAsync(() => !IsNativeUntinted(wImage), $"The image was tinted after the behavior detached: {DescribeNativeTint(wImage)}");
			}, MauiContext);
		});
	}

	// The WinUI Image has no source yet (MAUI sets it once the image source has loaded): attaching adds no tint and leaves the image alone,
	// and the behavior waits for the image to open. That the image that arrives afterwards is tinted is the subject of
	// TintIsAppliedWhenTheImageSourceIsSetAfterTheBehaviorAttached.
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ImageWithoutASourceIsNotTintedWhenTheBehaviorAttaches(bool hasFileImageSource)
	{
		SetupBuilder();

		var probe = new IconTintColorBehaviorProbe { TintColor = Colors.Blue };
		var image = hasFileImageSource ? new Image { Source = iconSource } : new Image();

		await InvokeOnMainThreadAsync(async () =>
		{
			var wImage = new WImage { Width = 40, Height = 40 };

			await wImage.AttachAndRun(async () =>
			{
				await WaitForLoadedAndMeasuredAsync(wImage);

				var exception = Record.Exception(() => probe.AttachTo(image, wImage));

				Assert.Null(exception);
				Assert.True(IsNativeUntinted(wImage), DescribeNativeTint(wImage));
				Assert.Null(wImage.Source);
			}, MauiContext);
		});
	}

	// MAUI's Button content is a panel that holds an Image (collapsed while the Button has no ImageSource): TryGetButtonImage takes the
	// first Image of the panel, which is tinted and restored. Here an ImageButton is the element, with a hand-made WinUI Button whose
	// panel has a TextBlock before the Image.
	[Fact]
	public async Task ButtonWhoseContentIsAPanelWithAnImageIsTintedAndRestored()
	{
		SetupBuilder();

		var probe = new IconTintColorBehaviorProbe { TintColor = Colors.Blue };
		var imageButton = new ImageButton { Source = iconSource };

		await InvokeOnMainThreadAsync(async () =>
		{
			var source = new BitmapImage(new Uri("ms-appx:///behavior_disc.png"));
			var buttonImage = new WImage { Width = 40, Height = 40, Source = source };
			var button = new WButton { Content = new WStackPanel { Children = { new WTextBlock { Text = "Icon" }, buttonImage } } };

			await button.AttachAndRun(async () =>
			{
				await WaitForLoadedAndMeasuredAsync(buttonImage);

				probe.AttachTo(imageButton, button);

				await AssertEventually(() => IsNativeTinted(button, Colors.Blue), timeout: 3000, message: $"The image of the panel was not tinted: {DescribeNativeTint(button)}");
				Assert.Null(ElementCompositionPreview.GetElementChildVisual(button));

				probe.DetachFrom(imageButton, button);

				Assert.True(IsNativeUntinted(button), DescribeNativeTint(button));
				Assert.Same(source, buttonImage.Source);
			}, MauiContext);
		});
	}

	// MAUI loads a FileImageSource with a rooted path from a stream (FileImageSourceService.GetLocal), so the WinUI Image shows a
	// BitmapImage without a UriSource, which cannot be compared with the file. The behavior has to cope with the missing UriSource and tint
	// the image (added to such an Image, it attaches in the Loaded callback).
	[Fact]
	public async Task TintIsAppliedToAnImageWhoseRootedFileIsLoadedFromAStream()
	{
		SetupBuilder();

		var path = Path.Combine(FileSystem.CacheDirectory, $"behavior_disc_{Guid.NewGuid():N}.png");
		await File.WriteAllBytesAsync(path, DiscPngBytes);

		try
		{
			var image = new Image { Source = ImageSource.FromFile(path) };

			await AssertStreamLoadedImageIsTintedAsync(image);
		}
		finally
		{
			File.Delete(path);
		}
	}

	// MAUI loads a StreamImageSource into a BitmapImage from the stream (StreamImageSourceService), which has no UriSource: the mask of the
	// tint cannot come from a uri.
	[Fact]
	public async Task TintIsAppliedToAnImageWithAStreamImageSource()
	{
		SetupBuilder();

		var image = new Image { Source = ImageSource.FromStream(() => new MemoryStream(DiscPngBytes)) };

		await AssertStreamLoadedImageIsTintedAsync(image);
	}

	// ImageSource.FromStream(() => stream) gives the same stream every time, and MAUI disposes it once it has loaded the image from it: the
	// mask of the tint cannot be loaded from it. The image is then shown untinted, instead of behind a tint without a mask, which shows nothing.
	[Fact]
	public async Task ImageWithAStreamImageSourceThatCannotBeReadAgainIsShownUntinted()
	{
		SetupBuilder();

		var stream = new MemoryStream(DiscPngBytes);
		var image = new Image { Source = ImageSource.FromStream(() => stream) };

		// What MAUI's StreamImageSourceService does with the stream once it has loaded the image.
		stream.Dispose();

		await InvokeOnMainThreadAsync(async () =>
		{
			var probe = new IconTintColorBehaviorProbe { TintColor = Colors.Blue };
			var source = await LoadDiscFromAStreamAsync();
			var wImage = new WImage { Width = 40, Height = 40, Source = source };

			await wImage.AttachAndRun(async () =>
			{
				await WaitForLoadedAndMeasuredAsync(wImage);

				probe.AttachTo(image, wImage);

				// The tint is applied before the mask is loaded.
				Assert.True(IsNativeTinted(wImage, Colors.Blue), DescribeNativeTint(wImage));

				await AssertEventually(() => IsNativeUntinted(wImage), timeout: 3000, message: $"The image is still hidden by a tint without a mask: {DescribeNativeTint(wImage)}");
				Assert.Same(source, wImage.Source);

				probe.DetachFrom(image, wImage);

				Assert.True(IsNativeUntinted(wImage), DescribeNativeTint(wImage));
				Assert.Same(source, wImage.Source);
			}, MauiContext);
		});
	}

	// Hosts a WinUI Image that shows the disc loaded from a stream, as MAUI shows a stream or a rooted file, and attaches the behavior to it
	// for the given view: no exception, a tint whose mask has loaded the icon, and the stream image back once the behavior detached.
	Task AssertStreamLoadedImageIsTintedAsync(Image image) => InvokeOnMainThreadAsync(async () =>
	{
		var probe = new IconTintColorBehaviorProbe { TintColor = Colors.Blue };
		var source = await LoadDiscFromAStreamAsync();
		var wImage = new WImage { Width = 40, Height = 40, Source = source };

		await wImage.AttachAndRun(async () =>
		{
			await WaitForLoadedAndMeasuredAsync(wImage);

			var exception = Record.Exception(() => probe.AttachTo(image, wImage));

			Assert.Null(exception);
			await AssertEventually(() => IsNativeTinted(wImage, Colors.Blue) && HasLoadedMask(wImage), timeout: 3000, message: $"The image was not tinted with a mask of the icon: {DescribeNativeTint(wImage)}, mask loaded {HasLoadedMask(wImage)}");

			probe.DetachFrom(image, wImage);

			Assert.True(IsNativeUntinted(wImage), DescribeNativeTint(wImage));
			Assert.Same(source, wImage.Source);
		}, MauiContext);
	});

	// The private handlers have guards that MAUI never reaches: the behavior is the sender of its own PropertyChanged event and the view
	// raises PropertyChanged with a name, and both unsubscribe when the behavior detaches. They are called directly to reach the guards.
	[Fact]
	public async Task BehaviorPropertyChangeOfABehaviorThatIsNotAttachedIsIgnored()
	{
		SetupBuilder();

		var behavior = new IconTintColorBehavior { TintColor = Colors.Blue };

		await InvokeOnMainThreadAsync(() =>
		{
			var exception = Record.Exception(() => InvokePrivate(behavior, "OnIconTintColorBehaviorPropertyChanged", behavior, new PropertyChangedEventArgs(nameof(IconTintColorBehavior.TintColor))));

			Assert.Null(exception);
		});
	}

	[Fact]
	public async Task BehaviorPropertyChangeWithoutASenderThrowsArgumentNullException()
	{
		SetupBuilder();

		var behavior = new IconTintColorBehavior { TintColor = Colors.Blue };

		await InvokeOnMainThreadAsync(() =>
		{
			var exception = Assert.Throws<TargetInvocationException>(() => InvokePrivate(behavior, "OnIconTintColorBehaviorPropertyChanged", null, new PropertyChangedEventArgs(nameof(IconTintColorBehavior.TintColor))));

			Assert.IsType<ArgumentNullException>(exception.InnerException);
		});
	}

	[Theory]
	[InlineData(UnusableElementChange.NoPropertyName)]
	[InlineData(UnusableElementChange.SenderIsNotAView)]
	[InlineData(UnusableElementChange.ViewWithoutAHandler)]
	public async Task ElementPropertyChangeThatCannotBeTintedIsIgnored(UnusableElementChange change)
	{
		SetupBuilder();

		var behavior = new IconTintColorBehavior { TintColor = Colors.Blue };

		await InvokeOnMainThreadAsync(() =>
		{
			var (sender, propertyName) = change switch
			{
				UnusableElementChange.NoPropertyName => ((object)new Image { Source = iconSource }, (string?)null),
				UnusableElementChange.SenderIsNotAView => (new object(), nameof(Image.Source)),
				_ => (new Image { Source = iconSource }, nameof(Image.Source))
			};

			var exception = Record.Exception(() => InvokePrivate(behavior, "OnElementPropertyChanged", sender, new PropertyChangedEventArgs(propertyName)));

			Assert.Null(exception);
		});
	}

	// ApplyImageTintColor is only called for a measured image; called for one that is not measured, it adds no tint.
	[Fact]
	public async Task ImageThatIsNotMeasuredIsNotTinted()
	{
		SetupBuilder();

		var behavior = new IconTintColorBehavior { TintColor = Colors.Blue };
		var image = new Image { Source = ImageSource.FromUri(new Uri("ms-appx:///behavior_disc.png")) };

		await InvokeOnMainThreadAsync(() =>
		{
			var wImage = new WImage();

			var exception = Record.Exception(() => InvokePrivate(behavior, "ApplyImageTintColor", image, wImage, Colors.Blue));

			Assert.Null(exception);
			Assert.Null(ElementCompositionPreview.GetElementChildVisual(wImage));
			Assert.Null(wImage.Source);
		});
	}

	// RestoreOriginalImage is only called while a tint is applied; without one it leaves the image alone.
	[Fact]
	public async Task RestoringTheOriginalImageWithoutATintLeavesTheImageAlone()
	{
		SetupBuilder();

		var behavior = new IconTintColorBehavior { TintColor = Colors.Blue };

		await InvokeOnMainThreadAsync(() =>
		{
			var source = new BitmapImage(new Uri("ms-appx:///behavior_disc.png"));
			var wImage = new WImage { Source = source };

			var exception = Record.Exception(() => InvokePrivate(behavior, "RestoreOriginalImage", wImage));

			Assert.Null(exception);
			Assert.Same(source, wImage.Source);
		});
	}

	static object? InvokePrivate(IconTintColorBehavior behavior, string methodName, params object?[] arguments)
	{
		var method = typeof(IconTintColorBehavior).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
		Assert.True(method is not null, $"IconTintColorBehavior.{methodName} no longer exists: update the tests that call it.");

		return method.Invoke(behavior, arguments);
	}
}