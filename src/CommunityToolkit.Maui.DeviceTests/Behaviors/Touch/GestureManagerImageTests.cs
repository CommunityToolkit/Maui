using System.Diagnostics;
using System.Reflection;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using Xunit.Sdk;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.TouchBehavior)]
[Collection(TestCollections.DisableParallelization)]
public class GestureManagerImageTests : TouchBehaviorTestBase
{
	static readonly BindingFlags anyMember = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

	static ImageSource Red => ImageSource.FromFile("touch_red.png");

	static ImageSource Green => ImageSource.FromFile("touch_green.png");

	static ImageSource Blue => ImageSource.FromFile("touch_blue.png");

	[Fact]
	public async Task ImageSourceAndAspectFollowEachState()
	{
		SetupBuilder();

		var image = new Image { WidthRequest = HostSize, HeightRequest = HostSize };
		var behavior = new ImageTouchBehavior
		{
			DefaultImageSource = Red,
			HoveredImageSource = Green,
			PressedImageSource = Blue,
			DefaultImageAspect = Aspect.AspectFit,
			HoveredImageAspect = Aspect.AspectFill,
			PressedImageAspect = Aspect.Fill
		};

		await RunAttachedAsync(image, behavior, async handler =>
		{
			await AssertImageAsync(image, handler, "touch_red.png", Aspect.AspectFit, Colors.Red, "after attach");

			behavior.HandleHover(HoverStatus.Entered);
			await AssertImageAsync(image, handler, "touch_green.png", Aspect.AspectFill, Colors.Lime, "while hovered");

			SimulatePress(behavior);
			await AssertImageAsync(image, handler, "touch_blue.png", Aspect.Fill, Colors.Blue, "while pressed");

			SimulateRelease(behavior);
			await AssertImageAsync(image, handler, "touch_green.png", Aspect.AspectFill, Colors.Lime, "after the release while hovered");

			behavior.HandleHover(HoverStatus.Exited);
			await AssertImageAsync(image, handler, "touch_red.png", Aspect.AspectFit, Colors.Red, "after the hover ended");
		});
	}

	[Fact]
	public async Task StatesWithoutTheirOwnImageUseTheDefaultImage()
	{
		SetupBuilder();

		var image = new Image { WidthRequest = HostSize, HeightRequest = HostSize };
		var behavior = new ImageTouchBehavior { DefaultImageSource = Red, DefaultImageAspect = Aspect.AspectFill };

		await RunAttachedAsync(image, behavior, async handler =>
		{
			behavior.HandleHover(HoverStatus.Entered);
			await AssertImageAsync(image, handler, "touch_red.png", Aspect.AspectFill, Colors.Red, "while hovered");

			SimulatePress(behavior);
			await AssertImageAsync(image, handler, "touch_red.png", Aspect.AspectFill, Colors.Red, "while pressed");

			SimulateRelease(behavior);
			behavior.HandleHover(HoverStatus.Exited);
		});
	}

	[Fact]
	public async Task SourceAndAspectOfAStateAreIndependent()
	{
		SetupBuilder();

		var image = new Image { WidthRequest = HostSize, HeightRequest = HostSize };
		var behavior = new ImageTouchBehavior
		{
			DefaultImageSource = Red,
			DefaultImageAspect = Aspect.AspectFit,
			PressedImageSource = Blue,
			HoveredImageAspect = Aspect.AspectFill
		};

		await RunAttachedAsync(image, behavior, async handler =>
		{
			SimulatePress(behavior);
			await AssertImageAsync(image, handler, "touch_blue.png", Aspect.AspectFit, Colors.Blue, "while pressed (own source, default aspect)");
			SimulateRelease(behavior);

			behavior.HandleHover(HoverStatus.Entered);
			await AssertImageAsync(image, handler, "touch_red.png", Aspect.AspectFill, Colors.Red, "while hovered (default source, own aspect)");
			behavior.HandleHover(HoverStatus.Exited);
		});
	}

	[Fact]
	public async Task ImageButtonSourceAndAspectFollowTheState()
	{
		SetupBuilder();

		var button = new ImageButton { WidthRequest = HostSize, HeightRequest = HostSize };
		var behavior = new ImageTouchBehavior
		{
			DefaultImageSource = Red,
			PressedImageSource = Blue,
			DefaultImageAspect = Aspect.AspectFit,
			PressedImageAspect = Aspect.AspectFill
		};

		await RunAttachedAsync(button, behavior, async _ =>
		{
			await WaitUntilAsync(() => button.Source is FileImageSource { File: "touch_red.png" } && button.Aspect == Aspect.AspectFit, "The default image was not applied.");

			SimulatePress(behavior);
			await WaitUntilAsync(() => button.Source is FileImageSource { File: "touch_blue.png" } && button.Aspect == Aspect.AspectFill, "The pressed image was not applied.");

			SimulateRelease(behavior);
			await WaitUntilAsync(() => button.Source is FileImageSource { File: "touch_red.png" } && button.Aspect == Aspect.AspectFit, "The default image did not come back.");
		});
	}

	[Fact]
	public async Task ImageIsSetAfterTheAnimationWhenRequested()
	{
		SetupBuilder();

		var image = new Image { WidthRequest = HostSize, HeightRequest = HostSize };
		var behavior = new ImageTouchBehavior
		{
			DefaultImageSource = Red,
			PressedImageSource = Blue,
			ShouldSetImageOnAnimationEnd = true,
			PressedAnimationDuration = 500
		};

		await RunAttachedAsync(image, behavior, async _ =>
		{
			await WaitUntilAsync(() => image.Source is FileImageSource { File: "touch_red.png" }, "The default image was not applied.");

			SimulatePress(behavior);
			Assert.Equal("touch_red.png", Assert.IsType<FileImageSource>(image.Source).File);

			await WaitUntilAsync(() => image.Source is FileImageSource { File: "touch_blue.png" }, "The pressed image was not applied after the animation.", 3000);

			SimulateRelease(behavior);
		});
	}

	[Fact]
	public async Task ImageIsSetImmediatelyWithoutTheOption()
	{
		SetupBuilder();

		var image = new Image { WidthRequest = HostSize, HeightRequest = HostSize };
		var behavior = new ImageTouchBehavior { DefaultImageSource = Red, PressedImageSource = Blue, PressedAnimationDuration = 800 };

		await RunAttachedAsync(image, behavior, async _ =>
		{
			await WaitUntilAsync(() => image.Source is FileImageSource { File: "touch_red.png" }, "The default image was not applied.");

			SimulatePress(behavior);

			await WaitUntilAsync(() => image.Source is FileImageSource { File: "touch_blue.png" }, "The pressed image was not applied at once.", 400);

			SimulateRelease(behavior);
		});
	}

	[Fact]
	public async Task StateChangeBeforeTheAnimationEndedCancelsThePendingImageChange()
	{
		SetupBuilder();

		var image = new Image { WidthRequest = HostSize, HeightRequest = HostSize };
		var behavior = new ImageTouchBehavior
		{
			DefaultImageSource = Red,
			PressedImageSource = Blue,
			ShouldSetImageOnAnimationEnd = true,
			PressedAnimationDuration = 400
		};

		await RunAttachedAsync(image, behavior, async _ =>
		{
			await WaitUntilAsync(() => image.Source is FileImageSource { File: "touch_red.png" }, "The default image was not applied.");

			SimulatePress(behavior);
			SimulateRelease(behavior);

			// Past the end of the cancelled delay.
			await Task.Delay(700);

			Assert.Equal("touch_red.png", Assert.IsType<FileImageSource>(image.Source).File);
		});
	}

	// BlockThreadPool holds the cancellation callbacks back, so the release is still waiting for them when the hover overtakes it.
	// Catches: the overtaking state change disposing the token source whose cancellation the release requested before the callbacks ran
	// (Dispose drops them, so the pending image change is never cancelled).
	[Fact]
	public async Task StateChangeThatOvertakesAReleaseDoesNotLetThePendingPressedImageComeBack()
	{
		SetupBuilder();

		var image = new Image { WidthRequest = HostSize, HeightRequest = HostSize };
		var behavior = new ImageTouchBehavior
		{
			DefaultImageSource = Red,
			HoveredImageSource = Green,
			PressedImageSource = Blue,
			ShouldSetImageOnAnimationEnd = true,
			PressedAnimationDuration = 700
		};

		await RunAttachedAsync(image, behavior, async _ =>
		{
			await WaitUntilAsync(() => image.Source is FileImageSource { File: "touch_red.png" }, "The default image was not applied.");

			for (var round = 1; round <= 3; round++)
			{
				SimulatePress(behavior);
				await Task.Delay(100);

				using (BlockThreadPool())
				{
					SimulateRelease(behavior);
					behavior.HandleHover(HoverStatus.Entered);
				}

				// Past the end of the pressed state's delay.
				await Task.Delay(1200);

				Assert.True(image.Source is FileImageSource { File: "touch_green.png" }, $"Round {round}: the hovered state shows {DescribeSource(image.Source)} instead of the hovered image.");

				behavior.HandleHover(HoverStatus.Exited);
				await WaitUntilAsync(() => image.Source is FileImageSource { File: "touch_red.png" }, $"Round {round}: the default image did not come back after the hover.");
			}
		});
	}

	// Catches: SetImageSource setting the image of its state without checking that its token was cancelled meanwhile.
	[Fact]
	public async Task PendingPressedImageWhoseDelayEndedRightBeforeAStateChangeIsNotSet()
	{
		SetupBuilder();

		var image = new Image { WidthRequest = HostSize, HeightRequest = HostSize };
		var behavior = new ImageTouchBehavior
		{
			DefaultImageSource = Red,
			HoveredImageSource = Green,
			PressedImageSource = Blue,
			ShouldSetImageOnAnimationEnd = true,
			PressedAnimationDuration = 300
		};

		await RunAttachedAsync(image, behavior, async _ =>
		{
			await WaitUntilAsync(() => image.Source is FileImageSource { File: "touch_red.png" }, "The default image was not applied.");

			SimulatePress(behavior);
			await Task.Delay(100);

			// The pressed state's delay ends while the UI thread sleeps: the continuation of SetImageSource waits behind the state changes.
			Thread.Sleep(600);
			SimulateRelease(behavior);
			behavior.HandleHover(HoverStatus.Entered);

			await Task.Delay(500);

			Assert.True(image.Source is FileImageSource { File: "touch_green.png" }, $"The hovered state shows {DescribeSource(image.Source)} instead of the hovered image.");

			behavior.HandleHover(HoverStatus.Exited);
		});
	}

	// BlockThreadPool holds the cancellation callbacks back, so the release is still waiting for them when the detach aborts the animations.
	// Catches: the detach disposing the token source whose cancellation the release requested before the callbacks ran.
	[Fact]
	public async Task BehaviorRemovedRightAfterAReleaseDoesNotSetThePendingPressedImageLater()
	{
		SetupBuilder();

		var image = new Image { WidthRequest = HostSize, HeightRequest = HostSize };
		var behavior = new ImageTouchBehavior
		{
			DefaultImageSource = Red,
			PressedImageSource = Blue,
			ShouldSetImageOnAnimationEnd = true,
			PressedAnimationDuration = 700
		};

		await RunAttachedAsync(image, behavior, async _ =>
		{
			await WaitUntilAsync(() => image.Source is FileImageSource { File: "touch_red.png" }, "The default image was not applied.");

			SimulatePress(behavior);
			await Task.Delay(100);

			using (BlockThreadPool())
			{
				SimulateRelease(behavior);
				image.Behaviors.Remove(behavior);
			}

			// Past the end of the pressed state's delay.
			await Task.Delay(1000);

			Assert.True(image.Source is FileImageSource { File: "touch_red.png" }, $"The removed behavior put {DescribeSource(image.Source)} on the view.");
		});
	}

	// TouchBehavior.Dispose disposes the gesture manager first. BlockThreadPool holds the cancellation callbacks back, so the release is
	// still waiting for them when the gesture manager is disposed.
	// Catches: Dispose disposing the token source whose cancellation the release requested before the callbacks ran.
	[Fact]
	public async Task DisposingTheGestureManagerRightAfterAReleaseDoesNotSetThePendingPressedImageLater()
	{
		SetupBuilder();

		var image = new Image { WidthRequest = HostSize, HeightRequest = HostSize };
		var behavior = new ImageTouchBehavior
		{
			DefaultImageSource = Red,
			PressedImageSource = Blue,
			ShouldSetImageOnAnimationEnd = true,
			PressedAnimationDuration = 700
		};

		await RunAttachedAsync(image, behavior, async _ =>
		{
			await WaitUntilAsync(() => image.Source is FileImageSource { File: "touch_red.png" }, "The default image was not applied.");

			SimulatePress(behavior);
			await Task.Delay(100);

			using (BlockThreadPool())
			{
				SimulateRelease(behavior);
				GetGestureManager(behavior).Dispose();
			}

			// Past the end of the pressed state's delay.
			await Task.Delay(1000);

			Assert.True(image.Source is FileImageSource { File: "touch_red.png" }, $"The disposed gesture manager put {DescribeSource(image.Source)} on the view.");
		});
	}

	// The platform attach refuses every element but Image and ImageButton, so the other elements are handed to the behavior directly.
	// MAUI's Button implements IImage, so this check accepts it; SetImageSource rejects it later.
	[Fact]
	public async Task OnlyImageElementsCanBeServed()
	{
		SetupBuilder();

		var image = new Image { WidthRequest = HostSize, HeightRequest = HostSize };
		var imageBehavior = new ImageTouchBehavior();
		await RunAttachedAsync(image, imageBehavior, _ =>
		{
			Assert.True(GestureManager.TryGetBindableImageTouchBehaviorElement(imageBehavior, out var bindable));
			Assert.Same(image, bindable);

			image.Behaviors.Remove(imageBehavior);

			Assert.False(GestureManager.TryGetBindableImageTouchBehaviorElement(imageBehavior, out var none));
			Assert.Null(none);
		});

		var imageButton = new ImageButton { WidthRequest = HostSize, HeightRequest = HostSize };
		var imageButtonBehavior = new ImageTouchBehavior();
		await RunAttachedAsync(imageButton, imageButtonBehavior, _ =>
		{
			Assert.True(GestureManager.TryGetBindableImageTouchBehaviorElement(imageButtonBehavior, out var bindable));
			Assert.Same(imageButton, bindable);
		});

		var foreign = new ForeignViewImageTouchBehavior();

		foreign.Use(new Label());
		Assert.False(GestureManager.TryGetBindableImageTouchBehaviorElement(foreign, out var label));
		Assert.Null(label);

		foreign.Use(new Button());
		Assert.True(GestureManager.TryGetBindableImageTouchBehaviorElement(foreign, out var button));
		Assert.IsType<Button>(button);
	}

	// Valid attach paths cannot reach these arms: the platform OnAttachedTo of ImageTouchBehavior only accepts Image and ImageButton.
	[Fact]
	public async Task SetImageSourceRejectsElementsItCannotServe()
	{
		SetupBuilder();

		var setImageSource = typeof(GestureManager).GetMethod("SetImageSource", anyMember) ?? throw new MissingMethodException(nameof(GestureManager), "SetImageSource");
		var behavior = new ForeignViewImageTouchBehavior();
		object?[] arguments = [behavior, TouchState.Default, HoverState.Default, TimeSpan.Zero, CancellationToken.None];

		await InvokeOnMainThreadAsync(async () =>
		{
			// A plain TouchBehavior has no image to set: the call must just return.
			arguments[0] = new TouchBehavior();
			await (Task)setImageSource.Invoke(null, arguments)!;

			arguments[0] = behavior;

			behavior.Use(new Label());
			var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => (Task)setImageSource.Invoke(null, arguments)!);
			Assert.Contains("Image", exception.Message);

			// MAUI's Button implements IImage but is neither an Image nor an ImageButton.
			behavior.Use(new Button());
			await Assert.ThrowsAsync<NotSupportedException>(() => (Task)setImageSource.Invoke(null, arguments)!);

			behavior.Use(new ForeignImageView());
			await Assert.ThrowsAsync<NotSupportedException>(() => (Task)setImageSource.Invoke(null, arguments)!);
		});
	}

	async Task AssertImageAsync(Image image, IPlatformViewHandler handler, string expectedFile, Aspect expectedAspect, Color expectedColor, string step)
	{
		await WaitUntilAsync(() => image.Source is FileImageSource source && source.File == expectedFile && image.Aspect == expectedAspect, $"{step}: expected {expectedFile} with {expectedAspect} but the image has {(image.Source as FileImageSource)?.File} with {image.Aspect}.");
		await WaitUntilAsync(() => GetNativeImageAspect(handler) == expectedAspect, $"{step}: the platform image view shows the image with {GetNativeImageAspect(handler)}, expected {expectedAspect}.");

		// The platform decodes the file asynchronously.
		var platformView = GetPlatformView(handler);
		await AssertionExtensions.AssertEventuallyAsync(async () =>
		{
			try
			{
				await platformView.AssertContainsColor(expectedColor, MauiContext);
				return true;
			}
			catch (XunitException)
			{
				return false;
			}
		}, timeout: 5000, interval: 100, message: $"{step}: the platform image view never showed {expectedFile} ({expectedColor.ToArgbHex()}).");
	}

	static string DescribeSource(ImageSource? source) => source switch
	{
		FileImageSource { File: "touch_blue.png" } => "the pressed image",
		FileImageSource { File: "touch_green.png" } => "the hovered image",
		FileImageSource { File: "touch_red.png" } => "the default image",
		null => "no image",
		_ => source.ToString() ?? source.GetType().Name
	};

	// CancelAsync queues the cancellation callbacks to the thread pool, so keeping its workers busy makes the state changes inside the scope
	// run before those callbacks (for 2 s at most).
	static IDisposable BlockThreadPool()
	{
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var started = 0;
		var workers = Math.Max(ThreadPool.ThreadCount, Environment.ProcessorCount) + 4;

		for (var i = 0; i < workers; i++)
		{
			ThreadPool.UnsafeQueueUserWorkItem(_ =>
			{
				Interlocked.Increment(ref started);
				release.Task.Wait(TimeSpan.FromSeconds(2));
			}, null);
		}

		var stopwatch = Stopwatch.StartNew();
		while (Volatile.Read(ref started) < ThreadPool.ThreadCount && stopwatch.ElapsedMilliseconds < 1000)
		{
			Thread.Sleep(1);
		}

		return new ThreadPoolRelease(release);
	}

	sealed class ThreadPoolRelease(TaskCompletionSource release) : IDisposable
	{
		public void Dispose() => release.TrySetResult();
	}

	sealed class ForeignViewImageTouchBehavior : ImageTouchBehavior
	{
		public void Use(VisualElement? element) => View = element;
	}

	sealed class ForeignImageView : View, Microsoft.Maui.IImage
	{
		public Aspect Aspect => Aspect.AspectFit;

		public bool IsOpaque => false;

		public Microsoft.Maui.IImageSource? Source => null;

		public bool IsAnimationPlaying => false;

		public void UpdateIsLoading(bool isLoading)
		{
		}
	}
}