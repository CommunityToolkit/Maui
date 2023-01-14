using System.ComponentModel;
using CommunityToolkit.Maui.Core;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class ExpanderTests : BaseHandlerTest
{
	readonly Maui.Views.Expander expander = new();

	[Fact]
	public void ExpanderShouldBeAssignedToIExpander()
	{
		expander.Should().BeAssignableTo<IExpander>();
	}

	[Fact]
	public void CheckDefaultValues()
	{
		Assert.Equal(ExpandDirection.Down, expander.Direction);
		Assert.False(expander.IsExpanded);
		Assert.Null(expander.Content);
		Assert.Null(expander.Header);
		Assert.Null(expander.Command);
		Assert.Null(expander.CommandParameter);
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

	[Theory]
	[InlineData((ExpandDirection)(-1))]
	[InlineData((ExpandDirection)2)]
	public void ExpanderDirectionThrowsInvalidEnumArgumentException(ExpandDirection direction)
	{
		Assert.Throws<InvalidEnumArgumentException>(() => expander.Direction = direction);
	}

	[Fact]
	public void EnsureExpandedChanged()
	{
		var isExpanded_Initial = expander.IsExpanded;

		var header = new View();
		expander.Header = header;

		expander.HeaderTapGestureRecognizer.SendTapped(header);
		var isExpanded_Final = expander.IsExpanded;

		Assert.True(isExpanded_Final);
		Assert.False(isExpanded_Initial);
		Assert.NotEqual(isExpanded_Initial, isExpanded_Final);

		expander.HeaderTapGestureRecognizer.SendTapped(header);

		Assert.False(expander.IsExpanded);
	}

	[Fact]
	public void EnsureHandleHeaderTappedExecutesWhenHeaderTapped()
	{
		int handleHeaderTappedCount = 0;
		bool didHandleHeaderTappedExecute = false;

		var header = new View();
		expander.Header = new View();
		expander.HandleHeaderTapped = HandleHeaderTapped;

		expander.HeaderTapGestureRecognizer.SendTapped(header);

		Assert.True(didHandleHeaderTappedExecute);
		Assert.Equal(1, handleHeaderTappedCount);

		expander.HandleHeaderTapped = null;

		expander.HeaderTapGestureRecognizer.SendTapped(header);

		Assert.True(didHandleHeaderTappedExecute);
		Assert.Equal(1, handleHeaderTappedCount);

		void HandleHeaderTapped(TappedEventArgs tappedEventArgs)
		{
			handleHeaderTappedCount++;
			didHandleHeaderTappedExecute = true;
		}
	}
}