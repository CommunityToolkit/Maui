using FluentAssertions;
using Xunit;
using ParentWindow = CommunityToolkit.Maui.Extensions.PageExtensions.ParentWindow;

namespace CommunityToolkit.Maui.UnitTests.Views;

// The fifth state of ParentWindow.Exists, next to ParentWindowTests. MediaManager's status timer (MediaManager.windows.cs PlatformUpdatePosition)
// asks ParentWindow.Exists on every tick to stop updating once its window is gone.
public class ParentWindowWithoutPageTests : BaseViewTest
{
	[Fact]
	public void Exists_WhenTheLastWindowHasNoPage_ReturnsFalse()
	{
		Application.Current.Should().NotBeNull();

		var window = new Window();
		Application.Current.OpenWindow(window);

		ParentWindow.Exists.Should().BeFalse();
	}
}