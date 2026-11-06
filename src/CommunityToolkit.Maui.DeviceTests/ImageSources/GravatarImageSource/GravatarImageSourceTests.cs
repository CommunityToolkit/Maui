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
	sealed class PictureHandler : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
			Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new NonSeekableStream(Convert.FromBase64String(redPngBase64))) });
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