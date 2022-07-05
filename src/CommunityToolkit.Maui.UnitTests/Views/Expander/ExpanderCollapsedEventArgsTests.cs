using CommunityToolkit.Maui.Core;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views.Expander;

public class ExpanderCollapsedEventArgsTests : BaseHandlerTest
{
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void IsCollapsedShouldBeEqualInExpanderCollapsedEventArgs(bool isCollapsed)
	{
		var eventArgs = new ExpanderCollapsedEventArgs(isCollapsed);
		eventArgs.IsCollapsed.Should().Be(isCollapsed);
	}
}