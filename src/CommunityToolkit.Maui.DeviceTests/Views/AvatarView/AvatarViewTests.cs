using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Handlers;
using Xunit.Sdk;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.AvatarView)]
[Collection(TestCollections.DisableParallelization)]
public partial class AvatarViewTests : CommunityToolkitHandlerTestBase
{
	// A 16x16 solid red PNG, embedded so that the tests need neither the network nor an asset file.
	const string redPngBase64 = "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAIAAACQkWg2AAAAFklEQVR42mP4z8BAEmIY1TCqYfhqAACQ+f8B8u7oVwAAAABJRU5ErkJggg==";

	const double avatarSize = 100;

	// A size that differs from the AvatarView default (48).
	const double styledSize = 64;

	// Android lays views out in whole pixels, so sizes differ from the requested ones by up to a pixel (0.4 dip at density 2.6).
	const double sizeTolerance = 1;

	// Image decoding and layout passes are asynchronous: a generous polling timeout keeps slow CI machines green without hiding a wrong state
	// (the assertion still fails once it is over).
	const int layoutTimeout = 5000;

	// A size that no avatar computes: only a refresh of the image replaces it.
	const double perturbedWidth = 1;
	const double perturbedHeight = 2;

	public enum NativeContent
	{
		None,
		Label,
		Image,
	}

	public enum AreaChange
	{
		Padding,
		// BorderWidth also sets StrokeThickness (two triggers in a row).
		BorderWidth,
		// StrokeThickness already has the new border width: the BorderWidth change is the only trigger.
		BorderWidthAlone,
		StrokeThickness,
		CornerRadius,
	}

	public enum FrameSide
	{
		Width,
		Height,
	}

	public enum ClipShape
	{
		Polyline,
		Ellipse,
		Path,
		Polygon,
		Rectangle,
		RoundRectangle,
	}

	public enum StyleKind
	{
		Explicit,
		Implicit,
	}

	// The BindingContext of an avatar whose StreamImageSource binds its Stream.
	sealed class ImageStreamViewModel : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler? PropertyChanged;

		public Func<CancellationToken, Task<Stream>>? Stream
		{
			get;
			set
			{
				field = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Stream)));
			}
		}
	}

	// The size of an AvatarView (48x48 unless it is set) is a default that a Style replaces, as AvatarViewBordersPage does: as a local value it
	// overrode every Style setter, and the avatar stayed 48x48.
	[Theory]
	[InlineData(StyleKind.Explicit)]
	[InlineData(StyleKind.Implicit)]
	public async Task StyleSizesTheAvatarView(StyleKind styleKind)
	{
		SetupBuilder();
		var avatar = await InvokeOnMainThreadAsync(() =>
		{
			var style = new Style(typeof(AvatarView))
			{
				Setters =
				{
					new Setter { Property = VisualElement.WidthRequestProperty, Value = styledSize },
					new Setter { Property = VisualElement.HeightRequestProperty, Value = styledSize },
				},
			};

			var avatar = new AvatarView { Text = "AB" };
			if (styleKind is StyleKind.Explicit)
			{
				avatar.Style = style;
			}
			else
			{
				avatar.Resources.Add(style);
			}

			return avatar;
		});

		await AttachAndRun(avatar, async _ =>
		{
			await LoadedAndLaidOutAsync(avatar);

			Assert.Equal(styledSize, avatar.WidthRequest);
			Assert.Equal(styledSize, avatar.HeightRequest);
			AssertionExtensions.CloseEnough(styledSize, avatar.Width, sizeTolerance, "laid out width");
			AssertionExtensions.CloseEnough(styledSize, avatar.Height, sizeTolerance, "laid out height");
			await AssertNativeSizeAsync(avatar, styledSize, styledSize);
		});
	}

	[Fact]
	public async Task ImageIsSizedToTheAreaInsideTheStrokeAndPaddingOnceTheViewIsLaidOut()
	{
		SetupBuilder();
		var avatar = await CreateAvatarViewAsync(strokeThickness: 4, padding: new Thickness(5, 7, 11, 13), withImage: true);

		await AttachAndRun(avatar, async handler =>
		{
			await LoadedAndLaidOutAsync(avatar);
			var image = Assert.IsType<Image>(avatar.Content, exactMatch: false);
			var (expectedWidth, expectedHeight) = ExpectedImageSize(avatar);

			await AssertionExtensions.AssertEventually(
				() => IsCloseTo(image.WidthRequest, expectedWidth) && IsCloseTo(image.HeightRequest, expectedHeight),
				timeout: layoutTimeout,
				message: "The image was not sized to the area inside the stroke and the padding.");
			AssertionExtensions.CloseEnough(avatarSize - 8 - 5 - 11, expectedWidth, sizeTolerance, "expected width");
			AssertionExtensions.CloseEnough(avatarSize - 8 - 7 - 13, expectedHeight, sizeTolerance, "expected height");

			await AssertNativeSizeAsync(image, expectedWidth, expectedHeight);
		});
	}

	[Fact]
	public async Task ImageIsSizedAgainWhenTheStrokeOrThePaddingChange()
	{
		SetupBuilder();
		var avatar = await CreateAvatarViewAsync(strokeThickness: 4, padding: new Thickness(5, 7, 11, 13), withImage: true);

		await AttachAndRun(avatar, async _ =>
		{
			await LoadedAndLaidOutAsync(avatar);
			var image = Assert.IsType<Image>(avatar.Content, exactMatch: false);

			avatar.Padding = new Thickness(2, 3, 5, 7);
			avatar.BorderWidth = 6;

			var (expectedWidth, expectedHeight) = ExpectedImageSize(avatar);
			AssertionExtensions.CloseEnough(avatarSize - 12 - 2 - 5, expectedWidth, sizeTolerance, "expected width");
			AssertionExtensions.CloseEnough(avatarSize - 12 - 3 - 7, expectedHeight, sizeTolerance, "expected height");
			Assert.Equal(expectedWidth, image.WidthRequest);
			Assert.Equal(expectedHeight, image.HeightRequest);
			await AssertNativeSizeAsync(image, expectedWidth, expectedHeight);
		});
	}

	[Fact]
	public async Task ImageKeepsItsSizeRequestsUntilTheViewIsLaidOut()
	{
		SetupBuilder();
		var avatar = await CreateAvatarViewAsync(strokeThickness: 4, padding: new Thickness(5, 7, 11, 13), withImage: false);

		await InvokeOnMainThreadAsync(() =>
		{
			// Not CreateHandler, which would lay the avatar out.
			var handler = new BorderHandler();
			handler.SetMauiContext(MauiContext);
			handler.SetVirtualView(avatar);
			Assert.Same(handler, avatar.Handler);
			Assert.Equal(-1, avatar.Width);
			Assert.Equal(-1, avatar.Height);

			avatar.ImageSource = CreateRedImage();

			var image = Assert.IsType<Image>(avatar.Content, exactMatch: false);
			Assert.Equal(avatarSize, image.WidthRequest);
			Assert.Equal(avatarSize, image.HeightRequest);

			var measured = avatar.Measure(avatarSize, avatarSize);
			AssertionExtensions.CloseEnough(avatarSize, measured.Width, sizeTolerance, "measured width");
			AssertionExtensions.CloseEnough(avatarSize, measured.Height, sizeTolerance, "measured height");
			avatar.Arrange(new Rect(0, 0, measured.Width, measured.Height));

			var (expectedWidth, expectedHeight) = ExpectedImageSize(avatar);
			AssertionExtensions.CloseEnough(avatarSize - 8 - 5 - 11, expectedWidth, sizeTolerance, "expected width");
			AssertionExtensions.CloseEnough(avatarSize - 8 - 7 - 13, expectedHeight, sizeTolerance, "expected height");
			Assert.Equal(expectedWidth, image.WidthRequest);
			Assert.Equal(expectedHeight, image.HeightRequest);
		});
	}

	// MAUI sets Width before Height in a layout pass, so a first layout never has a negative width together with a valid height (-1 is the size
	// of a view that was never laid out).
	[Theory]
	[InlineData(FrameSide.Width)]
	[InlineData(FrameSide.Height)]
	public async Task ImageIsNotRefreshedWhileOneSideOfTheViewIsNegative(FrameSide negativeSide)
	{
		SetupBuilder();
		var avatar = await CreateAvatarViewAsync(strokeThickness: 4, padding: new Thickness(5, 7, 11, 13), withImage: true);
		var image = await LayOutWithoutAWindowAsync(avatar);

		await InvokeOnMainThreadAsync(() =>
		{
			PerturbImageSize(image);
			var frame = avatar.Frame;

			// Only that side changes, so its property change is the only trigger.
			avatar.Frame = negativeSide is FrameSide.Width ? frame with { Width = -1 } : frame with { Height = -1 };

			Assert.Equal(negativeSide is FrameSide.Width ? -1 : frame.Width, avatar.Width);
			Assert.Equal(negativeSide is FrameSide.Height ? -1 : frame.Height, avatar.Height);
			Assert.Equal(perturbedWidth, image.WidthRequest);
			Assert.Equal(perturbedHeight, image.HeightRequest);
		});
	}

	[Theory]
	[InlineData(FrameSide.Width)]
	[InlineData(FrameSide.Height)]
	public async Task ImageIsRefreshedWhenOnlyOneSideOfTheViewChanges(FrameSide changedSide)
	{
		SetupBuilder();
		var avatar = await CreateAvatarViewAsync(strokeThickness: 4, padding: new Thickness(5, 7, 11, 13), withImage: true);
		var image = await LayOutWithoutAWindowAsync(avatar);

		await InvokeOnMainThreadAsync(() =>
		{
			PerturbImageSize(image);
			var frame = avatar.Frame;

			avatar.Frame = changedSide is FrameSide.Width ? frame with { Width = frame.Width + 20 } : frame with { Height = frame.Height + 20 };

			var (expectedWidth, expectedHeight) = ExpectedImageSize(avatar);
			AssertionExtensions.CloseEnough(frame.Width + (changedSide is FrameSide.Width ? 20 : 0) - 8 - 5 - 11, expectedWidth, sizeTolerance, "expected width");
			AssertionExtensions.CloseEnough(frame.Height + (changedSide is FrameSide.Height ? 20 : 0) - 8 - 7 - 13, expectedHeight, sizeTolerance, "expected height");
			Assert.Equal(expectedWidth, image.WidthRequest);
			Assert.Equal(expectedHeight, image.HeightRequest);
		});
	}

	[Fact]
	public async Task OtherPropertyChangesDoNotRefreshTheImage()
	{
		SetupBuilder();
		var avatar = await CreateAvatarViewAsync(strokeThickness: 4, padding: new Thickness(5, 7, 11, 13), withImage: true);
		var image = await LayOutWithoutAWindowAsync(avatar);

		await InvokeOnMainThreadAsync(() =>
		{
			PerturbImageSize(image);

			avatar.BorderColor = Colors.Green;
			avatar.Text = "AB";
			avatar.TextColor = Colors.Yellow;
			avatar.FontSize = 31;
			avatar.FontAttributes = FontAttributes.Bold;
			avatar.CharacterSpacing = 2;
			avatar.TextTransform = TextTransform.Uppercase;
			avatar.BackgroundColor = Colors.Orange;

			Assert.Equal(perturbedWidth, image.WidthRequest);
			Assert.Equal(perturbedHeight, image.HeightRequest);
		});
	}

	[Fact]
	public async Task ImageIsNotSizedAfterTheImageSourceWasRemoved()
	{
		SetupBuilder();
		var avatar = await CreateAvatarViewAsync(strokeThickness: 4, padding: new Thickness(5, 7, 11, 13), withImage: true);

		await AttachAndRun(avatar, async _ =>
		{
			await LoadedAndLaidOutAsync(avatar);
			var image = Assert.IsType<Image>(avatar.Content, exactMatch: false);
			var sizedWidth = image.WidthRequest;
			var sizedHeight = image.HeightRequest;
			Assert.NotEqual(avatarSize, sizedWidth);

			avatar.ImageSource = null;
			Assert.IsType<Label>(avatar.Content);

			avatar.Padding = new Thickness(20);
			avatar.BorderWidth = 9;

			Assert.Equal(sizedWidth, image.WidthRequest);
			Assert.Equal(sizedHeight, image.HeightRequest);
		});
	}

	[Fact]
	public async Task ImageSourceSwitchesTheNativeContentBetweenTheLabelAndTheImage()
	{
		SetupBuilder();
		var avatar = await CreateAvatarViewAsync(strokeThickness: 1, padding: new Thickness(1), withImage: false);

		await AttachAndRun(avatar, async handler =>
		{
			await LoadedAndLaidOutAsync(avatar);
			Assert.Equal(NativeContent.Label, NativeContentOf(handler));

			avatar.ImageSource = CreateRedImage();
			Assert.Equal(NativeContent.Image, NativeContentOf(handler));

			avatar.ImageSource = null;
			Assert.Equal(NativeContent.Label, NativeContentOf(handler));
		});
	}

	// MAUI's Image shows nothing when its source fails to load.
	[Fact]
	public async Task TextIsShownWhenTheImageSourceFailsToLoad()
	{
		SetupBuilder();
		var avatar = await CreateAvatarViewAsync(strokeThickness: 1, padding: new Thickness(1), withImage: false, static avatar =>
		{
			avatar.Text = "AB";
			avatar.ImageSource = CreateUndecodableImage();
		});

		await AttachAndRun(avatar, async handler =>
		{
			await LoadedAndLaidOutAsync(avatar);

			await AssertNativeContentAsync(handler, NativeContent.Label);
			Assert.Equal("AB", Assert.IsType<Label>(avatar.Content).Text);
		});
	}

	// This stream is already there, so the load fails synchronously while the handler of the AvatarView is still mapping the image (Windows), where
	// ImageSource.FromStream opens its stream on another thread and fails later.
	[Fact]
	public async Task TextIsShownWhenTheImageSourceFailsAtOnce()
	{
		SetupBuilder();
		var avatar = await CreateAvatarViewAsync(strokeThickness: 1, padding: new Thickness(1), withImage: false, static avatar =>
		{
			avatar.Text = "AB";
			avatar.ImageSource = new StreamImageSource { Stream = UndecodableImageStreamAsync };
		});

		await AttachAndRun(avatar, async handler =>
		{
			await LoadedAndLaidOutAsync(avatar);

			await AssertNativeContentAsync(handler, NativeContent.Label);
			Assert.Equal("AB", Assert.IsType<Label>(avatar.Content).Text);
		});
	}

	[Fact]
	public async Task ImageIsShownWhenAnotherImageSourceIsSetAfterAFailure()
	{
		SetupBuilder();
		var avatar = await CreateAvatarViewAsync(strokeThickness: 1, padding: new Thickness(1), withImage: false, static avatar =>
		{
			avatar.Text = "AB";
			avatar.ImageSource = CreateUndecodableImage();
		});

		await AttachAndRun(avatar, async handler =>
		{
			await LoadedAndLaidOutAsync(avatar);
			await AssertNativeContentAsync(handler, NativeContent.Label);

			avatar.ImageSource = CreateRedImage();

			await AssertPixelAsync(handler, avatarSize / 2, avatarSize / 2, Colors.Red);
			await AssertNativeContentAsync(handler, NativeContent.Image);
		});
	}

	// After a failed load the text replaces the image, which leaves the tree: a binding of the image source still applies there, and the image
	// replaces the text once the source that the binding changes loads (a GravatarImageSource whose bound Email changes, for example).
	[Fact]
	public async Task ImageIsShownWhenABindingChangesTheImageSourceAfterAFailure()
	{
		SetupBuilder();
		var viewModel = new ImageStreamViewModel { Stream = UndecodableImageStreamAsync };
		var avatar = await CreateAvatarViewAsync(strokeThickness: 1, padding: new Thickness(1), withImage: false, avatar =>
		{
			var source = new StreamImageSource();
			source.SetBinding(StreamImageSource.StreamProperty, new Binding(nameof(ImageStreamViewModel.Stream)));

			avatar.Text = "AB";
			avatar.ImageSource = source;
			avatar.BindingContext = viewModel;
		});

		await AttachAndRun(avatar, async handler =>
		{
			await LoadedAndLaidOutAsync(avatar);
			await AssertNativeContentAsync(handler, NativeContent.Label);

			viewModel.Stream = RedImageStreamAsync;

			await AssertPixelAsync(handler, avatarSize / 2, avatarSize / 2, Colors.Red);
			await AssertNativeContentAsync(handler, NativeContent.Image);
		});
	}

	// The text would show through the transparent parts of an image.
	[Fact]
	public async Task ImageThatLoadsIsShownWithoutTheText()
	{
		SetupBuilder();
		var avatar = await CreateAvatarViewAsync(strokeThickness: 1, padding: new Thickness(1), withImage: true);

		await AttachAndRun(avatar, async handler =>
		{
			await LoadedAndLaidOutAsync(avatar);

			await AssertPixelAsync(handler, avatarSize / 2, avatarSize / 2, Colors.Red);
			Assert.Equal(NativeContent.Image, NativeContentOf(handler));
			Assert.IsType<Image>(avatar.Content, exactMatch: false);
		});
	}

	[Fact]
	public async Task WithoutAStrokeShapeTheImageIsClippedWithTheGeometryOfThePlatform()
	{
		SetupBuilder();
		var avatar = await CreateAvatarViewAsync(strokeThickness: 3, padding: new Thickness(5, 7, 11, 13), withImage: true, static avatar =>
		{
			avatar.CornerRadius = new CornerRadius(10, 20, 30, 40);
			// Setting the corner radius creates a stroke shape, so clearing it comes last.
			avatar.StrokeShape = null;
		});

		await AttachAndRun(avatar, async handler =>
		{
			await LoadedAndLaidOutAsync(avatar);
			var image = Assert.IsType<Image>(avatar.Content, exactMatch: false);

			var (expectedWidth, expectedHeight) = ExpectedImageSize(avatar);
			AssertionExtensions.CloseEnough(avatarSize - 6 - 5 - 11, expectedWidth, sizeTolerance, "expected width");
			AssertionExtensions.CloseEnough(avatarSize - 6 - 7 - 13, expectedHeight, sizeTolerance, "expected height");
			AssertFallbackClip(avatar, image, expectedWidth, expectedHeight);

			var nativeClipIsTheImageClip = await AssertHelpers.Wait(
				() => ReferenceEquals(NativeClipOf(image), image.Clip) && (NativeClipOf(image) is not null) == BuildsFallbackClipGeometry,
				timeout: layoutTimeout);
			Assert.True(nativeClipIsTheImageClip, $"The native image {(BuildsFallbackClipGeometry ? "is not clipped by the fallback geometry" : "has a clip")}: native clip {Describe(NativeClipOf(image))}, image.Clip {Describe(image.Clip)}.");
		});
	}

#if ANDROID || WINDOWS
	// The fallback geometry clips the image in the image's own coordinates: the image is already laid out inside the stroke and the
	// padding, so the geometry must start at the image's origin. The Apple platforms build no geometry (UIKit clips the content to the
	// border shape), so this has no counterpart there.
	[Fact]
	public async Task FallbackClipGeometryStartsAtTheImageOrigin()
	{
		SetupBuilder();
		var avatar = await CreateAvatarViewAsync(strokeThickness: 4, padding: new Thickness(5, 7, 11, 13), withImage: true, static avatar => avatar.StrokeShape = null);

		var image = await LayOutWithoutAWindowAsync(avatar);

		await InvokeOnMainThreadAsync(() =>
		{
			var geometry = Assert.IsType<RoundRectangleGeometry>(image.Clip);
			Assert.Equal(0, geometry.Rect.X);
			Assert.Equal(0, geometry.Rect.Y);
			Assert.Equal(image.WidthRequest, geometry.Rect.Width, 0.01);
			Assert.Equal(image.HeightRequest, geometry.Rect.Height, 0.01);
		});
	}
#endif

	[Theory]
	[InlineData(AreaChange.Padding)]
	[InlineData(AreaChange.BorderWidth)]
	[InlineData(AreaChange.BorderWidthAlone)]
	[InlineData(AreaChange.StrokeThickness)]
	[InlineData(AreaChange.CornerRadius)]
	public async Task ChangingAPropertyOfTheImageAreaRefreshesTheImage(AreaChange change)
	{
		SetupBuilder();
		var avatar = await CreateAvatarViewAsync(strokeThickness: 4, padding: new Thickness(5, 7, 11, 13), withImage: true);

		await AttachAndRun(avatar, async _ =>
		{
			await LoadedAndLaidOutAsync(avatar);
			var image = Assert.IsType<Image>(avatar.Content, exactMatch: false);
			if (change is AreaChange.BorderWidthAlone)
			{
				avatar.StrokeThickness = 6;
			}

			PerturbImageSize(image);

			switch (change)
			{
				case AreaChange.Padding:
					avatar.Padding = new Thickness(2, 3, 5, 7);
					break;
				case AreaChange.BorderWidth:
				case AreaChange.BorderWidthAlone:
					avatar.BorderWidth = 6;
					break;
				case AreaChange.StrokeThickness:
					avatar.StrokeThickness = 7;
					break;
				case AreaChange.CornerRadius:
					avatar.CornerRadius = new CornerRadius(7);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(change), change, null);
			}

			var (expectedWidth, expectedHeight) = ExpectedImageSize(avatar);
			Assert.Equal(expectedWidth, image.WidthRequest);
			Assert.Equal(expectedHeight, image.HeightRequest);
			await AssertNativeSizeAsync(image, expectedWidth, expectedHeight);
		});
	}

	// A clip geometry offset by the stroke and the padding, in the coordinates of the image itself, would cut off its left and top parts.
	[Fact]
	public async Task ImageWithoutAStrokeShapeIsNotCutOff()
	{
		SetupBuilder();
		var avatar = await CreateAvatarViewAsync(strokeThickness: 10, padding: new Thickness(12), withImage: true, static avatar =>
		{
			avatar.CornerRadius = new CornerRadius(10);
			avatar.StrokeShape = null;
		});

		await AttachAndRun(avatar, async handler =>
		{
			await LoadedAndLaidOutAsync(avatar);

			// The image area is 22..78 in both directions. These points are well inside it, away from the rounded corners.
			foreach (var (x, y) in new[] { (30d, 30d), (50d, 30d), (30d, 50d), (50d, 50d), (70d, 50d), (50d, 70d), (70d, 70d) })
			{
				await AssertPixelAsync(handler, x, y, Colors.Red);
			}
		});
	}

	[Fact]
	public async Task RoundedShapeClipsTheImageToItsCornersAndTheStrokeIsDrawnAroundIt()
	{
		SetupBuilder();
		var avatar = await CreateAvatarViewAsync(strokeThickness: 4, padding: new Thickness(5, 7, 11, 13), withImage: true, static avatar =>
		{
			avatar.CornerRadius = new CornerRadius(50);
			avatar.BorderColor = Colors.Blue;
		});

		await AttachAndRun(avatar, async handler =>
		{
			await LoadedAndLaidOutAsync(avatar);

			await AssertPixelAsync(handler, 50, 50, Colors.Red);
			await AssertPixelAsync(handler, 30, 40, Colors.Red);
			await AssertPixelAsync(handler, 70, 60, Colors.Red);
			await AssertPixelAsync(handler, 50, 1.5, Colors.Blue);
			await AssertPixelAsync(handler, 1.5, 50, Colors.Blue);
			await AssertPixelAsync(handler, 98, 50, Colors.Blue);
			await AssertPixelAsync(handler, 50, 98, Colors.Blue);
			await AssertPixelAsync(handler, 3, 3, Colors.Transparent);
			await AssertPixelAsync(handler, 96, 3, Colors.Transparent);
			await AssertPixelAsync(handler, 3, 96, Colors.Transparent);
			await AssertPixelAsync(handler, 96, 96, Colors.Transparent);
		});
	}

	[Theory]
	[InlineData(ClipShape.Polyline)]
	[InlineData(ClipShape.Ellipse)]
	[InlineData(ClipShape.Path)]
	[InlineData(ClipShape.Polygon)]
	[InlineData(ClipShape.Rectangle)]
	[InlineData(ClipShape.RoundRectangle)]
	public async Task ImageUsesTheClipOfTheStrokeShape(ClipShape shapeKind)
	{
		SetupBuilder();
		var avatar = await CreateAvatarViewAsync(strokeThickness: 2, padding: new Thickness(3), withImage: true);
		var clip = new RectangleGeometry(new Rect(0, 0, 30, 30));

		await AttachAndRun(avatar, async _ =>
		{
			await LoadedAndLaidOutAsync(avatar);
			var image = Assert.IsType<Image>(avatar.Content, exactMatch: false);
			Assert.Null(image.Clip);
			Assert.Null(NativeClipOf(image));

			avatar.StrokeShape = CreateShape(shapeKind, clip);
			avatar.Padding = new Thickness(4);

			Assert.Same(clip, image.Clip);
			var nativeImageIsClipped = await AssertHelpers.Wait(() => ReferenceEquals(NativeClipOf(image), clip), timeout: layoutTimeout);
			Assert.True(nativeImageIsClipped, $"The native image is not clipped by the clip of the stroke shape: native clip {Describe(NativeClipOf(image))}.");
		});
	}

	[Fact]
	public async Task ChangingTheStrokeShapeRefreshesTheClipOfTheImage()
	{
		SetupBuilder();
		var avatar = await CreateAvatarViewAsync(strokeThickness: 2, padding: new Thickness(3), withImage: true);
		var image = await LayOutWithoutAWindowAsync(avatar);
		var clip = new RectangleGeometry(new Rect(0, 0, 30, 30));

		await InvokeOnMainThreadAsync(() =>
		{
			// The default stroke shape (a RoundRectangle) has no clip of its own.
			Assert.Null(image.Clip);

			avatar.StrokeShape = new Ellipse { Clip = clip };

			Assert.Same(clip, image.Clip);
		});
	}

	[Fact]
	public async Task AvatarViewDoesNotLeak()
	{
		SetupBuilder();
		var references = await CreateLoadAndDisconnectAnAvatarAsync();

		await AssertionExtensions.WaitForGC(references);
	}

	// The image loads before the handlers are disconnected: while it still loads, the avatar stays alive until the load has completed, which is
	// no leak (on Windows 11 26200 the forced collections of WaitForGC ended first, while after the load nothing stayed alive). Only weak
	// references leave this method.
	[MethodImpl(MethodImplOptions.NoInlining)]
	async Task<WeakReference[]> CreateLoadAndDisconnectAnAvatarAsync()
	{
		var (avatar, handler, image) = await InvokeOnMainThreadAsync(() =>
		{
			var avatar = CreateAvatarView(strokeThickness: 2, padding: new Thickness(3), withImage: true);
			var handler = CreateHandler<BorderHandler>(avatar);
			return (avatar, handler, Assert.IsType<Image>(avatar.Content, exactMatch: false));
		});

		await AssertionExtensions.AssertEventuallyAsync(() => InvokeOnMainThreadAsync(() => !image.IsLoading), layoutTimeout, 50, "The image of the avatar never finished loading.");

		return await InvokeOnMainThreadAsync(() =>
		{
			var imageHandler = Assert.IsType<IPlatformViewHandler>(image.Handler, exactMatch: false);
			WeakReference[] references = [new(avatar), new(handler), new(handler.PlatformView), new(image), new(imageHandler), new(imageHandler.PlatformView)];

			((IElementHandler)handler).DisconnectHandler();
			avatar.Handler = null;
			return references;
		});
	}

	// AvatarView binds the WidthRequest and HeightRequest of its image to its own (BindingBase.Create in its constructor), and a binding applies
	// a change made off the UI thread by dispatching it to the UI thread, while the thread that made the change goes on writing to the same
	// image (ImageSource, the size refresh). An avatar built on the test's thread was therefore written by two threads at once: in a full run
	// of the app on Android API 36 the image's bindable-property bookkeeping (a HashSet) broke and the UI thread crashed the app
	// (IndexOutOfRangeException in Element.OnBindablePropertySet). So every test creates and sets up its avatar on the UI thread.
	Task<AvatarView> CreateAvatarViewAsync(double strokeThickness, Thickness padding, bool withImage, Action<AvatarView>? setUp = null) =>
		InvokeOnMainThreadAsync(() =>
		{
			var avatar = CreateAvatarView(strokeThickness, padding, withImage);
			setUp?.Invoke(avatar);
			return avatar;
		});

	// Call it on the UI thread (see CreateAvatarViewAsync).
	static AvatarView CreateAvatarView(double strokeThickness, Thickness padding, bool withImage)
	{
		var avatar = new AvatarView
		{
			WidthRequest = avatarSize,
			HeightRequest = avatarSize,
			BorderWidth = strokeThickness,
			Padding = padding,
		};

		if (withImage)
		{
			avatar.ImageSource = CreateRedImage();
		}

		return avatar;
	}

	static ImageSource CreateRedImage() => ImageSource.FromStream(() => new MemoryStream(Convert.FromBase64String(redPngBase64)));

	// Simulates a server that answers the request for an avatar with an error page, without a network.
	static ImageSource CreateUndecodableImage() => ImageSource.FromStream(() => new MemoryStream(UndecodableImageBytes()));

	static byte[] UndecodableImageBytes() => "<html><body>403 Forbidden</body></html>"u8.ToArray();

	static Task<Stream> RedImageStreamAsync(CancellationToken token) => Task.FromResult<Stream>(new MemoryStream(Convert.FromBase64String(redPngBase64)));

	static Task<Stream> UndecodableImageStreamAsync(CancellationToken token) => Task.FromResult<Stream>(new MemoryStream(UndecodableImageBytes()));

	static async Task AssertNativeContentAsync(IPlatformViewHandler handler, NativeContent expected)
	{
		var hasExpectedContent = await AssertHelpers.Wait(() => NativeContentOf(handler) == expected, timeout: layoutTimeout);

		Assert.True(hasExpectedContent, $"The native content of the avatar is {NativeContentOf(handler)}, expected {expected}.");
	}

	// Outside any window no platform layout pass runs afterwards, so a frame or a size that a test sets stays as it set it.
	Task<Image> LayOutWithoutAWindowAsync(AvatarView avatar) =>
		InvokeOnMainThreadAsync(() =>
		{
			var handler = new BorderHandler();
			handler.SetMauiContext(MauiContext);
			handler.SetVirtualView(avatar);

			var measured = avatar.Measure(avatarSize, avatarSize);
			avatar.Arrange(new Rect(0, 0, measured.Width, measured.Height));

			var image = Assert.IsType<Image>(avatar.Content, exactMatch: false);
			var (expectedWidth, expectedHeight) = ExpectedImageSize(avatar);
			AssertionExtensions.CloseEnough(avatarSize, avatar.Width, sizeTolerance, "laid out width");
			AssertionExtensions.CloseEnough(avatarSize, avatar.Height, sizeTolerance, "laid out height");
			Assert.Equal(expectedWidth, image.WidthRequest);
			Assert.Equal(expectedHeight, image.HeightRequest);
			return image;
		});

	static void PerturbImageSize(Image image)
	{
		image.WidthRequest = perturbedWidth;
		image.HeightRequest = perturbedHeight;
	}

	// A view that AttachAndRun hosts on its own is laid out by InitializeViewHandler before it is hosted. On Windows that cannot measure a view
	// that is not loaded, so the arrange leaves an empty Frame (centered, 0x0) and no MAUI parent lays it out later: there it is laid out once
	// it is loaded.
	static async Task LoadedAndLaidOutAsync(AvatarView avatar)
	{
		await avatar.OnLoadedAsync();
#if WINDOWS
		var size = ((IView)avatar).Measure(avatar.WidthRequest, avatar.HeightRequest);
		((IView)avatar).Arrange(new Rect(0, 0, size.Width, size.Height));
#endif
		if (!await AssertHelpers.Wait(() => avatar.Width > 0 && avatar.Height > 0, timeout: layoutTimeout))
		{
			throw new XunitException($"The avatar was never laid out (Frame {avatar.Frame}, DesiredSize {avatar.DesiredSize}, requested {avatar.WidthRequest}x{avatar.HeightRequest}).");
		}
	}

	static (double Width, double Height) ExpectedImageSize(AvatarView avatar) =>
		(avatar.Width - (avatar.StrokeThickness * 2) - avatar.Padding.Left - avatar.Padding.Right,
		avatar.Height - (avatar.StrokeThickness * 2) - avatar.Padding.Top - avatar.Padding.Bottom);

	static bool IsCloseTo(double actual, double expected) => Math.Abs(actual - expected) <= 0.01;

	static async Task AssertNativeSizeAsync(IView view, double expectedWidth, double expectedHeight)
	{
		var resized = await AssertHelpers.Wait(
			() => Math.Abs(NativeSizeOf(view).Width - expectedWidth) <= sizeTolerance && Math.Abs(NativeSizeOf(view).Height - expectedHeight) <= sizeTolerance,
			timeout: layoutTimeout);

		var nativeSize = NativeSizeOf(view);
		Assert.True(resized, $"The native {view.GetType().Name} is {nativeSize.Width}x{nativeSize.Height}, expected {expectedWidth}x{expectedHeight}.");
	}

	// MAUI wraps a native view that has a clip in a container view (WrapperView), which applies the view's clip. The container alone is no
	// oracle: on Windows MAUI wraps every AspectFill image (the avatar's image) in a WrapperView, with or without a clip.
	static IShape? NativeClipOf(IView view) =>
		(view.Handler as IViewHandler)?.ContainerView is Microsoft.Maui.Platform.WrapperView wrapper ? wrapper.Clip : null;

	static string Describe(object? clip) => clip switch
	{
		null => "null",
		RectangleGeometry rectangle => $"RectangleGeometry {rectangle.Rect}",
		RoundRectangleGeometry roundRectangle => $"RoundRectangleGeometry {roundRectangle.Rect} {roundRectangle.CornerRadius}",
		_ => clip.GetType().Name,
	};

	static Shape CreateShape(ClipShape kind, Geometry clip) => kind switch
	{
		ClipShape.Polyline => new Polyline { Clip = clip },
		ClipShape.Ellipse => new Ellipse { Clip = clip },
		ClipShape.Path => new Microsoft.Maui.Controls.Shapes.Path { Clip = clip },
		ClipShape.Polygon => new Polygon { Clip = clip },
		ClipShape.Rectangle => new Rectangle { Clip = clip },
		ClipShape.RoundRectangle => new RoundRectangle { Clip = clip },
		_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
	};

	// The image is decoded and drawn asynchronously and the clip is applied by the next layout pass, so the pixel is polled until it has the
	// expected color.
	static async Task AssertPixelAsync(IPlatformViewHandler handler, double x, double y, Color expected)
	{
		var actual = Colors.Transparent;

		try
		{
			await AssertionExtensions.AssertEventuallyAsync(
				async () =>
				{
					actual = await ColorAtAsync(handler, x, y);
					return PixelHasColor(actual, expected);
				},
				timeout: layoutTimeout,
				message: $"Pixel ({x}, {y}) did not get the color {expected.ToArgbHex()}.");
		}
		catch (XunitException exception)
		{
			throw new XunitException($"{exception.Message} It is {actual.ToArgbHex()}.");
		}
	}

	static bool PixelHasColor(Color actual, Color expected) =>
		expected.Alpha < 0.5f
			? actual.Alpha < 0.1f
			: actual.Alpha > 0.9f && Math.Abs(actual.Red - expected.Red) < 0.1f && Math.Abs(actual.Green - expected.Green) < 0.1f && Math.Abs(actual.Blue - expected.Blue) < 0.1f;
}