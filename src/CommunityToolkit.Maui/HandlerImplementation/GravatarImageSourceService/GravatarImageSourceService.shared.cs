namespace CommunityToolkit.Maui.Views;

using CommunityToolkit.Maui.Core;
using Microsoft.Extensions.Logging;

/// <summary>Gravatar image source service.</summary>
public partial class GravatarImageSourceService : ImageSourceService, IImageSourceService<IGravatarImageSource>
{
	/// <summary>Initializes a new instance of the <see cref="GravatarImageSourceService"/> class.</summary>
	public GravatarImageSourceService() : this(null)
	{
	}

	/// <summary>Initializes a new instance of the <see cref="GravatarImageSourceService"/> class.</summary>
	/// <param name="logger">Logging service.</param>
	public GravatarImageSourceService(ILogger<GravatarImageSourceService>? logger = null) : base(logger)
	{
	}
}