using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Text;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.ImageSources;
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

	// Gravatar answers with a picture of the requested size, and a Button sizes itself to its picture: a picture requested at the Button's
	// laid-out size made the Button larger and requested a larger picture each time, until the Button was as wide as its layout.
	[Fact]
	public async Task ButtonDoesNotKeepGrowingForItsGravatarPicture()
	{
		SetupBuilder();
		var requestedSizes = new ConcurrentQueue<int>();
		var source = new GravatarImageSource { Email = "a@example.org" };

		// Like the default Stream function, the stub reads the Uri when the picture is requested.
		source.Stream = _ =>
		{
			var size = GetRequestedSize(source.Uri);
			requestedSizes.Enqueue(size);
			return Task.FromResult<Stream>(new MemoryStream(CreateSquarePng(size)));
		};

		var button = new Button { Text = "Button", ImageSource = source };
		var reference = new Button { Text = "Button" };
		var layout = new VerticalStackLayout { WidthRequest = 300, Children = { reference, button } };

		await AttachAndRun(layout, async handler =>
		{
			try
			{
				await layout.OnLoadedAsync();

				// On Windows, a view that is not loaded cannot be measured by InitializeViewHandler, and no MAUI parent lays this layout out later.
				if (!await AssertHelpers.Wait(() => button.Width > 0 && button.Height > 0, timeout: pictureTimeout))
				{
					var size = ((IView)layout).Measure(layout.WidthRequest, double.PositiveInfinity);
					((IView)layout).Arrange(new Rect(0, 0, size.Width, size.Height));
				}

				await AssertEventually(() => button.Height > 0 && reference.Height > 0 && button.Handler is IPlatformViewHandler buttonHandler && HasAnyPicture(buttonHandler), timeout: pictureTimeout, message: "The Button never showed a picture.");

				// The source waits 737 ms after a size change before it asks for a new picture, so a Button that keeps growing asks for one in every window.
				await Task.Delay(TimeSpan.FromMilliseconds(737 * 3));
				var settledHeight = button.Height;
				var settledRequestCount = requestedSizes.Count;
				await Task.Delay(TimeSpan.FromMilliseconds(737 * 2));

				var message = $"Requested sizes {string.Join(", ", requestedSizes)}; the Button is {button.Width}x{button.Height} DIP, {settledHeight} DIP a moment before, the same Button without a picture {reference.Height} DIP.";
				Assert.True(requestedSizes.Count == settledRequestCount, message);
				Assert.True(Math.Abs(button.Height - settledHeight) <= 0.5, message);
				Assert.True(button.Height <= reference.Height + GravatarImageSourceDefaults.GravatarSize + 1, message);
			}
			finally
			{
				// The source raises SourceChanged 737 ms after a change, which could reach a handler whose MauiApp this test has disposed.
				button.ImageSource = null;
			}
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

	// Gravatar answers a Uri without a size with its default size.
	static int GetRequestedSize(Uri uri) => uri.Query.TrimStart('?').Split('&')
		.Select(static parameter => parameter.Split('='))
		.Where(static pair => pair is ["s", _])
		.Select(static pair => int.Parse(pair[1], CultureInfo.InvariantCulture))
		.DefaultIfEmpty(GravatarImageSourceDefaults.GravatarSize)
		.First();

	// A solid red square PNG of any size, because Gravatar answers with a picture of the requested size.
	static byte[] CreateSquarePng(int size)
	{
		using var pixels = new MemoryStream();
		using (var zlib = new ZLibStream(pixels, CompressionLevel.Fastest, leaveOpen: true))
		{
			// PNG starts each row with its filter type: 0, none.
			var row = new byte[1 + (size * 3)];
			for (var x = 0; x < size; x++)
			{
				row[1 + (x * 3)] = 0xFF;
			}

			for (var y = 0; y < size; y++)
			{
				zlib.Write(row);
			}
		}

		using var png = new MemoryStream();
		png.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
		WritePngChunk(png, "IHDR", [.. ToBigEndian(size), .. ToBigEndian(size), 8, 2, 0, 0, 0]);
		WritePngChunk(png, "IDAT", pixels.ToArray());
		WritePngChunk(png, "IEND", []);

		return png.ToArray();
	}

	static void WritePngChunk(Stream png, string type, byte[] data)
	{
		byte[] typeAndData = [.. Encoding.ASCII.GetBytes(type), .. data];
		png.Write(ToBigEndian(data.Length));
		png.Write(typeAndData);
		png.Write(ToBigEndian(unchecked((int)Crc32(typeAndData))));
	}

	static byte[] ToBigEndian(int value)
	{
		var bytes = new byte[4];
		BinaryPrimitives.WriteInt32BigEndian(bytes, value);
		return bytes;
	}

	static uint Crc32(byte[] bytes)
	{
		var crc = 0xFFFFFFFFu;
		foreach (var value in bytes)
		{
			crc ^= value;
			for (var bit = 0; bit < 8; bit++)
			{
				crc = (crc & 1) is 1 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
			}
		}

		return ~crc;
	}

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