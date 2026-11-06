using System.Net;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using static CommunityToolkit.Maui.DeviceTests.AssertHelpers;

namespace CommunityToolkit.Maui.DeviceTests;

// GravatarImageSource gets its picture from DownloadStreamAsync. The tests serve the picture from a stub handler, because tests never use the network.
[Category(TestCategory.GravatarImageSource)]
[Collection(TestCollections.DisableParallelization)]
public partial class GravatarImageSourceTests : CommunityToolkitHandlerTestBase
{
	// A 16x16 solid red PNG, embedded so that the tests need neither the network nor an asset file.
	const string redPngBase64 = "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAIAAACQkWg2AAAAFklEQVR42mP4z8BAEmIY1TCqYfhqAACQ+f8B8u7oVwAAAABJRU5ErkJggg==";

	const int pictureSize = 16;

	// A 200x200 solid red PNG: larger than the room a Button has for a picture at any display scale of the test runners.
	const string largeRedPngBase64 = "iVBORw0KGgoAAAANSUhEUgAAAMgAAADICAIAAAAiOjnJAAABcklEQVR42u3SMQ0AAAjAsPk3DSY4OJpUwbKm4JwEGAtjYSwwFsbCWGAsjIWxwFgYC2OBsTAWxgJjYSyMBcbCWBgLjIWxMBYYC2NhLDAWxsJYYCyMhbHAWBgLY4GxMBbGAmNhLIwFxsJYGAuMhbEwFhgLY2EsMBbGwlhgLIyFscBYGAtjgbEwFsYCY2EsjAXGwlgYC4yFsTAWGAtjYSwwFsbCWGAsjIWxwFgYC2OBBBgLY2EsMBbGwlhgLIyFscBYGAtjgbEwFsYCY2EsjAXGwlgYC4yFsTAWGAtjYSwwFsbCWGAsjIWxwFgYC2OBsTAWxgJjYSyMBcbCWBgLjIWxMBYYC2NhLDAWxsJYYCyMhbHAWBgLY4GxMBbGAmNhLIwFxsJYGAuMhbEwFhgLY2EsMBbGwlhgLIyFscBYGAtjgbEwFsbCWBJgLIyFscBYGAtjgbEwFsYCY2EsjAXGwlgYC4yFsTAWGAtjYSwwFsbCWGAs3lnRh6zWL0rapgAAAABJRU5ErkJggg==";

	// Decoding and showing the picture are asynchronous: a generous polling timeout keeps slow CI machines green without hiding a wrong state
	// (the assertion still fails once it is over).
	const int pictureTimeout = 5000;

	static readonly Uri pictureUri = new("https://www.gravatar.com/avatar/b65a519785f69fbe7236dd0fd6396094?s=16&d=mp");

	public enum PictureKind
	{
		Image,
		ImageButton,
		Button,
		AvatarView,
	}

	[Theory]
	[InlineData(PictureKind.Image)]
	[InlineData(PictureKind.ImageButton)]
	[InlineData(PictureKind.Button)]
	[InlineData(PictureKind.AvatarView)]
	public async Task ViewShowsThePictureOfAResponseWhoseBodyCannotSeek(PictureKind kind)
	{
		SetupBuilder();
		using var client = new HttpClient(new PictureHandler());
		var view = CreatePictureView(kind, new StreamImageSource { Stream = cancellationToken => client.DownloadStreamAsync(pictureUri, cancellationToken) });

		await AttachAndRun(view, async handler =>
		{
			await view.OnLoadedAsync();

			await AssertEventually(() => HasPicture(view, handler), timeout: pictureTimeout, message: $"The {kind} never showed the picture.");
		});
	}

	[Theory]
	[InlineData(PictureKind.Image)]
	[InlineData(PictureKind.ImageButton)]
	[InlineData(PictureKind.Button)]
	[InlineData(PictureKind.AvatarView)]
	public async Task ViewKeepsThePictureWhenTheLoadItReplacedIsCanceled(PictureKind kind)
	{
		SetupBuilder();
		var pictureHandler = new FirstRequestHeldHandler();
		using var client = new HttpClient(pictureHandler);
		var source = new StreamImageSource { Stream = cancellationToken => client.DownloadStreamAsync(pictureUri, cancellationToken) };
		var view = CreatePictureView(kind, source);

		await AttachAndRun(view, async handler =>
		{
			await view.OnLoadedAsync();
			await pictureHandler.FirstRequestStarted.WaitAsync(TimeSpan.FromMilliseconds(pictureTimeout));

			// StreamImageSource raises SourceChanged when its Stream changes, which starts a second load and cancels the first.
			source.Stream = cancellationToken => client.DownloadStreamAsync(pictureUri, cancellationToken);
			await AssertEventually(() => HasPicture(view, handler), timeout: pictureTimeout, message: $"The {kind} never showed the picture of the second load.");

			pictureHandler.ReleaseFirstRequest();

			// The load that was replaced ends asynchronously and nothing signals its end, so the test can only wait before it checks.
			await Task.Delay(1000);

			Assert.True(HasPicture(view, handler), $"The {kind} lost its picture when the load it replaced ended.");
		});
	}

	static View CreatePictureView(PictureKind kind, StreamImageSource source) => kind switch
	{
		PictureKind.Image => new Image { Source = source, WidthRequest = 40, HeightRequest = 40 },
		PictureKind.ImageButton => new ImageButton { Source = source, WidthRequest = 40, HeightRequest = 40 },
		PictureKind.Button => new Button { ImageSource = source, WidthRequest = 80, HeightRequest = 50 },
		PictureKind.AvatarView => new AvatarView { ImageSource = source, WidthRequest = 64, HeightRequest = 64 },
		_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
	};

	// An AvatarView shows its picture through the Image that is its Content, and replaces that Image with its text when the picture fails to load.
	static bool HasPicture(View view, IPlatformViewHandler handler) => view switch
	{
		AvatarView { Content: Image { Handler: IPlatformViewHandler imageHandler } } => HasPicture(imageHandler),
		AvatarView => false,
		_ => HasPicture(handler),
	};

	// A real server answers with a stream that cannot seek, which a MemoryStream does not reproduce.
	sealed class PictureHandler(string pngBase64 = redPngBase64) : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
			Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new NonSeekableStream(Convert.FromBase64String(pngBase64))) });
	}

	// The first request waits without observing its token, like a connection that only notices the cancellation late. Later requests are answered at once.
	sealed class FirstRequestHeldHandler : HttpMessageHandler
	{
		readonly TaskCompletionSource firstRequestStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
		readonly TaskCompletionSource firstRequestReleased = new(TaskCreationOptions.RunContinuationsAsynchronously);

		int requestCount;

		public Task FirstRequestStarted => firstRequestStarted.Task;

		public void ReleaseFirstRequest() => firstRequestReleased.TrySetResult();

		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			if (Interlocked.Increment(ref requestCount) == 1)
			{
				firstRequestStarted.TrySetResult();
				await firstRequestReleased.Task;
			}

			return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new NonSeekableStream(Convert.FromBase64String(redPngBase64))) };
		}
	}

	sealed class NonSeekableStream(byte[] content) : Stream
	{
		int position;

		public override bool CanRead => true;

		public override bool CanSeek => false;

		public override bool CanWrite => false;

		public override long Length => throw new NotSupportedException();

		public override long Position
		{
			get => throw new NotSupportedException();
			set => throw new NotSupportedException();
		}

		public override void Flush()
		{
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			var readCount = Math.Min(count, content.Length - position);
			Array.Copy(content, position, buffer, offset, readCount);
			position += readCount;

			return readCount;
		}

		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

		public override void SetLength(long value) => throw new NotSupportedException();

		public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
	}
}