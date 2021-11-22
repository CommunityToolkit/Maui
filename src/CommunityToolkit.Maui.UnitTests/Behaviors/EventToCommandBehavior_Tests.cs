using System;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.UnitTests.Mocks;
using Microsoft.Maui.Controls;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class EventToCommandBehavior_Tests : BaseTest
{
	[Fact]
	public void ArgumentExceptionIfSpecifiedEventDoesNotExist()
	{
		var listView = new ListView();
		var behavior = new EventToCommandBehavior
		{
			EventName = "Wrong Event Name"
		};
		Assert.Throws<ArgumentException>(() => listView.Behaviors.Add(behavior));
	}

	[Fact]
	public void NoExceptionIfSpecifiedEventExists()
	{
		var listView = new ListView();
		var behavior = new EventToCommandBehavior
		{
			EventName = nameof(ListView.ItemTapped)
		};
		listView.Behaviors.Add(behavior);
	}

	[Fact]
	public void NoExceptionIfAttachedToPage()
	{
		var page = new ContentPage();
		var behavior = new EventToCommandBehavior
		{
			EventName = nameof(Page.Appearing)
		};
		page.Behaviors.Add(behavior);
	}
}