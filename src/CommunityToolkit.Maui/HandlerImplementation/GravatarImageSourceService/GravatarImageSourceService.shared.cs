namespace CommunityToolkit.Maui.Views;

using CommunityToolkit.Maui.Core;
using Microsoft.Extensions.Logging;

/// <summary>Gravatar image source service.</summary>
public partial class GravatarImageSourceService : ImageSourceService, IImageSourceService<IGravatarImageSource>
{
	/// <summary>Initializes a new instance of the <see cref="GravatarImageSourceService"/> class.</summary>
	public GravatarImageSourceService()
	{
		uriImageSourceService = new UriImageSourceService();
	}

	/// <summary>Initializes a new instance of the <see cref="GravatarImageSourceService"/> class.</summary>
	/// <param name="uriImageSourceService">Base service.</param>
	/// <param name="logger">Logging service.</param>
	public GravatarImageSourceService(UriImageSourceService uriImageSourceService, ILogger<GravatarImageSourceService>? logger = null) : base(logger)
	{
		this.uriImageSourceService = uriImageSourceService;
	}

	UriImageSourceService uriImageSourceService { get; }
}