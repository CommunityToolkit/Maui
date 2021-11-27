using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Views.Popup.Snackbar;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class Snackbar_Tests : BaseTest
{
	Snackbar snackbar;
	public Snackbar_Tests()
	{
		snackbar = new Snackbar();
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
		var receivedEvents = new List<EventArgs>();
		Snackbar.Shown += delegate (object? sender, EventArgs e)
		{
			receivedEvents.Add(e);
		};
		await snackbar.Show();
		Assert.Single(receivedEvents);
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

	class MockSnackbar : Snackbar
	{
		public override Task Dismiss() => throw new NotImplementedException();

		public override Task Show() => throw new NotImplementedException();
	}
}