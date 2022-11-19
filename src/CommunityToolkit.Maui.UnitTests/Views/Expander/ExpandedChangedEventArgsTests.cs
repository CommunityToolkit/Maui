using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views.Expander;

public class ExpandedChangedEventArgsTests : BaseHandlerTest
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