using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class ExpanderContentVisibilityTests : BaseViewTest
{
	readonly Maui.Views.Expander expander = new();

	[Fact]
	public void ContentVisibility_FollowsIsExpanded()
	{
		var content = new Label();

		expander.Content = content;

		content.IsVisible.Should().BeFalse();

		expander.IsExpanded = true;

		content.IsVisible.Should().BeTrue();

		expander.IsExpanded = false;

		content.IsVisible.Should().BeFalse();
	}

	[Fact]
	public void ReplacedContentVisibility_FollowsIsExpanded()
	{
		expander.Content = new Label();
		expander.IsExpanded = true;

		var replacementContent = new Label();
		expander.Content = replacementContent;

		replacementContent.IsVisible.Should().BeTrue();

		expander.IsExpanded = false;

		replacementContent.IsVisible.Should().BeFalse();
	}
}
