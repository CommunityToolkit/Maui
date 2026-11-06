using Google.Android.Material.Button;
using AImageView = Android.Widget.ImageView;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class GravatarImageSourceTests
{
	static bool HasPicture(IPlatformViewHandler handler) => handler.PlatformView switch
	{
		AImageView imageView => imageView.Drawable is not null,
		MaterialButton button => button.Icon is not null,
		_ => false,
	};
}