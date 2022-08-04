namespace CommunityToolkit.Maui.UnitTests.Views.GravatarImageSource;

using System;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Core;

public abstract partial class ImageSourceStub : IImageSource
{
	public bool IsEmpty { get; set; }
}

public partial class GravatarImageImageSourceStub : ImageSourceStub, IGravatarImageSource
{
#nullable disable
	public GravatarImageImageSourceStub()
	{
	}

	public TimeSpan CacheValidity { get; set; }
	public bool CachingEnabled { get; set; }
	public string Email { get; set; }
	public DefaultImage Image { get; set; }
	public Func<CancellationToken, Task<Stream>> Stream { get; set; }

	public Uri Uri { get; }

	public Task<Stream> GetStreamAsync(CancellationToken cancellationToken = default) => Stream?.Invoke(cancellationToken);

#nullable enable
}

public partial class UriImageSourceStub : ImageSourceStub, IUriImageSource
{
#nullable disable
	public UriImageSourceStub()
	{
	}

	public UriImageSourceStub(string uri)
		: this(new Uri(uri))
	{
	}

	public UriImageSourceStub(Uri uri)
	{
		Uri = uri;
	}

	public Uri Uri { get; set; }

	public TimeSpan CacheValidity { get; set; } = TimeSpan.FromDays(1);

	public bool CachingEnabled { get; set; } = true;
#nullable enable
}

public partial class InvalidImageSourceStub : ImageSourceStub
{
}