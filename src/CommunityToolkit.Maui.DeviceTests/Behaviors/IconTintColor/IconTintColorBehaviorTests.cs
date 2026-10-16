using System.ComponentModel;
using CommunityToolkit.Maui.Behaviors;
using static CommunityToolkit.Maui.DeviceTests.AssertHelpers;
#if IOS || MACCATALYST
using PlatformView = UIKit.UIView;
#elif ANDROID
using PlatformView = Android.Views.View;
#elif WINDOWS
using PlatformView = Microsoft.UI.Xaml.FrameworkElement;
#endif

namespace CommunityToolkit.Maui.DeviceTests;

// Device tests of IconTintColorBehavior (IconTintColorBehavior.android.cs, .macios.cs, .windows.cs). Every test hosts the view in the
// runner's window, because MAUI attaches a PlatformBehavior when the view is Loaded and detaches it when the view is Unloaded or the
// behavior is removed. The native state is read through the platform partials (IconTintColorBehaviorTests.android/macios/windows.cs).
[Category(TestCategory.IconTintColorBehavior)]
[Collection(TestCollections.DisableParallelization)]
public partial class IconTintColorBehaviorTests : CommunityToolkitHandlerTestBase
{
	// A red disc on a transparent background: a tint replaces the red and keeps the transparent corners.
	const string iconSource = "behavior_disc.png";
	const string greenSource = "behavior_green.png";
	static readonly Color iconColor = Colors.Red;

	public enum IconKind
	{
		Image,
		ImageButton,
		Button
	}

	[Theory]
	[InlineData(IconKind.Image)]
	[InlineData(IconKind.ImageButton)]
	[InlineData(IconKind.Button)]
	public async Task TintIsNotAppliedBeforeTheViewLoads(IconKind kind)
	{
		SetupBuilder();

		var behavior = new CountingIconTintColorBehavior { TintColor = Colors.Blue };
		var view = CreateIconView(kind);
		view.Behaviors.Add(behavior);

		var handler = (IPlatformViewHandler)await CreateHandlerAsync(view);

		await InvokeOnMainThreadAsync(async () =>
		{
			await WaitForNativeImageAsync(handler);

			Assert.Equal(0, behavior.AttachedCount);
			Assert.True(IsNativeUntinted(handler.PlatformView!), DescribeNativeTint(handler.PlatformView!));
		});
	}

	[Theory]
	[InlineData(IconKind.Image)]
	[InlineData(IconKind.ImageButton)]
	[InlineData(IconKind.Button)]
	public async Task TintIsAppliedWhenTheViewLoads(IconKind kind)
	{
		SetupBuilder();

		var behavior = new CountingIconTintColorBehavior { TintColor = Colors.Blue };
		var view = CreateIconView(kind);
		view.Behaviors.Add(behavior);

		await AttachAndRun(view, async handler =>
		{
			await view.OnLoadedAsync();
			await WaitForNativeImageAsync(handler);

			await AssertTintedAsync(handler, kind, Colors.Blue, checkPixels: true);
			Assert.Equal(1, behavior.AttachedCount);
			Assert.Equal(0, behavior.DetachedCount);
			Assert.Same(handler.PlatformView, behavior.AttachedPlatformView);
		});
	}

	[Theory]
	[InlineData(IconKind.Image)]
	[InlineData(IconKind.ImageButton)]
	[InlineData(IconKind.Button)]
	public async Task TintIsAppliedWhenTheBehaviorIsAddedToAnAlreadyLoadedView(IconKind kind)
	{
		SetupBuilder();

		var view = CreateIconView(kind);

		await AttachAndRun(view, async handler =>
		{
			await view.OnLoadedAsync();
			await WaitForNativeImageAsync(handler);
			Assert.True(IsNativeUntinted(handler.PlatformView!), DescribeNativeTint(handler.PlatformView!));

			var behavior = new CountingIconTintColorBehavior { TintColor = Colors.Blue };
			view.Behaviors.Add(behavior);

			// MAUI raises Loaded at once for a late subscriber, so the behavior attaches during Add.
			Assert.Equal(1, behavior.AttachedCount);
			await AssertTintedAsync(handler, kind, Colors.Blue);
		});
	}

	[Theory]
	[InlineData(IconKind.Image)]
	[InlineData(IconKind.ImageButton)]
	[InlineData(IconKind.Button)]
	public async Task TintIsClearedWhenTheBehaviorIsRemovedFromALoadedView(IconKind kind)
	{
		SetupBuilder();

		var behavior = new CountingIconTintColorBehavior { TintColor = Colors.Blue };
		var view = CreateIconView(kind);
		view.Behaviors.Add(behavior);

		await AttachAndRun(view, async handler =>
		{
			await view.OnLoadedAsync();
			await WaitForNativeImageAsync(handler);
			await AssertTintedAsync(handler, kind, Colors.Blue);

			view.Behaviors.Remove(behavior);

			Assert.Equal(1, behavior.DetachedCount);
			await AssertUntintedAsync(handler, kind, checkPixels: true);
		});
	}

	[Theory]
	[InlineData(IconKind.Image)]
	[InlineData(IconKind.ImageButton)]
	[InlineData(IconKind.Button)]
	public async Task TintIsClearedWhenTheViewUnloads(IconKind kind)
	{
		SetupBuilder();

		var behavior = new CountingIconTintColorBehavior { TintColor = Colors.Blue };
		var view = CreateIconView(kind);
		view.Behaviors.Add(behavior);
		IPlatformViewHandler? hostedHandler = null;

		await AttachAndRun(view, async handler =>
		{
			await view.OnLoadedAsync();
			await WaitForNativeImageAsync(handler);
			await AssertTintedAsync(handler, kind, Colors.Blue);
			hostedHandler = handler;
		});

		await view.OnUnloadedAsync();
		await WaitForDetachAsync(behavior, 1);

		Assert.NotNull(hostedHandler);
		Assert.Equal(1, behavior.AttachedCount);
		Assert.Equal(1, behavior.DetachedCount);

		// Native state only: rendering a view that left the window would host it again (and so attach the behavior again).
		await InvokeOnMainThreadAsync(() => AssertEventually(() => IsNativeUntinted(hostedHandler.PlatformView!), timeout: 3000, message: $"The native view is still tinted after it left the window: {DescribeNativeTint(hostedHandler.PlatformView!)}"));
	}

	// Nothing changes while the view is out of the window: only the attach that comes with the next Loaded can tint the view again.
	[Theory]
	[InlineData(IconKind.Image)]
	[InlineData(IconKind.ImageButton)]
	[InlineData(IconKind.Button)]
	public async Task TintIsAppliedAgainWhenTheViewLoadsAgain(IconKind kind)
	{
		SetupBuilder();

		var behavior = new CountingIconTintColorBehavior { TintColor = Colors.Blue };
		var view = CreateIconView(kind);
		view.Behaviors.Add(behavior);
		IPlatformViewHandler? hostedHandler = null;

		await AttachAndRun(view, async handler =>
		{
			await view.OnLoadedAsync();
			await WaitForNativeImageAsync(handler);
			await AssertTintedAsync(handler, kind, Colors.Blue);
			hostedHandler = handler;
		});

		await view.OnUnloadedAsync();
		await WaitForDetachAsync(behavior, 1);

		Assert.NotNull(hostedHandler);
		await InvokeOnMainThreadAsync(() => AssertEventually(() => IsNativeUntinted(hostedHandler.PlatformView!), timeout: 3000, message: $"The native view is still tinted after it left the window: {DescribeNativeTint(hostedHandler.PlatformView!)}"));

		await AttachAndRun(view, async handler =>
		{
			await view.OnLoadedAsync();
			await WaitForNativeImageAsync(handler);

			Assert.Same(hostedHandler.PlatformView, handler.PlatformView);
			Assert.Equal(2, behavior.AttachedCount);
			await AssertTintedAsync(handler, kind, Colors.Blue, checkPixels: true);
		});
	}

	// A TintColor set while the view is out of the window is the one used when the behavior attaches again. Whether that change tints
	// the detached view right away is TintColorChangesAfterTheBehaviorDetachedDoNotTintTheView's subject, so the native tint is reset
	// after the change: only the next attach can tint the view.
	[Theory]
	[InlineData(IconKind.Image)]
	[InlineData(IconKind.ImageButton)]
	[InlineData(IconKind.Button)]
	public async Task TintColorChangedWhileTheViewIsUnloadedIsUsedWhenItLoadsAgain(IconKind kind)
	{
		SetupBuilder();

		var behavior = new CountingIconTintColorBehavior { TintColor = Colors.Blue };
		var view = CreateIconView(kind);
		view.Behaviors.Add(behavior);
		IPlatformViewHandler? hostedHandler = null;

		await AttachAndRun(view, async handler =>
		{
			await view.OnLoadedAsync();
			await WaitForNativeImageAsync(handler);
			await AssertTintedAsync(handler, kind, Colors.Blue);
			hostedHandler = handler;
		});

		await view.OnUnloadedAsync();
		await WaitForDetachAsync(behavior, 1);

		Assert.NotNull(hostedHandler);

		// On the UI thread, like the other TintColor changes: a behavior that still handled it would touch the platform view.
		await InvokeOnMainThreadAsync(() =>
		{
			behavior.TintColor = Colors.Green;
		});

		await InvokeOnMainThreadAsync(() =>
		{
			ResetNativeTint(hostedHandler.PlatformView!);
			Assert.True(IsNativeUntinted(hostedHandler.PlatformView!), DescribeNativeTint(hostedHandler.PlatformView!));
		});

		await AttachAndRun(view, async handler =>
		{
			await view.OnLoadedAsync();
			await WaitForNativeImageAsync(handler);

			Assert.Equal(2, behavior.AttachedCount);
			await AssertTintedAsync(handler, kind, Colors.Green, checkPixels: true);
		});
	}

	[Theory]
	[InlineData(IconKind.Image)]
	[InlineData(IconKind.ImageButton)]
	[InlineData(IconKind.Button)]
	public async Task TintColorChangedBeforeTheViewLoadsIsUsedWhenItLoads(IconKind kind)
	{
		SetupBuilder();

		var behavior = new IconTintColorBehavior { TintColor = Colors.Blue };
		var view = CreateIconView(kind);
		view.Behaviors.Add(behavior);
		behavior.TintColor = Colors.Green;

		await AttachAndRun(view, async handler =>
		{
			await view.OnLoadedAsync();
			await WaitForNativeImageAsync(handler);

			await AssertTintedAsync(handler, kind, Colors.Green, checkPixels: true);
		});
	}

	[Theory]
	[InlineData(IconKind.Image)]
	[InlineData(IconKind.ImageButton)]
	[InlineData(IconKind.Button)]
	public async Task TintFollowsTintColorChangesWhileAttached(IconKind kind)
	{
		SetupBuilder();

		var behavior = new IconTintColorBehavior { TintColor = Colors.Blue };
		var view = CreateIconView(kind);
		view.Behaviors.Add(behavior);

		await AttachAndRun(view, async handler =>
		{
			await view.OnLoadedAsync();
			await WaitForNativeImageAsync(handler);
			await AssertTintedAsync(handler, kind, Colors.Blue, checkPixels: true);

			behavior.TintColor = Colors.Green;

			await AssertTintedAsync(handler, kind, Colors.Green, checkPixels: true);

			behavior.TintColor = Colors.Blue;

			await AssertTintedAsync(handler, kind, Colors.Blue, checkPixels: true);
		});
	}

	[Theory]
	[InlineData(IconKind.Image)]
	[InlineData(IconKind.ImageButton)]
	[InlineData(IconKind.Button)]
	public async Task TintColorSetToNullWhileAttachedClearsTheTint(IconKind kind)
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

			behavior.TintColor = null;

			await AssertUntintedAsync(handler, kind, checkPixels: true);

			behavior.TintColor = Colors.Green;

			await AssertTintedAsync(handler, kind, Colors.Green, checkPixels: true);
		});
	}

	[Theory]
	[InlineData(IconKind.Image)]
	[InlineData(IconKind.ImageButton)]
	[InlineData(IconKind.Button)]
	public async Task BehaviorWithoutTintColorLeavesTheViewUntinted(IconKind kind)
	{
		SetupBuilder();

		var behavior = new CountingIconTintColorBehavior();
		var view = CreateIconView(kind);
		view.Behaviors.Add(behavior);

		await AttachAndRun(view, async handler =>
		{
			await view.OnLoadedAsync();
			await WaitForNativeImageAsync(handler);

			Assert.Equal(1, behavior.AttachedCount);
			await AssertUntintedAsync(handler, kind, checkPixels: true);
		});
	}

	// The behavior unsubscribes from the tint color when it detaches.
	[Theory]
	[InlineData(IconKind.Image)]
	[InlineData(IconKind.ImageButton)]
	[InlineData(IconKind.Button)]
	public async Task TintColorChangesAfterTheBehaviorDetachedDoNotTintTheView(IconKind kind)
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

			view.Behaviors.Remove(behavior);
			await AssertUntintedAsync(handler, kind);

			behavior.TintColor = Colors.Green;

			Assert.True(IsNativeUntinted(handler.PlatformView!), DescribeNativeTint(handler.PlatformView!));
			await AssertUntintedAsync(handler, kind, checkPixels: true);
		});
	}

	[Theory]
	[InlineData(IconKind.Image)]
	[InlineData(IconKind.ImageButton)]
	public async Task TintIsKeptWhenTheImageSourceChanges(IconKind kind)
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
			var imageBefore = GetNativeImageIdentity(handler.PlatformView!);

			// MAUI loads the new image asynchronously and reports it through IsLoading: once it is shown, the tint must cover it again.
			await ChangeImageSourceAndWaitForTheImageAsync(view, greenSource);

			Assert.False(ReferenceEquals(imageBefore, GetNativeImageIdentity(handler.PlatformView!)), $"The view does not show a new image: {DescribeNativeTint(handler.PlatformView!)}");
			await AssertTintedAsync(handler, kind, Colors.Blue);

			if (CanAssertTintPixels)
			{
				await WaitForPixelsAsync(handler.PlatformView!, Colors.Blue, Colors.Lime, iconColor);
			}
		});
	}

	// A Button has no IsLoading to wait for: its native image is replaced by another one once the new ImageSource has loaded.
	[Fact]
	public async Task TintIsKeptWhenTheButtonImageSourceChanges()
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
			var imageBefore = GetNativeImageIdentity(handler.PlatformView!);

			button.ImageSource = greenSource;

			await AssertEventually(() => !ReferenceEquals(imageBefore, GetNativeImageIdentity(handler.PlatformView!)), timeout: 3000, message: $"The button never showed its new image: {DescribeNativeTint(handler.PlatformView!)}");
			await AssertTintedAsync(handler, IconKind.Button, Colors.Blue);

			if (CanAssertButtonTintPixels)
			{
				await WaitForPixelsAsync(handler.PlatformView!, Colors.Blue, Colors.Lime, iconColor);
			}
		});
	}

	[Fact]
	public async Task TintIsAppliedWhenTheImageSourceIsSetAfterTheBehaviorAttached()
	{
		SetupBuilder();

		var behavior = new CountingIconTintColorBehavior { TintColor = Colors.Blue };
		var image = new Image { WidthRequest = 40, HeightRequest = 40 };
		image.Behaviors.Add(behavior);

		await AttachAndRun(image, async handler =>
		{
			await image.OnLoadedAsync();

			// There is no image yet when the behavior attaches.
			Assert.Equal(1, behavior.AttachedCount);
			Assert.False(HasNativeImage(handler.PlatformView!));

			image.Source = iconSource;

			await WaitForNativeImageAsync(handler);
			await AssertTintedAsync(handler, IconKind.Image, Colors.Blue, checkPixels: true);
		});
	}

	// There is no icon to tint (a text-only Button): the behavior has to cope with it when it attaches and when it detaches.
	[Fact]
	public async Task ButtonWithoutAnImageAttachesAndDetachesWithoutAnException()
	{
		SetupBuilder();

		var behavior = new CountingIconTintColorBehavior { TintColor = Colors.Blue };
		var button = new Button { Text = "No icon", WidthRequest = 80, HeightRequest = 50 };
		button.Behaviors.Add(behavior);

		await AttachAndRun(button, async handler =>
		{
			await button.OnLoadedAsync();

			Assert.Equal(1, behavior.AttachedCount);
			Assert.False(HasNativeImage(handler.PlatformView!), DescribeNativeTint(handler.PlatformView!));

			var exception = Record.Exception(() => button.Behaviors.Remove(behavior));

			Assert.Null(exception);
			Assert.Equal(1, behavior.DetachedCount);
			Assert.False(HasNativeImage(handler.PlatformView!), DescribeNativeTint(handler.PlatformView!));
		});
	}

	[Fact]
	public async Task UnsupportedPlatformViewThrowsNotSupportedExceptionWhenTheBehaviorAttaches()
	{
		SetupBuilder();

		var label = new Label { Text = "Not an icon" };
		var handler = (IPlatformViewHandler)await CreateHandlerAsync(label);
		var probe = new IconTintColorBehaviorProbe { TintColor = Colors.Blue };

		await InvokeOnMainThreadAsync(() =>
		{
			var exception = Assert.Throws<NotSupportedException>(() => probe.AttachTo(label, handler.PlatformView!));

			Assert.Contains(nameof(IconTintColorBehavior), exception.Message, StringComparison.Ordinal);
		});
	}

	[Fact]
	public async Task BehaviorAndViewAreCollectedAfterTheBehaviorDetaches()
	{
		SetupBuilder();

		var (viewReference, behaviorReference, platformViewReference) = await HostTintedImageAndReleaseItAsync(new IconTintColorBehavior { TintColor = Colors.Blue });

		// An Android platform view is collected whenever the Java garbage collector gets to its Java peer, which is not
		// deterministic (it happens just as often without the behavior), so only the managed objects of the toolkit are required.
		await WaitForGCAsync(CanAssertPlatformViewIsCollected ? [viewReference, behaviorReference, platformViewReference] : [viewReference, behaviorReference]);
	}

	[Fact]
	public async Task ViewIsCollectedWhileTheDetachedBehaviorIsStillReferenced()
	{
		SetupBuilder();

		var behavior = new IconTintColorBehavior { TintColor = Colors.Blue };
		var (viewReference, _, _) = await HostTintedImageAndReleaseItAsync(behavior);

		await WaitForGCAsync(viewReference);

		GC.KeepAlive(behavior);
	}

	async Task<(WeakReference View, WeakReference Behavior, WeakReference PlatformView)> HostTintedImageAndReleaseItAsync(IconTintColorBehavior behavior)
	{
		var image = CreateIconView(IconKind.Image);
		image.Behaviors.Add(behavior);
		WeakReference? platformViewReference = null;

		await AttachAndRun(image, async handler =>
		{
			await image.OnLoadedAsync();
			await WaitForNativeImageAsync(handler);
			await AssertTintedAsync(handler, IconKind.Image, Colors.Blue);
			platformViewReference = new WeakReference(handler.PlatformView);
		});

		await image.OnUnloadedAsync();

		await InvokeOnMainThreadAsync(() =>
		{
			image.Behaviors.Remove(behavior);
			((IElementHandler?)image.Handler)?.DisconnectHandler();
			image.Handler = null;
		});

		Assert.NotNull(platformViewReference);
		return (new WeakReference(image), new WeakReference(behavior), platformViewReference);
	}

	static View CreateIconView(IconKind kind) => kind switch
	{
		IconKind.Image => new Image { Source = iconSource, WidthRequest = 40, HeightRequest = 40 },
		IconKind.ImageButton => new ImageButton { Source = iconSource, WidthRequest = 40, HeightRequest = 40 },
		IconKind.Button => new Button { ImageSource = iconSource, WidthRequest = 80, HeightRequest = 50 },
		_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
	};

	static void SetIconSource(View view, string source)
	{
		switch (view)
		{
			case Image image:
				image.Source = source;
				break;
			case ImageButton imageButton:
				imageButton.Source = source;
				break;
			case Button button:
				button.ImageSource = source;
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(view), view, null);
		}
	}

	// Changes the source of an Image or ImageButton and returns once MAUI reports that the new image has loaded (IsLoading goes true,
	// then false). The behavior subscribed to the view before this method did, so it has handled the change when this returns.
	static async Task ChangeImageSourceAndWaitForTheImageAsync(View view, string source)
	{
		var imageLoaded = new TaskCompletionSource();
		var isLoading = false;

		void OnViewPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName != nameof(Image.IsLoading))
			{
				return;
			}

			var nowLoading = view switch
			{
				Image image => image.IsLoading,
				ImageButton imageButton => imageButton.IsLoading,
				_ => throw new ArgumentOutOfRangeException(nameof(view), view, null)
			};

			if (nowLoading)
			{
				isLoading = true;
			}
			else if (isLoading)
			{
				imageLoaded.TrySetResult();
			}
		}

		view.PropertyChanged += OnViewPropertyChanged;

		try
		{
			SetIconSource(view, source);

			await imageLoaded.Task.WaitAsync(TimeSpan.FromSeconds(5));
		}
		finally
		{
			view.PropertyChanged -= OnViewPropertyChanged;
		}
	}

	// The behavior detaches when MAUI raises Unloaded, which can be a moment after the view reports that it is no longer loaded.
	static Task WaitForDetachAsync(CountingIconTintColorBehavior behavior, int expectedDetachCount) =>
		AssertEventually(() => behavior.DetachedCount == expectedDetachCount, timeout: 3000, message: $"The behavior never detached after the view unloaded (DetachedCount {behavior.DetachedCount}).");

	static Task WaitForNativeImageAsync(IPlatformViewHandler handler) =>
		AssertEventually(() => HasNativeImage(handler.PlatformView!), timeout: 3000, message: $"The platform view never showed its image: {DescribeNativeTint(handler.PlatformView!)}");

	// The native state says the tint is applied. With checkPixels (Image, ImageButton), the rendered pixels also show the tint
	// instead of the icon's own color: the native state of an Android ImageView does not tell which color the filter has.
	async Task AssertTintedAsync(IPlatformViewHandler handler, IconKind kind, Color tint, bool checkPixels = false)
	{
		await AssertEventually(() => IsNativeTinted(handler.PlatformView!, tint), timeout: 3000, message: $"The native view is not tinted {tint}: {DescribeNativeTint(handler.PlatformView!)}");

		if (checkPixels && kind is not IconKind.Button && CanAssertTintPixels)
		{
			await WaitForPixelsAsync(handler.PlatformView!, tint, iconColor);
		}
	}

	// The native state says there is no tint. With checkPixels (Image, ImageButton), the rendered pixels also show the icon's own
	// color again.
	async Task AssertUntintedAsync(IPlatformViewHandler handler, IconKind kind, bool checkPixels = false)
	{
		await AssertEventually(() => IsNativeUntinted(handler.PlatformView!), timeout: 3000, message: $"The native view is still tinted: {DescribeNativeTint(handler.PlatformView!)}");

		if (checkPixels && kind is not IconKind.Button && CanAssertTintPixels)
		{
			await WaitForPixelsAsync(handler.PlatformView!, iconColor, Colors.Blue, Colors.Green);
		}
	}

	Task WaitForPixelsAsync(PlatformView platformView, Color expected, params Color[] unexpected) =>
		VisualBehaviorHelpers.WaitForPixelsAsync(platformView, MauiContext, expected, unexpected);

	static Task WaitForGCAsync(params WeakReference[] references) => VisualBehaviorHelpers.WaitForGCAsync(references);

	sealed class CountingIconTintColorBehavior : IconTintColorBehavior
	{
		public int AttachedCount { get; private set; }

		public int DetachedCount { get; private set; }

		public PlatformView? AttachedPlatformView { get; private set; }

		protected override void OnAttachedTo(View bindable, PlatformView platformView)
		{
			base.OnAttachedTo(bindable, platformView);
			AttachedCount++;
			AttachedPlatformView = platformView;
		}

		protected override void OnDetachedFrom(View bindable, PlatformView platformView)
		{
			base.OnDetachedFrom(bindable, platformView);
			DetachedCount++;
		}
	}

	// Calls the protected attach/detach methods directly, e.g. for platform views that MAUI would never hand to the behavior
	// without throwing inside the Loaded event.
	sealed partial class IconTintColorBehaviorProbe : IconTintColorBehavior
	{
		public void AttachTo(View view, PlatformView platformView) => OnAttachedTo(view, platformView);

		public void DetachFrom(View view, PlatformView platformView) => OnDetachedFrom(view, platformView);
	}
}