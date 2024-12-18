using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Handlers;
namespace CommunityToolkit.Maui.UnitTests.Mocks;

public class MockAvatarViewHandler(IPropertyMapper mapper) : ViewHandler<IAvatarView, AvatarView>(mapper), IBorderHandler
{
	readonly AvatarView avatarView = new();

	public MockAvatarViewHandler() : this(new PropertyMapper<IView>())
	{

	}

	protected override AvatarView CreatePlatformView() => avatarView;

	public new AvatarView PlatformView => avatarView;
	public new AvatarView VirtualView => (AvatarView)base.VirtualView;

	public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
	{
		var width = VirtualView.WidthRequest <= widthConstraint ? VirtualView.WidthRequest : widthConstraint;
		var height = VirtualView.HeightRequest <= heightConstraint ? VirtualView.HeightRequest : heightConstraint;

		return new Size(width, height);
	}

	object IBorderHandler.PlatformView => PlatformView;
	IBorderView IBorderHandler.VirtualView => base.VirtualView;
}