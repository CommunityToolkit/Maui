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
	public void GetRequiredServiceThrowsOnNoContext()
	{
		var handlerStub = new MockExpanderHandler();

		(handlerStub as IElementHandler).MauiContext.Should().BeNull();

		var ex = Assert.Throws<InvalidOperationException>(() => handlerStub.GetRequiredService<IExpander>());

		ex.Message.Should().Be("Unable to find the context. The MauiContext property should have been set by the host.");
	}
	
	[Fact]
	public void HeaderMapperIsCalled()
	{
		var expanderHandler = CreateViewHandler<MockExpanderHandler>(expander);
		expander.Handler.Should().NotBeNull();

		expanderHandler.MapHeaderCount.Should().Be(1);

		expander.Header = new Label();
		expanderHandler.MapHeaderCount.Should().Be(2);
	}

	[Fact]
	public void IsExpandedMapperIsCalled()
	{
		var expanderHandler = CreateViewHandler<MockExpanderHandler>(expander);
		expander.Handler.Should().NotBeNull();

		expanderHandler.MapIsExpandedCount.Should().Be(1);

		expander.IsExpanded = true;
		expanderHandler.MapIsExpandedCount.Should().Be(2);
	}

	[Fact]
	public void ContentMapperIsCalled()
	{
		var expanderHandler = CreateViewHandler<MockExpanderHandler>(expander);
		expander.Handler.Should().NotBeNull();

		expanderHandler.MapContentCount.Should().Be(1);

		expander.Content = new Label();
		expanderHandler.MapContentCount.Should().Be(2);
	}

	[Fact]
	public void DirectionMapperIsCalled()
	{
		var expanderHandler = CreateViewHandler<MockExpanderHandler>(expander);
		expander.Handler.Should().NotBeNull();

		expanderHandler.MapDirectionCount.Should().Be(1);

		expander.Direction = ExpandDirection.Up;
		expanderHandler.MapDirectionCount.Should().Be(2);
	}

	[Fact]
	public void CheckDefaultValues()
	{
		var expectedDefaultValue = new Maui.Views.Expander
		{
			Direction = ExpandDirection.Down,
			IsExpanded = false
		};

		expander.Should().BeEquivalentTo(expectedDefaultValue, config => config.Excluding(ctx => ctx.Id));
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