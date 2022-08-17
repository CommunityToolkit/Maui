using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views.Expander;

public class ExpanderTests : BaseHandlerTest
{
	readonly Maui.Views.Expander expander = new();

	[Fact]
	public void ExpanderShouldBeAssignedToIExpander()
	{
		new Maui.Views.Expander().Should().BeAssignableTo<IExpander>();
	}
	
	[Fact]
	public void CheckDefaultValues()
	{
		var expectedDefaultValue = new Maui.Views.Expander
		{
			Direction = ExpandDirection.Down,
			IsExpanded = false
		};

		expander.Should().BeEquivalentTo(expectedDefaultValue);
	}
	
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void ExpandedChangedIsExpandedPassedWithEvent(bool expectedIsExpanded)
	{
		bool? isExpanded = null;
		var action = new EventHandler<ExpandedChangedEventArgs>((_, e) => isExpanded = e.IsExpanded);
		expander.ExpandedChanged += action;
		((IExpander)expander).ExpandedChanged(expectedIsExpanded);
		expander.ExpandedChanged -= action;

		isExpanded.Should().Be(expectedIsExpanded);
	}
	
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void ExpandedChangedCommandExecutedWithParams(bool expectedIsExpanded)
	{
		bool? isExpanded = null;

		expander.Command = new Command<bool>(parameter => isExpanded = parameter);
		expander.CommandParameter = expectedIsExpanded;
		((IExpander)expander).ExpandedChanged(expectedIsExpanded);

		isExpanded.Should().Be(expectedIsExpanded);
	}
}