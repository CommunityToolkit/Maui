using System.Reflection;
using AVFoundation;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Views;
using Microsoft.Extensions.Logging;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

// MediaManager.macios.cs SetPoster: audio-only media can show the MetadataArtworkUrl image in the player's content overlay.
// The UIKit view hierarchy of AVPlayerViewController.ContentOverlayView is what the tests read. Before #3281 the source update adds the
// poster; since #3281 the handler of the item's ReadyToPlay status adds it right after MediaOpened, so the tests wait for the media to open.
[Category(TestCategory.MediaElementApple)]
[Collection(TestCollections.DisableParallelization)]
public class MediaManagerPosterAppleTests : AppleMediaElementTestBase
{
	static UIView Overlay(AppleMediaElementHandler handler) => handler.Manager!.NativePlayerViewController!.ContentOverlayView!;

	/// <summary>Runs SetPoster (before #3281: SetPoster(), which reads PlayerItem; since #3281: SetPoster(in AVPlayerItem)).</summary>
	static async Task SetPosterAsync(MediaManager manager, AVPlayerItem item)
	{
		var setPoster = typeof(MediaManager).GetMethod("SetPoster", BindingFlags.NonPublic | BindingFlags.Instance)!;
		if (setPoster.Invoke(manager, setPoster.GetParameters().Length == 0 ? null : [item]) is Task task)
		{
			await task;
		}
	}

	void AssertNothingWasLoggedAsAnError() => Assert.DoesNotContain(Logs.Entries, entry => entry.Level is LogLevel.Error);

	[Fact]
	public async Task AudioWithArtworkShowsAPosterInTheContentOverlay()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.MetadataArtworkUrl = BundleFileUrl("poster", "png");

		await HostAsync(mediaElement, async handler =>
		{
			await LoadAsync(mediaElement, handler, ResourceSource(AudioResource));

			var overlay = Overlay(handler);
			var poster = Assert.IsType<UIImageView>(Assert.Single(overlay.Subviews));
			Assert.Equal((40, 30), (poster.Image!.Size.Width, poster.Image.Size.Height));
			Assert.Equal(UIViewContentMode.ScaleAspectFit, poster.ContentMode);
			Assert.True(poster.ClipsToBounds);
			Assert.False(poster.TranslatesAutoresizingMaskIntoConstraints);

			// Centered in the overlay and never larger than it...
			var constraints = overlay.Constraints.Where(constraint => constraint.FirstItem == poster).ToList();
			Assert.All(constraints, constraint => Assert.True(constraint.Active));
			Assert.Equivalent(
				new[]
				{
					(NSLayoutAttribute.CenterX, NSLayoutRelation.Equal),
					(NSLayoutAttribute.CenterY, NSLayoutRelation.Equal),
					(NSLayoutAttribute.Width, NSLayoutRelation.LessThanOrEqual),
					(NSLayoutAttribute.Height, NSLayoutRelation.LessThanOrEqual)
				},
				constraints.Select(constraint => (constraint.FirstAttribute, constraint.Relation)));

			// ...and keeping the aspect ratio of the image (40 x 30).
			var aspectRatio = Assert.Single(poster.Constraints, constraint => constraint.SecondItem is not null);
			Assert.Equal((NSLayoutAttribute.Width, NSLayoutAttribute.Height, NSLayoutRelation.Equal), (aspectRatio.FirstAttribute, aspectRatio.SecondAttribute, aspectRatio.Relation));
			Assert.Equal(40.0 / 30.0, (double)aspectRatio.Multiplier, precision: 3);
		});
	}

	[Fact]
	public async Task VideoWithArtworkShowsNoPoster()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.MetadataArtworkUrl = BundleFileUrl("poster", "png");

		await HostAsync(mediaElement, async handler =>
		{
			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));

			Assert.Empty(Overlay(handler).Subviews);
		});
	}

	[Fact]
	public async Task AudioWithEmptyArtworkUrlShowsNoPoster()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.MetadataArtworkUrl = string.Empty;

		await HostAsync(mediaElement, async handler =>
		{
			await LoadAsync(mediaElement, handler, ResourceSource(AudioResource));

			Assert.Empty(Overlay(handler).Subviews);
		});
	}

	[Fact]
	public async Task AudioWithoutArtworkUrlShowsNoPoster()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.MetadataArtworkUrl = null!;

		await HostAsync(mediaElement, async handler =>
		{
			await LoadAsync(mediaElement, handler, ResourceSource(AudioResource));

			Assert.Empty(Overlay(handler).Subviews);
		});
	}

	[Fact]
	public async Task ChangingTheSourceRemovesThePreviousPoster()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.MetadataArtworkUrl = BundleFileUrl("poster", "png");

		await HostAsync(mediaElement, async handler =>
		{
			await LoadAsync(mediaElement, handler, ResourceSource(AudioResource));
			Assert.Single(Overlay(handler).Subviews);

			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));

			Assert.Empty(Overlay(handler).Subviews);
		});
	}

	[Fact]
	public async Task ReloadingTheSameAudioKeepsASinglePoster()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.MetadataArtworkUrl = BundleFileUrl("poster", "png");

		await HostAsync(mediaElement, async handler =>
		{
			await LoadAsync(mediaElement, handler, ResourceSource(AudioResource));
			var firstPoster = Assert.Single(Overlay(handler).Subviews);

			await LoadAsync(mediaElement, handler, ResourceSource(AudioResource));

			var poster = Assert.Single(Overlay(handler).Subviews);
			Assert.NotSame(firstPoster, poster);
		});
	}

	[Fact]
	public async Task PosterIsSkippedWhileThereIsNoPlayerViewController()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.MetadataArtworkUrl = BundleFileUrl("poster", "png");

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			var controller = manager.NativePlayerViewController;
			manager.NativePlayerViewController = null;

			var exception = await SetSourceAsync(mediaElement, handler, ResourceSource(AudioResource));

			manager.NativePlayerViewController = controller;
			Assert.Null(exception);
			Assert.Empty(controller!.ContentOverlayView!.Subviews);
			Assert.NotNull(manager.NativePlayerItem);
			AssertNothingWasLoggedAsAnError();
		});
	}

	[Fact]
	public async Task PosterIsSkippedWhenThePlayerViewControllerHasNoView()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.MetadataArtworkUrl = BundleFileUrl("poster", "png");

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			var controller = manager.NativePlayerViewController;
			var viewless = new AppleViewlessPlayerViewController();
			manager.NativePlayerViewController = viewless;

			var exception = await SetSourceAsync(mediaElement, handler, ResourceSource(AudioResource));

			manager.NativePlayerViewController = controller;
			Assert.Null(exception);
			Assert.Empty(viewless.ContentOverlayView?.Subviews ?? []);
			Assert.Empty(controller!.ContentOverlayView!.Subviews);
			Assert.NotNull(manager.NativePlayerItem);
			AssertNothingWasLoggedAsAnError();
		});
	}

	[Fact]
	public async Task PosterIsSkippedWhenThePlayerViewControllerHasNoContentOverlayView()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.MetadataArtworkUrl = BundleFileUrl("poster", "png");

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			var controller = manager.NativePlayerViewController;
			var overlayless = new AppleOverlaylessPlayerViewController();
			manager.NativePlayerViewController = overlayless;

			var exception = await SetSourceAsync(mediaElement, handler, ResourceSource(AudioResource));

			manager.NativePlayerViewController = controller;
			Assert.Null(exception);
			Assert.Empty(controller!.ContentOverlayView!.Subviews);
			Assert.NotNull(manager.NativePlayerItem);
			AssertNothingWasLoggedAsAnError();
		});
	}

	[Fact]
	public async Task PosterIsSkippedWhileThereIsNoMetadata()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.MetadataArtworkUrl = BundleFileUrl("poster", "png");

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			await LoadAsync(mediaElement, handler, ResourceSource(AudioResource));
			Overlay(handler).Subviews.Single().RemoveFromSuperview();
			typeof(MediaManager).GetField("metaData", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(manager, null);

			// An update creates the Metadata before it asks for the poster; calling SetPoster alone has none.
			await SetPosterAsync(manager, manager.NativePlayerItem!);

			Assert.Empty(Overlay(handler).Subviews);
		});
	}

	[Fact]
	public async Task MediaWithUndecodableArtworkIsStillOpened()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);
		var path = Path.Combine(Path.GetTempPath(), $"not_an_image_{Guid.NewGuid():N}.png");
		File.WriteAllText(path, "This is not an image.");
		mediaElement.MetadataArtworkUrl = new Uri(path).AbsoluteUri;

		try
		{
			await HostAsync(mediaElement, async handler =>
			{
				// The poster is the last step of opening; the media is opened before it (UndecodableArtworkDoesNotFailTheSourceUpdate below).
				await SetSourceAsync(mediaElement, handler, ResourceSource(AudioResource));

				Assert.Equal(1, events.Opened);
				Assert.Empty(events.Failures);
				Assert.NotNull(handler.Manager!.NativePlayerItem);
			});
		}
		finally
		{
			File.Delete(path);
		}
	}

	[Fact]
	public async Task PosterIsSkippedWhenTheSourceIsClearedWhileTheAudioIsStillLoading()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.MetadataArtworkUrl = BundleFileUrl("poster", "png");

		await HostAsync(mediaElement, async handler =>
		{
			var manager = handler.Manager!;
			await manager.TakeSourceUpdates();

			// The first update suspends while it loads the tracks; the second one removes the item before it resumes.
			mediaElement.Source = ResourceSource(AudioResource);
			mediaElement.Source = null;
			await manager.TakeSourceUpdates();
			await DrainMainQueueAsync();

			Assert.Null(manager.NativePlayerItem);
			Assert.Empty(Overlay(handler).Subviews);
		});
	}

	[Fact]
	public async Task QuickSourceChangesLeaveNoStalePoster()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.MetadataArtworkUrl = BundleFileUrl("poster", "png");

		await HostAsync(mediaElement, async handler =>
		{
			await handler.Manager!.TakeSourceUpdates();

			// Two audio sources in one main-thread turn: the first update is still loading the tracks when the second one starts.
			var opened = WhenOpenedOrFailedAsync(mediaElement);
			mediaElement.Source = ResourceSource(AudioResource);
			mediaElement.Source = ResourceSource(RootAudioResource);
			await handler.Manager.TakeSourceUpdates();
			await WaitAsync(opened, "MediaOpened was not raised.");
			await DrainMainQueueAsync();

			Assert.Single(Overlay(handler).Subviews);

			await LoadAsync(mediaElement, handler, ResourceSource(VideoResource));

			Assert.Empty(Overlay(handler).Subviews);
		});
	}

	[Fact]
	public async Task UnreadableArtworkUrlDoesNotFailTheSourceUpdate()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		mediaElement.MetadataArtworkUrl = "file:///avplayer/does_not_exist.png";

		await HostAsync(mediaElement, async handler =>
		{
			var exception = await SetSourceAsync(mediaElement, handler, ResourceSource(AudioResource));

			Assert.Null(exception);
			AssertNothingWasLoggedAsAnError();
		});
	}

	[Fact]
	public async Task UndecodableArtworkDoesNotFailTheSourceUpdate()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var path = Path.Combine(Path.GetTempPath(), $"not_an_image_{Guid.NewGuid():N}.png");
		File.WriteAllText(path, "This is not an image.");
		mediaElement.MetadataArtworkUrl = new Uri(path).AbsoluteUri;

		try
		{
			await HostAsync(mediaElement, async handler =>
			{
				var exception = await SetSourceAsync(mediaElement, handler, ResourceSource(AudioResource));

				Assert.Null(exception);
				AssertNothingWasLoggedAsAnError();
			});
		}
		finally
		{
			File.Delete(path);
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task UnusableArtworkShowsNoPoster(bool artworkFileExists)
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var path = Path.Combine(Path.GetTempPath(), $"not_an_image_{Guid.NewGuid():N}.png");
		if (artworkFileExists)
		{
			File.WriteAllText(path, "This is not an image.");
		}

		mediaElement.MetadataArtworkUrl = new Uri(path).AbsoluteUri;

		try
		{
			await HostAsync(mediaElement, async handler =>
			{
				// An artwork URL that cannot be read, or a file that is not an image: there is no image to show.
				await LoadAsync(mediaElement, handler, ResourceSource(AudioResource));
				await DrainMainQueueAsync();

				Assert.Empty(Overlay(handler).Subviews);
			});
		}
		finally
		{
			File.Delete(path);
		}
	}
}