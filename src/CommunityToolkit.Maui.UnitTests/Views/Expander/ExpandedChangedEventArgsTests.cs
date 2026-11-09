using CommunityToolkit.Maui.Core;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class ExpandedChangedEventArgsTests : BaseViewTest
{
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void IsExpandedShouldBeEqualInExpandedChangedEventArgs(bool isExpanded)
	{
		var eventArgs = new ExpandedChangedEventArgs(isExpanded);
		eventArgs.IsExpanded.Should().Be(isExpanded);
	}
}