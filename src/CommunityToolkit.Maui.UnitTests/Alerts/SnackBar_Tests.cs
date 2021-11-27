using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Alerts.Snackbar;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Alerts;

public class Snackbar_Tests : BaseTest
{
	readonly ISnackbar _snackbar = new Snackbar();

	[Fact]
	public async Task SnackbarShow_IsShownTrue()
	{
		await _snackbar.Show();
		Assert.True(Snackbar.IsShown);
	}

	[Fact]
	public async Task SnackbarDismissed_IsShownFalse()
	{
		await _snackbar.Dismiss();
		Assert.False(Snackbar.IsShown);
	}

	[Fact]
	public async Task SnackbarShow_ShownEventRaised()
	{
		var receivedEvents = new List<EventArgs>();
		Snackbar.Shown += (sender, e) =>
		{
			receivedEvents.Add(e);
		};
		await _snackbar.Show();
		Assert.Single(receivedEvents);
	}

	[Fact]
	public async Task SnackbarDismiss_DismissedEventRaised()
	{
		var receivedEvents = new List<EventArgs>();
		Snackbar.Dismissed += (sender, e) =>
		{
			receivedEvents.Add(e);
		};
		await _snackbar.Dismiss();
		Assert.Single(receivedEvents);
	}
}