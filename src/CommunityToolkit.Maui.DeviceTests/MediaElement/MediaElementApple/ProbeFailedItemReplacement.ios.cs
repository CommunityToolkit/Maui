using System.Diagnostics;
using System.Text;
using AVFoundation;
using Foundation;
using AVKit;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Maui.DeviceTests.Runners;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

public static partial class TestCategory
{
	public const string R5Probe = nameof(R5Probe);
}

// TEMPORARY probe (r5): measures how an AVPlayer behaves when a valid item follows a failed one. Every case ends in Assert.Fail so the
// numbers land in the xunit XML. Remove once the cause of ReplacingTheFailedSourceOpensTheNewMedia on iOS 15 is known.
[Category(TestCategory.R5Probe)]
[Collection(TestCollections.DisableParallelization)]
public class ProbeFailedItemReplacement : AppleMediaElementTestBase
{
	static string ValidPath()
	{
		var path = Path.Combine(NSBundle.MainBundle.BundlePath, VideoResource);
		return File.Exists(path) ? path : throw new FileNotFoundException(path);
	}

	static string Describe(AVPlayer player, AVPlayerItem item) =>
		$"player {player.Status} '{player.Error?.LocalizedDescription}', item {item.Status} '{item.Error?.LocalizedDescription}', current is item {ReferenceEquals(player.CurrentItem, item)}";

	static async Task<string> CorruptThenValidAsync(bool clearBetween, bool assignViewController, bool freshPlayer)
	{
		var report = new StringBuilder();
		var corruptPath = Path.Combine(Path.GetTempPath(), $"corrupt_{Guid.NewGuid():N}.mp4");
		File.WriteAllText(corruptPath, "This is not a media file.");
		try
		{
			await MainThread.InvokeOnMainThreadAsync(() => { });
			var player = new AVPlayer();
			AVPlayerViewController? controller = null;
			if (assignViewController)
			{
				controller = new AVPlayerViewController { Player = player };
			}

			var corruptItem = AVPlayerItem.FromUrl(NSUrl.FromFilename(corruptPath));
			player.ReplaceCurrentItemWithPlayerItem(corruptItem);
			var stopwatch = Stopwatch.StartNew();
			while (corruptItem.Status is AVPlayerItemStatus.Unknown && stopwatch.ElapsedMilliseconds < 5000)
			{
				await Task.Delay(20);
			}

			report.Append($"corrupt after {stopwatch.ElapsedMilliseconds} ms: {Describe(player, corruptItem)}; ");

			if (clearBetween)
			{
				player.ReplaceCurrentItemWithPlayerItem(null);
				await DrainMainQueueAsync();
			}

			var validPlayer = player;
			if (freshPlayer)
			{
				validPlayer = new AVPlayer();
				controller = assignViewController ? new AVPlayerViewController { Player = validPlayer } : null;
			}

			var asset = AVAsset.FromUrl(NSUrl.FromFilename(ValidPath()));
			var validItem = AVPlayerItem.FromAsset(asset);
			validPlayer.ReplaceCurrentItemWithPlayerItem(validItem);
			stopwatch.Restart();
			while (validItem.Status is AVPlayerItemStatus.Unknown && stopwatch.ElapsedMilliseconds < 20000)
			{
				await Task.Delay(20);
			}

			report.Append($"valid after {stopwatch.ElapsedMilliseconds} ms: {Describe(validPlayer, validItem)}, asset playable {asset.Playable}");
			GC.KeepAlive(controller);
		}
		finally
		{
			File.Delete(corruptPath);
		}

		return report.ToString();
	}

	[Fact]
	public async Task R1_SamePlayerCorruptThenValid() =>
		Assert.Fail("R1: " + await CorruptThenValidAsync(clearBetween: false, assignViewController: false, freshPlayer: false));

	[Fact]
	public async Task R2_SamePlayerClearedBetween() =>
		Assert.Fail("R2: " + await CorruptThenValidAsync(clearBetween: true, assignViewController: false, freshPlayer: false));

	[Fact]
	public async Task R3_FreshPlayerForTheValidItem() =>
		Assert.Fail("R3: " + await CorruptThenValidAsync(clearBetween: false, assignViewController: false, freshPlayer: true));

	[Fact]
	public async Task R4_SamePlayerInAViewController() =>
		Assert.Fail("R4: " + await CorruptThenValidAsync(clearBetween: false, assignViewController: true, freshPlayer: false));

	[Fact]
	public async Task R5_MediaElementWith20SecondWait()
	{
		SetupApple();
		var mediaElement = CreateMediaElement();
		var events = new MediaElementEvents(mediaElement);
		var path = Path.Combine(Path.GetTempPath(), $"corrupt_{Guid.NewGuid():N}.mp4");
		File.WriteAllText(path, "This is not a media file.");
		var report = string.Empty;

		try
		{
			await HostAsync(mediaElement, async handler =>
			{
				mediaElement.Source = MediaSource.FromFile(path);
				await Record.ExceptionAsync(handler.Manager!.TakeSourceUpdates);
				await WaitUntilAsync(() => events.Failures.Count > 0, "MediaFailed was not raised.");

				mediaElement.Source = ResourceSource(VideoResource);
				await handler.Manager.TakeSourceUpdates();
				var stopwatch = Stopwatch.StartNew();
				while (events.Opened == 0 && stopwatch.ElapsedMilliseconds < 20000)
				{
					await Task.Delay(20);
				}

				var player = handler.Manager.NativePlayer!;
				var item = handler.Manager.NativePlayerItem!;
				report = $"opened {events.Opened} after {stopwatch.ElapsedMilliseconds} ms: {Describe(player, item)}";
			});
		}
		finally
		{
			File.Delete(path);
		}

		Assert.Fail("R5: " + report);
	}
}
