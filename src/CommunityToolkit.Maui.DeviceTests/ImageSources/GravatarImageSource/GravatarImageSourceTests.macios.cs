using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class GravatarImageSourceTests
{
	static bool HasPicture(IPlatformViewHandler handler) => handler.PlatformView switch
	{
		UIImageView imageView => imageView.Image is not null,
		UIButton button => button.CurrentImage is not null,
		_ => false,
	};
}