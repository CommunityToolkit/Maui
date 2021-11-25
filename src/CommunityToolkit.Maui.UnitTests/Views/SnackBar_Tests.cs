using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Maui.UnitTests.Mocks;
using CommunityToolkit.Maui.Views.Popup;
using CommunityToolkit.Maui.Views.Popup.SnackBar;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class SnackBar_Tests : BaseTest
{
	Snackbar snackbar;
	public SnackBar_Tests()
	{
		snackbar = new Snackbar();
		snackbar.PlatformPopupExtensions = new MockPlatformPopupExtensions();
	}

	[Fact]
	public async Task SnackbarShow_IsShownTrue()
	{
		await snackbar.Show();
		Assert.True(Snackbar.IsShown);
	}

	[Fact]
	public async Task SnackbarDismissed_IsShownFalse()
	{
		await snackbar.Dismiss();
		Assert.False(Snackbar.IsShown);
	}

	[Fact]
	public async Task SnackbarShow_ShownEventRaised()
	{
		var receivedEvents = new List<ShownEventArgs>();
		Snackbar.Shown += delegate (object? sender, ShownEventArgs e)
		{
			receivedEvents.Add(e);
		};
		await snackbar.Show();
		Assert.Single(receivedEvents);
		Assert.True(receivedEvents.First()?.IsShown);
	}

	[Fact]
	public async Task SnackbarDismiss_DismissedEventRaised()
	{
		var receivedEvents = new List<EventArgs>();
		Snackbar.Dismissed += delegate (object? sender, EventArgs e)
		{
			receivedEvents.Add(e);
		};
		await snackbar.Show();
		Assert.Single(receivedEvents);
	}
}
