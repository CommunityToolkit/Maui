using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using static CommunityToolkit.Maui.DeviceTests.AssertHelpers;
#if IOS || MACCATALYST
using PlatformView = UIKit.UIView;
#elif ANDROID
using PlatformView = Android.Views.View;
#elif WINDOWS
using PlatformView = Microsoft.UI.Xaml.FrameworkElement;
#endif

namespace CommunityToolkit.Maui.DeviceTests;

// MAUI attaches the PlatformBehavior while the view is Loaded, so the tests host the view in the runner's window. The images are solid
// colors, so the rendered pixels tell which source the native view shows.
[Category(TestCategory.ImageTouchBehavior)]
[Collection(TestCollections.DisableParallelization)]
public partial class ImageTouchBehaviorTests : CommunityToolkitHandlerTestBase
{
	const string defaultSource = "behavior_red.png";
	const string hoveredSource = "behavior_green.png";
	const string pressedSource = "behavior_blue.png";
	const string changedSource = "behavior_yellow.png";

	static readonly Color defaultColor = Colors.Red;
	static readonly Color hoveredColor = Colors.Lime;
	static readonly Color pressedColor = Colors.Blue;
	static readonly Color changedColor = Colors.Yellow;

	public enum ImageKind
	{
		Image,
		ImageButton
	}

	public enum Interaction
	{
		Idle,
		Hovered,
		Pressed,
		HoveredAndPressed
	}

	public enum ChangedSource
	{
		Default,
		Hovered,
		Pressed
	}

	public enum ChangedAspect
	{
		Default,
		Hovered,
		Pressed
	}

	[Theory]
	[InlineData(ImageKind.Image)]
	[InlineData(ImageKind.ImageButton)]
	public async Task DefaultImageSourceAndAspectAreAppliedWhenTheViewLoads(ImageKind kind)
	{
		SetupBuilder();

		var behavior = CreateBehavior();
		behavior.DefaultImageAspect = Aspect.AspectFill;
		var view = CreateImageView(kind);
		view.Behaviors.Add(behavior);

		await AttachAndRun(view, async handler =>
		{
			await view.OnLoadedAsync();

			await AssertShowsAsync(handler, kind, defaultColor, Aspect.AspectFill);
		});
	}

	[Theory]
	[InlineData(ImageKind.Image)]
	[InlineData(ImageKind.ImageButton)]
	public async Task PressedStateShowsThePressedImageSourceAndAspect(ImageKind kind)
	{
		SetupBuilder();

		var behavior = CreateBehavior();
		var view = CreateImageView(kind);
		view.Behaviors.Add(behavior);

		await AttachAndRun(view, async handler =>
		{
			await view.OnLoadedAsync();
			await AssertShowsAsync(handler, kind, defaultColor, Aspect.AspectFit);

			await PressAsync(handler, behavior);

			Assert.Equal(TouchState.Pressed, behavior.CurrentTouchState);
			await AssertShowsAsync(handler, kind, pressedColor, Aspect.Fill);

			await ReleaseAsync(handler, behavior);

			Assert.Equal(TouchState.Default, behavior.CurrentTouchState);
			await AssertShowsAsync(handler, kind, defaultColor, Aspect.AspectFit);
		});
	}

	[Theory]
	[InlineData(ImageKind.Image)]
	[InlineData(ImageKind.ImageButton)]
	public async Task HoveredStateShowsTheHoveredImageSourceAndAspect(ImageKind kind)
	{
		SetupBuilder();

		var behavior = CreateBehavior();
		var view = CreateImageView(kind);
		view.Behaviors.Add(behavior);

		await AttachAndRun(view, async handler =>
		{
			await view.OnLoadedAsync();
			await AssertShowsAsync(handler, kind, defaultColor, Aspect.AspectFit);

			await HoverEnterAsync(handler, behavior);

			Assert.Equal(HoverState.Hovered, behavior.CurrentHoverState);
			await AssertShowsAsync(handler, kind, hoveredColor, Aspect.AspectFill);

			await HoverExitAsync(handler, behavior);

			Assert.Equal(HoverState.Default, behavior.CurrentHoverState);
			await AssertShowsAsync(handler, kind, defaultColor, Aspect.AspectFit);
		});
	}

	[Fact]
	public async Task HoveredStateKeepsTheDefaultImageSourceAndAspectWhenNoHoveredOnesAreSet()
	{
		SetupBuilder();

		var behavior = new ImageTouchBehavior { DefaultImageSource = defaultSource, DefaultImageAspect = Aspect.AspectFill };
		var image = CreateImageView(ImageKind.Image);
		image.Behaviors.Add(behavior);

		await AttachAndRun(image, async handler =>
		{
			await image.OnLoadedAsync();
			await AssertShowsAsync(handler, ImageKind.Image, defaultColor, Aspect.AspectFill);

			await HoverEnterAsync(handler, behavior);

			Assert.Equal(HoverState.Hovered, behavior.CurrentHoverState);
			Assert.Same(behavior.DefaultImageSource, GetSource(image));
			await AssertShowsAsync(handler, ImageKind.Image, defaultColor, Aspect.AspectFill);
		});
	}

	[Fact]
	public async Task HoveredStateShowsTheBuiltInAspectWhenNeitherHoveredNorDefaultAspectIsSet()
	{
		SetupBuilder();

		var behavior = new ImageTouchBehavior { DefaultImageSource = defaultSource };
		var image = new Image { WidthRequest = 40, HeightRequest = 40, Aspect = Aspect.Fill };
		image.Behaviors.Add(behavior);

		await AttachAndRun(image, async handler =>
		{
			await image.OnLoadedAsync();

			// The behavior takes over the aspect of the view, with AspectFit when it has none of its own.
			await AssertShowsAsync(handler, ImageKind.Image, defaultColor, ImageTouchBehaviorDefaults.DefaultBackgroundImageAspect);

			// The behavior does not watch the view's own Aspect: from here on only the hovered state can show the built-in aspect again.
			image.Aspect = Aspect.Fill;
			await AssertShowsAsync(handler, ImageKind.Image, defaultColor, Aspect.Fill);

			await HoverEnterAsync(handler, behavior);

			Assert.Equal(HoverState.Hovered, behavior.CurrentHoverState);
			await AssertShowsAsync(handler, ImageKind.Image, defaultColor, ImageTouchBehaviorDefaults.HoveredBackgroundImageAspect);
		});
	}

	[Fact]
	public async Task PressedStateKeepsTheDefaultImageSourceAndAspectWhenNoPressedOnesAreSet()
	{
		SetupBuilder();

		var behavior = new ImageTouchBehavior { DefaultImageSource = defaultSource, DefaultImageAspect = Aspect.AspectFill };
		var image = CreateImageView(ImageKind.Image);
		image.Behaviors.Add(behavior);

		await AttachAndRun(image, async handler =>
		{
			await image.OnLoadedAsync();
			await AssertShowsAsync(handler, ImageKind.Image, defaultColor, Aspect.AspectFill);

			await PressAsync(handler, behavior);

			Assert.Equal(TouchState.Pressed, behavior.CurrentTouchState);
			Assert.Same(behavior.DefaultImageSource, GetSource(image));
			await AssertShowsAsync(handler, ImageKind.Image, defaultColor, Aspect.AspectFill);
		});
	}

	[Fact]
	public async Task PressedStateWinsOverTheHoveredStateAndTheHoveredImageReturnsWhenTheTouchEnds()
	{
		SetupBuilder();

		var behavior = CreateBehavior();
		var image = CreateImageView(ImageKind.Image);
		image.Behaviors.Add(behavior);

		await AttachAndRun(image, async handler =>
		{
			await image.OnLoadedAsync();

			await HoverEnterAsync(handler, behavior);
			await AssertShowsAsync(handler, ImageKind.Image, hoveredColor, Aspect.AspectFill);

			await PressAsync(handler, behavior);
			await AssertShowsAsync(handler, ImageKind.Image, pressedColor, Aspect.Fill);

			await ReleaseAsync(handler, behavior);
			await AssertShowsAsync(handler, ImageKind.Image, hoveredColor, Aspect.AspectFill);

			await HoverExitAsync(handler, behavior);
			await AssertShowsAsync(handler, ImageKind.Image, defaultColor, Aspect.AspectFit);
		});
	}

	[Theory]
	[InlineData(ImageKind.Image, ChangedSource.Default, Interaction.Idle, true, true)]
	[InlineData(ImageKind.Image, ChangedSource.Default, Interaction.Hovered, false, true)]
	[InlineData(ImageKind.Image, ChangedSource.Default, Interaction.Hovered, true, false)]
	[InlineData(ImageKind.Image, ChangedSource.Default, Interaction.Pressed, true, false)]
	[InlineData(ImageKind.Image, ChangedSource.Default, Interaction.HoveredAndPressed, true, false)]
	[InlineData(ImageKind.Image, ChangedSource.Hovered, Interaction.Hovered, true, true)]
	[InlineData(ImageKind.Image, ChangedSource.Hovered, Interaction.Idle, true, false)]
	[InlineData(ImageKind.Image, ChangedSource.Hovered, Interaction.Pressed, true, false)]
	[InlineData(ImageKind.Image, ChangedSource.Hovered, Interaction.HoveredAndPressed, true, false)]
	[InlineData(ImageKind.Image, ChangedSource.Pressed, Interaction.Pressed, true, true)]
	[InlineData(ImageKind.Image, ChangedSource.Pressed, Interaction.HoveredAndPressed, true, true)]
	[InlineData(ImageKind.Image, ChangedSource.Pressed, Interaction.Idle, true, false)]
	[InlineData(ImageKind.Image, ChangedSource.Pressed, Interaction.Hovered, true, false)]
	[InlineData(ImageKind.ImageButton, ChangedSource.Default, Interaction.Idle, true, true)]
	[InlineData(ImageKind.ImageButton, ChangedSource.Hovered, Interaction.Hovered, true, true)]
	[InlineData(ImageKind.ImageButton, ChangedSource.Pressed, Interaction.Pressed, true, true)]
	[InlineData(ImageKind.ImageButton, ChangedSource.Pressed, Interaction.Hovered, true, false)]
	public async Task ChangingAnImageSourceUpdatesTheImageOnlyWhileThatSourceIsShown(ImageKind kind, ChangedSource changed, Interaction interaction, bool hasHoveredSource, bool expectUpdate)
	{
		SetupBuilder();

		var behavior = CreateBehavior(hasHoveredSource);
		var view = CreateImageView(kind);
		view.Behaviors.Add(behavior);
		var newSource = ImageSource.FromFile(changedSource);

		await AttachAndRun(view, async handler =>
		{
			await view.OnLoadedAsync();
			await EnterAsync(handler, behavior, interaction);

			var shownBefore = interaction switch
			{
				Interaction.Pressed or Interaction.HoveredAndPressed => behavior.PressedImageSource,
				Interaction.Hovered when hasHoveredSource => behavior.HoveredImageSource,
				_ => behavior.DefaultImageSource
			};
			await AssertEventually(() => ReferenceEquals(shownBefore, GetSource(view)), timeout: 3000, message: $"The {interaction} state never showed its image source.");

			switch (changed)
			{
				case ChangedSource.Default:
					behavior.DefaultImageSource = newSource;
					break;
				case ChangedSource.Hovered:
					behavior.HoveredImageSource = newSource;
					break;
				default:
					behavior.PressedImageSource = newSource;
					break;
			}

			if (expectUpdate)
			{
				Assert.Same(newSource, GetSource(view));
				await WaitForPixelsAsync(handler.PlatformView!, changedColor);
			}
			else
			{
				Assert.Same(shownBefore, GetSource(view));
			}
		});
	}

	[Theory]
	[InlineData(ImageKind.Image, ChangedSource.Pressed)]
	[InlineData(ImageKind.ImageButton, ChangedSource.Pressed)]
	[InlineData(ImageKind.Image, ChangedSource.Hovered)]
	[InlineData(ImageKind.ImageButton, ChangedSource.Hovered)]
	public async Task ClearingTheShownImageSourceShowsTheDefaultImageSource(ImageKind kind, ChangedSource cleared)
	{
		SetupBuilder();

		var behavior = CreateBehavior();
		var view = CreateImageView(kind);
		view.Behaviors.Add(behavior);

		await AttachAndRun(view, async handler =>
		{
			await view.OnLoadedAsync();
			await EnterAsync(handler, behavior, cleared is ChangedSource.Pressed ? Interaction.Pressed : Interaction.Hovered);

			var shown = cleared is ChangedSource.Pressed ? behavior.PressedImageSource : behavior.HoveredImageSource;
			await AssertEventually(() => ReferenceEquals(shown, GetSource(view)), timeout: 3000, message: $"The {cleared} state never showed its image source.");

			if (cleared is ChangedSource.Pressed)
			{
				behavior.PressedImageSource = null;
			}
			else
			{
				behavior.HoveredImageSource = null;
			}

			Assert.Same(behavior.DefaultImageSource, GetSource(view));
			await WaitForPixelsAsync(handler.PlatformView!, defaultColor);
		});
	}

	[Theory]
	[InlineData(ImageKind.Image, Interaction.Pressed)]
	[InlineData(ImageKind.ImageButton, Interaction.Pressed)]
	[InlineData(ImageKind.Image, Interaction.HoveredAndPressed)]
	public async Task DefaultImageSourceChangeIsShownWhilePressedWithoutAPressedImageSource(ImageKind kind, Interaction interaction)
	{
		SetupBuilder();

		// The pressed aspect differs from the default one: once it is shown, the state machine has finished with the pressed state.
		var behavior = new ImageTouchBehavior { DefaultImageSource = defaultSource, DefaultImageAspect = Aspect.AspectFit, PressedImageAspect = Aspect.Fill };
		var view = CreateImageView(kind);
		view.Behaviors.Add(behavior);
		var newSource = ImageSource.FromFile(changedSource);

		await AttachAndRun(view, async handler =>
		{
			await view.OnLoadedAsync();
			await EnterAsync(handler, behavior, interaction);
			await AssertShowsAsync(handler, kind, defaultColor, Aspect.Fill);
			Assert.Equal(TouchState.Pressed, behavior.CurrentTouchState);

			behavior.DefaultImageSource = newSource;

			Assert.Same(newSource, GetSource(view));
			await WaitForPixelsAsync(handler.PlatformView!, changedColor);
		});
	}

	// Xamarin Community Toolkit's TouchEffect re-applied the state for every image aspect change, so a new aspect of the state that is shown
	// is shown right away.
	[Theory]
	[InlineData(ImageKind.Image, ChangedAspect.Default, Interaction.Idle)]
	[InlineData(ImageKind.Image, ChangedAspect.Hovered, Interaction.Hovered)]
	[InlineData(ImageKind.Image, ChangedAspect.Pressed, Interaction.Pressed)]
	[InlineData(ImageKind.ImageButton, ChangedAspect.Default, Interaction.Idle)]
	public async Task ChangingTheImageAspectOfTheShownStateUpdatesTheImage(ImageKind kind, ChangedAspect changed, Interaction interaction)
	{
		SetupBuilder();

		var behavior = CreateBehavior();
		var view = CreateImageView(kind);
		view.Behaviors.Add(behavior);

		await AttachAndRun(view, async handler =>
		{
			await view.OnLoadedAsync();
			await EnterAsync(handler, behavior, interaction);

			var (shownColor, shownAspect) = changed switch
			{
				ChangedAspect.Pressed => (pressedColor, Aspect.Fill),
				ChangedAspect.Hovered => (hoveredColor, Aspect.AspectFill),
				_ => (defaultColor, Aspect.AspectFit)
			};
			await AssertShowsAsync(handler, kind, shownColor, shownAspect);

			switch (changed)
			{
				case ChangedAspect.Default:
					behavior.DefaultImageAspect = Aspect.Center;
					break;
				case ChangedAspect.Hovered:
					behavior.HoveredImageAspect = Aspect.Center;
					break;
				default:
					behavior.PressedImageAspect = Aspect.Center;
					break;
			}

			await AssertEventually(() => GetNativeAspect(handler.PlatformView!) == Aspect.Center, timeout: 2000, message: $"The new aspect of the {changed} state is not shown: {DescribeNativeAspect(handler.PlatformView!)}");
		});
	}

	[Theory]
	[InlineData(ImageKind.Image, Interaction.Pressed)]
	[InlineData(ImageKind.Image, Interaction.Hovered)]
	[InlineData(ImageKind.ImageButton, Interaction.Pressed)]
	public async Task StateWithoutAnImageAspectOfItsOwnShowsTheDefaultImageAspect(ImageKind kind, Interaction interaction)
	{
		SetupBuilder();

		var behavior = CreateBehavior();
		var view = CreateImageView(kind);
		view.Behaviors.Add(behavior);

		await AttachAndRun(view, async handler =>
		{
			await view.OnLoadedAsync();
			await EnterAsync(handler, behavior, interaction);

			var (shownColor, ownAspect) = interaction is Interaction.Pressed ? (pressedColor, Aspect.Fill) : (hoveredColor, Aspect.AspectFill);
			await AssertShowsAsync(handler, kind, shownColor, ownAspect);

			if (interaction is Interaction.Pressed)
			{
				behavior.PressedImageAspect = null;
			}
			else
			{
				behavior.HoveredImageAspect = null;
			}

			await AssertEventually(() => GetNativeAspect(handler.PlatformView!) == Aspect.AspectFit, timeout: 2000, message: $"The default aspect is not shown once the {interaction} aspect was cleared: {DescribeNativeAspect(handler.PlatformView!)}");

			behavior.DefaultImageAspect = Aspect.Center;

			await AssertEventually(() => GetNativeAspect(handler.PlatformView!) == Aspect.Center, timeout: 2000, message: $"The new default aspect is not shown in the {interaction} state: {DescribeNativeAspect(handler.PlatformView!)}");
		});
	}

	[Theory]
	[InlineData(ImageKind.Image)]
	[InlineData(ImageKind.ImageButton)]
	public async Task ImageSourceChangesWhileTheBehaviorIsNotAttachedLeaveTheImageAlone(ImageKind kind)
	{
		SetupBuilder();

		var behavior = CreateBehavior();
		var view = CreateImageView(kind);
		SetSource(view, ImageSource.FromFile(changedSource));
		var originalSource = GetSource(view);
		view.Behaviors.Add(behavior);

		// The view is not loaded, so the behavior is not attached to a platform view: it has no element to change.
		behavior.DefaultImageSource = ImageSource.FromFile(pressedSource);
		behavior.HoveredImageSource = ImageSource.FromFile(pressedSource);
		behavior.PressedImageSource = ImageSource.FromFile(defaultSource);

		Assert.Same(originalSource, GetSource(view));

		await AttachAndRun(view, async handler =>
		{
			await view.OnLoadedAsync();
			await AssertEventually(() => ReferenceEquals(behavior.DefaultImageSource, GetSource(view)), timeout: 3000, message: "The behavior never showed its default image source once attached.");

			view.Behaviors.Remove(behavior);

			var sourceWhenDetached = GetSource(view);
			behavior.DefaultImageSource = ImageSource.FromFile(hoveredSource);
			behavior.HoveredImageSource = ImageSource.FromFile(hoveredSource);
			behavior.PressedImageSource = ImageSource.FromFile(hoveredSource);

			Assert.Same(sourceWhenDetached, GetSource(view));
			await WaitForPixelsAsync(handler.PlatformView!, pressedColor);
		});
	}

	[Theory]
	[InlineData(ImageKind.Image)]
	[InlineData(ImageKind.ImageButton)]
	public async Task TouchChangesNothingAfterTheBehaviorDetached(ImageKind kind)
	{
		SetupBuilder();

		var behavior = CreateBehavior();
		var view = CreateImageView(kind);
		view.Behaviors.Add(behavior);

		await AttachAndRun(view, async handler =>
		{
			await view.OnLoadedAsync();
			await AssertShowsAsync(handler, kind, defaultColor, Aspect.AspectFit);

			view.Behaviors.Remove(behavior);
			await TouchAfterDetachAsync(handler, behavior);

			Assert.Equal(TouchState.Default, behavior.CurrentTouchState);
			Assert.Same(behavior.DefaultImageSource, GetSource(view));
			await AssertShowsAsync(handler, kind, defaultColor, Aspect.AspectFit);
		});
	}

	[Theory]
	[InlineData(ImageKind.Image)]
	[InlineData(ImageKind.ImageButton)]
	public async Task TouchChangesTheImageAgainAfterTheBehaviorAttachedAgain(ImageKind kind)
	{
		SetupBuilder();

		var behavior = CreateBehavior();
		var view = CreateImageView(kind);
		view.Behaviors.Add(behavior);

		await AttachAndRun(view, async handler =>
		{
			await view.OnLoadedAsync();
			view.Behaviors.Remove(behavior);
			view.Behaviors.Add(behavior);

			await AssertShowsAsync(handler, kind, defaultColor, Aspect.AspectFit);
			await PressAsync(handler, behavior);
			await AssertShowsAsync(handler, kind, pressedColor, Aspect.Fill);

			await ReleaseAsync(handler, behavior);
			await AssertShowsAsync(handler, kind, defaultColor, Aspect.AspectFit);
		});
	}

	[Fact]
	public async Task PressedImageSourceIsOnlySetWhenTheAnimationEndsWhenRequested()
	{
		SetupBuilder();

		var behavior = CreateBehavior();
		behavior.ShouldSetImageOnAnimationEnd = true;
		behavior.PressedAnimationDuration = 1500;
		var image = CreateImageView(ImageKind.Image);
		image.Behaviors.Add(behavior);

		await AttachAndRun(image, async handler =>
		{
			await image.OnLoadedAsync();
			await AssertShowsAsync(handler, ImageKind.Image, defaultColor, Aspect.AspectFit);

			await PressAsync(handler, behavior);

			Assert.Same(behavior.DefaultImageSource, GetSource(image));

			await AssertEventually(() => ReferenceEquals(behavior.PressedImageSource, GetSource(image)), timeout: 5000, message: "The pressed image was never set.");
			await AssertShowsAsync(handler, ImageKind.Image, pressedColor, Aspect.Fill);
		});
	}

	[Fact]
	public async Task PressedImageSourceIsSetAtOnceWhenTheAnimationEndIsNotRequested()
	{
		SetupBuilder();

		var behavior = CreateBehavior();
		behavior.PressedAnimationDuration = 4000;
		var image = CreateImageView(ImageKind.Image);
		image.Behaviors.Add(behavior);

		await AttachAndRun(image, async handler =>
		{
			await image.OnLoadedAsync();
			await AssertShowsAsync(handler, ImageKind.Image, defaultColor, Aspect.AspectFit);

			await PressAsync(handler, behavior);

			// Long before the end of the pressed animation (4 s).
			await AssertEventually(() => ReferenceEquals(behavior.PressedImageSource, GetSource(image)), timeout: 1500, message: "The pressed image was not set before the animation ended.");
			await AssertShowsAsync(handler, ImageKind.Image, pressedColor, Aspect.Fill);
		});
	}

	[Fact]
	public async Task PendingPressedImageIsDroppedWhenTheTouchEndsBeforeTheAnimationEnds()
	{
		SetupBuilder();

		var behavior = CreateBehavior();
		behavior.ShouldSetImageOnAnimationEnd = true;
		behavior.PressedAnimationDuration = 200;
		var image = CreateImageView(ImageKind.Image);
		image.Behaviors.Add(behavior);

		await AttachAndRun(image, async handler =>
		{
			await image.OnLoadedAsync();
			await AssertShowsAsync(handler, ImageKind.Image, defaultColor, Aspect.AspectFit);

			await PressAsync(handler, behavior);
			await ReleaseAsync(handler, behavior);

			Assert.Equal(TouchState.Default, behavior.CurrentTouchState);

			// The window is longer than the pressed animation (200 ms).
			await VisualBehaviorHelpers.AssertStaysFalseAsync(() => ReferenceEquals(behavior.PressedImageSource, GetSource(image)), "The pressed image was set after the touch ended.", windowMilliseconds: 600);

			Assert.Same(behavior.DefaultImageSource, GetSource(image));
			await AssertShowsAsync(handler, ImageKind.Image, defaultColor, Aspect.AspectFit);
		});
	}

	// Once the behavior has an image source it owns the image of the view, and its built-in default image source is no image at all
	// (ImageTouchBehaviorDefaults.DefaultBackgroundImageSource).
	[Theory]
	[InlineData(ImageKind.Image)]
	[InlineData(ImageKind.ImageButton)]
	public async Task ImageIsRemovedWhenTheBehaviorHasNoDefaultImageSource(ImageKind kind)
	{
		SetupBuilder();

		var behavior = new ImageTouchBehavior { PressedImageSource = pressedSource };
		var view = CreateImageView(kind);
		SetSource(view, ImageSource.FromFile(defaultSource));

		// The view starts with an aspect that is not the built-in one, so the test can tell that the behavior takes over the aspect as well.
		SetAspect(view, Aspect.Fill);
		view.Behaviors.Add(behavior);

		await AttachAndRun(view, async handler =>
		{
			await view.OnLoadedAsync();

			await AssertEventually(() => GetSource(view) is null && !HasNativeImage(handler.PlatformView!) && GetNativeAspect(handler.PlatformView!) == ImageTouchBehaviorDefaults.DefaultBackgroundImageAspect, timeout: 3000, message: $"The view still shows its own image or aspect: {DescribeNativeAspect(handler.PlatformView!)}");

			await PressAsync(handler, behavior);

			await AssertEventually(() => ReferenceEquals(behavior.PressedImageSource, GetSource(view)), timeout: 3000, message: "The pressed image was never set.");
			await AssertShowsAsync(handler, kind, pressedColor, ImageTouchBehaviorDefaults.DefaultBackgroundImageAspect);

			await ReleaseAsync(handler, behavior);

			await AssertEventually(() => GetSource(view) is null && !HasNativeImage(handler.PlatformView!), timeout: 3000, message: $"The pressed image was not removed when the touch ended: {DescribeNativeAspect(handler.PlatformView!)}");

			await HoverEnterAsync(handler, behavior);

			Assert.Equal(HoverState.Hovered, behavior.CurrentHoverState);
			await VisualBehaviorHelpers.AssertStaysFalseAsync(() => GetSource(view) is not null || HasNativeImage(handler.PlatformView!), $"The hovered state shows an image: {DescribeNativeAspect(handler.PlatformView!)}");
			await AssertEventually(() => GetNativeAspect(handler.PlatformView!) == ImageTouchBehaviorDefaults.DefaultBackgroundImageAspect, timeout: 3000, message: $"The hovered state does not show the built-in aspect: {DescribeNativeAspect(handler.PlatformView!)}");
		});
	}

	// Xamarin Community Toolkit's TouchEffect left the image alone when none of its image sources was set (SetBackgroundImageAsync
	// returned early), so an ImageTouchBehavior can be used for its touch animations alone, or with sources that are bound later.
	[Theory]
	[InlineData(ImageKind.Image)]
	[InlineData(ImageKind.ImageButton)]
	public async Task BehaviorWithoutAnyImageSourceKeepsTheImageOfTheView(ImageKind kind)
	{
		SetupBuilder();

		var behavior = new ImageTouchBehavior();
		var view = CreateImageView(kind);
		var source = ImageSource.FromFile(changedSource);
		SetSource(view, source);
		SetAspect(view, Aspect.Fill);
		view.Behaviors.Add(behavior);

		await AttachAndRun(view, async handler =>
		{
			await view.OnLoadedAsync();

			bool LostItsImageOrAspect() => !ReferenceEquals(source, GetSource(view)) || GetNativeAspect(handler.PlatformView!) != Aspect.Fill;

			await AssertShowsAsync(handler, kind, changedColor, Aspect.Fill);
			await VisualBehaviorHelpers.AssertStaysFalseAsync(LostItsImageOrAspect, $"The behavior replaced the view's own image or aspect: source {GetSource(view)?.ToString() ?? "null"}, {DescribeNativeAspect(handler.PlatformView!)}");

			await PressAsync(handler, behavior);
			await ReleaseAsync(handler, behavior);

			await VisualBehaviorHelpers.AssertStaysFalseAsync(LostItsImageOrAspect, $"A touch replaced the view's own image or aspect: source {GetSource(view)?.ToString() ?? "null"}, {DescribeNativeAspect(handler.PlatformView!)}");
			await AssertShowsAsync(handler, kind, changedColor, Aspect.Fill);
		});
	}

	[Theory]
	[InlineData(ImageKind.Image)]
	[InlineData(ImageKind.ImageButton)]
	public async Task BehaviorWithImageAspectsButNoImageSourceKeepsTheImageOfTheView(ImageKind kind)
	{
		SetupBuilder();

		var behavior = new ImageTouchBehavior { DefaultImageAspect = Aspect.AspectFit, PressedImageAspect = Aspect.Fill };
		var view = CreateImageView(kind);
		var source = ImageSource.FromFile(changedSource);
		SetSource(view, source);
		SetAspect(view, Aspect.Center);
		view.Behaviors.Add(behavior);

		await AttachAndRun(view, async handler =>
		{
			await view.OnLoadedAsync();
			await AssertShowsAsync(handler, kind, changedColor, Aspect.AspectFit);
			Assert.Same(source, GetSource(view));

			await PressAsync(handler, behavior);
			await AssertShowsAsync(handler, kind, changedColor, Aspect.Fill);
			Assert.Same(source, GetSource(view));

			await ReleaseAsync(handler, behavior);
			await AssertShowsAsync(handler, kind, changedColor, Aspect.AspectFit);
			Assert.Same(source, GetSource(view));
		});
	}

	// A binding that resolves later sets the image sources after the attach; from then on the behavior owns the image of the view.
	[Theory]
	[InlineData(ImageKind.Image)]
	[InlineData(ImageKind.ImageButton)]
	public async Task ImageSourcesSetAfterTheAttachShowTheSameAspectBeforeAndAfterATouch(ImageKind kind)
	{
		SetupBuilder();

		var behavior = new ImageTouchBehavior();
		var view = CreateImageView(kind);
		SetSource(view, ImageSource.FromFile(changedSource));
		SetAspect(view, Aspect.Fill);
		view.Behaviors.Add(behavior);

		await AttachAndRun(view, async handler =>
		{
			await view.OnLoadedAsync();
			await AssertShowsAsync(handler, kind, changedColor, Aspect.Fill);

			behavior.DefaultImageSource = defaultSource;
			behavior.PressedImageSource = pressedSource;

			Assert.Same(behavior.DefaultImageSource, GetSource(view));
			await AssertShowsAsync(handler, kind, defaultColor, ImageTouchBehaviorDefaults.DefaultBackgroundImageAspect);

			await PressAsync(handler, behavior);
			await AssertEventually(() => ReferenceEquals(behavior.PressedImageSource, GetSource(view)), timeout: 3000, message: "The pressed image was never set.");
			await AssertShowsAsync(handler, kind, pressedColor, ImageTouchBehaviorDefaults.DefaultBackgroundImageAspect);

			await ReleaseAsync(handler, behavior);
			await AssertEventually(() => ReferenceEquals(behavior.DefaultImageSource, GetSource(view)), timeout: 3000, message: "The default image did not come back.");
			await AssertShowsAsync(handler, kind, defaultColor, ImageTouchBehaviorDefaults.DefaultBackgroundImageAspect);
		});
	}

	// GestureManager.SetImageSource swaps the images and skips every TouchBehavior that is not an ImageTouchBehavior.
	[Theory]
	[InlineData(ImageKind.Image)]
	[InlineData(ImageKind.ImageButton)]
	public async Task PlainTouchBehaviorLeavesTheImageSourceAndAspectAlone(ImageKind kind)
	{
		SetupBuilder();

		var behavior = new TouchBehavior();
		var view = CreateImageView(kind);
		SetSource(view, ImageSource.FromFile(changedSource));
		var source = GetSource(view);
		view.Behaviors.Add(behavior);

		await AttachAndRun(view, async handler =>
		{
			await view.OnLoadedAsync();
			await AssertShowsAsync(handler, kind, changedColor, Aspect.AspectFit);

			await HoverEnterAsync(handler, behavior);
			await PressAsync(handler, behavior);

			Assert.Equal(TouchState.Pressed, behavior.CurrentTouchState);

			await VisualBehaviorHelpers.AssertStaysFalseAsync(() => !ReferenceEquals(source, GetSource(view)) || GetNativeAspect(handler.PlatformView!) != Aspect.AspectFit, "A plain TouchBehavior changed the image source or the aspect.");

			await ReleaseAsync(handler, behavior);
			await HoverExitAsync(handler, behavior);

			Assert.Equal(TouchState.Default, behavior.CurrentTouchState);
			Assert.Same(source, GetSource(view));
			await AssertShowsAsync(handler, kind, changedColor, Aspect.AspectFit);
		});
	}

	[Theory]
	[InlineData(typeof(Label))]
	[InlineData(typeof(Button))]
	[InlineData(typeof(BoxView))]
	public async Task AttachingToAViewThatIsNotAnImageThrowsInvalidOperationException([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type viewType)
	{
		SetupBuilder();

		// MAUI would call the behavior from the Loaded event, where an exception ends the app, so the method is called directly.
		var view = (VisualElement)Activator.CreateInstance(viewType)!;
		var handler = (IPlatformViewHandler)await CreateHandlerAsync(view);
		var probe = new ImageTouchBehaviorProbe();

		await InvokeOnMainThreadAsync(() =>
		{
			var exception = Assert.Throws<InvalidOperationException>(() => probe.AttachTo(view, handler.PlatformView!));

			Assert.Equal($"{nameof(ImageTouchBehavior)} can only be attached to an {nameof(Image)} or {nameof(ImageButton)}", exception.Message);
			Assert.Null(probe.Element);
		});
	}

	// The image code also defends against an element that is not an Image or ImageButton. MAUI never attaches the behavior to one (see
	// AttachingToAViewThatIsNotAnImageThrowsInvalidOperationException), so the internal Element is set directly, while IsEnabled is false:
	// the Element setter starts a state update that is not awaited, and a disabled behavior returns from it before it reaches the image.
	[Fact]
	public async Task DefaultImageSourceChangeThrowsNotSupportedExceptionForAnImageViewThatIsNeitherImageNorImageButton()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var behavior = new ImageTouchBehavior { IsEnabled = false };
			behavior.Element = new ImageViewThatIsNotAnImage();

			var exception = Assert.Throws<NotSupportedException>(() => behavior.DefaultImageSource = defaultSource);

			Assert.Equal($"{typeof(ImageViewThatIsNotAnImage)} is not supported by {nameof(ImageTouchBehavior)}", exception.Message);
		});
	}

	[Fact]
	public async Task StateUpdateThrowsInvalidOperationExceptionForAnElementThatIsNotAnImage()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var behavior = new ImageTouchBehavior { IsEnabled = false };
			behavior.Element = new Label();
			behavior.IsEnabled = true;

			var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => behavior.ForceUpdateState(CancellationToken.None));

			Assert.Equal($"{nameof(ImageTouchBehavior)} can only be attached to an {nameof(Image)} or {nameof(ImageButton)}", exception.Message);
		});
	}

	[Fact]
	public async Task StateUpdateThrowsNotSupportedExceptionForAnImageViewThatIsNeitherImageNorImageButton()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			// The source is set before the element: HandleDefaultImageSourceChanged has no element to change yet.
			var behavior = new ImageTouchBehavior { IsEnabled = false, DefaultImageSource = defaultSource };
			behavior.Element = new ImageViewThatIsNotAnImage();
			behavior.IsEnabled = true;

			var exception = await Assert.ThrowsAsync<NotSupportedException>(() => behavior.ForceUpdateState(CancellationToken.None));

			Assert.Equal($"{typeof(ImageViewThatIsNotAnImage)} is not yet supported", exception.Message);
		});
	}

	// GestureManager.SetImageSource has a default arm for a touch and hover state combination it does not know. The state update never
	// reaches it (UpdateVisualState and the animation timing throw first for such a combination), so the private method is called directly.
	[Fact]
	public async Task ImageStateMachineThrowsNotSupportedExceptionForAnUnknownTouchState()
	{
		SetupBuilder();

		var setImageSource = typeof(GestureManager).GetMethod("SetImageSource", BindingFlags.Static | BindingFlags.NonPublic);
		Assert.True(setImageSource is not null, "GestureManager.SetImageSource no longer exists: update this test.");

		await InvokeOnMainThreadAsync(async () =>
		{
			var behavior = new ImageTouchBehavior { IsEnabled = false, DefaultImageSource = defaultSource };
			behavior.Element = new Image();
			const TouchState unknownTouchState = (TouchState)99;

			var exception = await Assert.ThrowsAsync<NotSupportedException>(() => (Task)setImageSource.Invoke(null, [behavior, unknownTouchState, HoverState.Default, TimeSpan.Zero, CancellationToken.None])!);

			Assert.Equal($"The combination of {nameof(TouchState)} {unknownTouchState} and {nameof(HoverState)} {HoverState.Default} is not yet supported", exception.Message);
		});
	}

	[Theory]
	[InlineData(ImageKind.Image)]
	[InlineData(ImageKind.ImageButton)]
	public async Task ViewAndBehaviorAreCollectedAfterTheBehaviorDetaches(ImageKind kind)
	{
		SetupBuilder();

		var (viewReference, behaviorReference, platformViewReference) = await HostTouchedImageAndReleaseItAsync(kind);

		// An Android platform view is collected whenever the Java garbage collector gets to its Java peer, which is not
		// deterministic (it happens just as often without the behavior), so only the managed objects of the toolkit are required.
		await VisualBehaviorHelpers.WaitForGCAsync(CanAssertPlatformViewIsCollected ? [viewReference, behaviorReference, platformViewReference] : [viewReference, behaviorReference]);
	}

	async Task<(WeakReference View, WeakReference Behavior, WeakReference PlatformView)> HostTouchedImageAndReleaseItAsync(ImageKind kind)
	{
		var behavior = CreateBehavior();
		var view = CreateImageView(kind);
		view.Behaviors.Add(behavior);
		WeakReference? platformViewReference = null;

		await AttachAndRun(view, async handler =>
		{
			await view.OnLoadedAsync();
			await AssertShowsAsync(handler, kind, defaultColor, Aspect.AspectFit);
			await PressAsync(handler, behavior);
			await ReleaseAsync(handler, behavior);
			platformViewReference = new WeakReference(handler.PlatformView);
		});

		await view.OnUnloadedAsync();

		await InvokeOnMainThreadAsync(() =>
		{
			view.Behaviors.Remove(behavior);
			((IElementHandler?)view.Handler)?.DisconnectHandler();
			view.Handler = null;
		});

		Assert.NotNull(platformViewReference);
		return (new WeakReference(view), new WeakReference(behavior), platformViewReference);
	}

	static ImageTouchBehavior CreateBehavior(bool hasHoveredSource = true) => new()
	{
		DefaultImageSource = defaultSource,
		HoveredImageSource = hasHoveredSource ? ImageSource.FromFile(hoveredSource) : null,
		PressedImageSource = pressedSource,
		DefaultImageAspect = Aspect.AspectFit,
		HoveredImageAspect = Aspect.AspectFill,
		PressedImageAspect = Aspect.Fill
	};

	static View CreateImageView(ImageKind kind) => kind switch
	{
		ImageKind.Image => new Image { WidthRequest = 40, HeightRequest = 40 },
		ImageKind.ImageButton => new ImageButton { WidthRequest = 40, HeightRequest = 40 },
		_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
	};

	static ImageSource? GetSource(View view) => view switch
	{
		Image image => image.Source,
		ImageButton imageButton => imageButton.Source,
		_ => throw new ArgumentOutOfRangeException(nameof(view), view, null)
	};

	static void SetSource(View view, ImageSource? source)
	{
		switch (view)
		{
			case Image image:
				image.Source = source;
				break;
			case ImageButton imageButton:
				imageButton.Source = source;
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(view), view, null);
		}
	}

	static void SetAspect(View view, Aspect aspect)
	{
		switch (view)
		{
			case Image image:
				image.Aspect = aspect;
				break;
			case ImageButton imageButton:
				imageButton.Aspect = aspect;
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(view), view, null);
		}
	}

	async Task EnterAsync(IPlatformViewHandler handler, TouchBehavior behavior, Interaction interaction)
	{
		if (interaction is Interaction.Hovered or Interaction.HoveredAndPressed)
		{
			await HoverEnterAsync(handler, behavior);
		}

		if (interaction is Interaction.Pressed or Interaction.HoveredAndPressed)
		{
			await PressAsync(handler, behavior);
		}
	}

	// The images load asynchronously.
	async Task AssertShowsAsync(IPlatformViewHandler handler, ImageKind kind, Color color, Aspect aspect)
	{
		await AssertEventually(() => GetNativeAspect(handler.PlatformView!) == aspect, timeout: 3000, message: $"The native {kind} does not show the aspect {aspect}: {DescribeNativeAspect(handler.PlatformView!)}");

		await WaitForPixelsAsync(handler.PlatformView!, color);
	}

	Task WaitForPixelsAsync(PlatformView platformView, Color expected)
	{
		var unexpected = new[] { defaultColor, hoveredColor, pressedColor, changedColor }.Where(color => color != expected).ToArray();

		return VisualBehaviorHelpers.WaitForPixelsAsync(platformView, MauiContext, expected, unexpected);
	}

	sealed class ImageTouchBehaviorProbe : ImageTouchBehavior
	{
		public void AttachTo(VisualElement view, PlatformView platformView) => OnAttachedTo(view, platformView);
	}

	sealed class ImageViewThatIsNotAnImage : View, Microsoft.Maui.IImage
	{
		Aspect Microsoft.Maui.IImage.Aspect => Aspect.AspectFit;

		bool Microsoft.Maui.IImage.IsOpaque => false;

		IImageSource? IImageSourcePart.Source => null;

		bool IImageSourcePart.IsAnimationPlaying => false;

		void IImageSourcePart.UpdateIsLoading(bool isLoading)
		{
		}
	}
}