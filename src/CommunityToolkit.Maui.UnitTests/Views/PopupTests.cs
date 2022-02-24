using System;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests;

public class PopupTests : BaseTest
{
	readonly IPopup popup = new MockPopup();
	public PopupTests()
	{
		Assert.IsAssignableFrom<IPopup>(new MockPopup());
		Assert.IsAssignableFrom<IPopup>(new MockBasePopup());
	}

	[Fact]
	public void PlaceholderTest()
	{
		var page = new ContentPage
		{
			Content = new Label
			{
				Text = "Hello there"
			}
		};
		var np = new NavigationPage(page);
		_ = page.Navigation;
		page.ShowPopup((MockPopup)popup);
		Assert.True(true);
		// This is a place holder to remind us to implement Unit Tests for Popup
		// This test should be deleted once Popup Unit Tests are added
		//throw new NotImplementedException("Popup Unit Tests Not completed");
	}

	class MockPopup : Popup
	{

	}

	class MockBasePopup : BasePopup
	{

	}
}

