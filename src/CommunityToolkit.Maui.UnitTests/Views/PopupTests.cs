using System;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests;

public class PopupTests : BaseTest
{
	public PopupTests()
	{
		Assert.IsAssignableFrom<IPopup>(new MockPopup());
		Assert.IsAssignableFrom<IPopup>(new MockBasePopup());
	}

	[Fact]
	public void PlaceholderTest()
	{
		// This is a place holder to remind us to implement Unit Tests for Popup
		// This test should be deleted once Popup Unit Tests are added
		throw new NotImplementedException("Popup Unit Tests Not completed");
	}

	class MockPopup : Popup
	{

	}

	class MockBasePopup : BasePopup
	{

	}
}

