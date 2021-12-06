using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Alerts.Snackbar;
using FluentAssertions;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
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

	[Fact]
	public async Task VisualElement_DisplaySnackbar_ShownEventReceived()
	{
		var receivedEvents = new List<EventArgs>();
		Snackbar.Shown += (sender, e) =>
		{
			receivedEvents.Add(e);
		};
		var button = new Button();
		await button.DisplaySnackbar("message");
		Assert.Single(receivedEvents);
	}

	[Fact]
	public async Task SnackbarMake_NewSnackbarCreatedWithValidProperties()
	{
		var action = () => { };
		var anchor = new Button();
		var expectedSnackbar = new Snackbar
		{
			Anchor = anchor,
			Action = action,
			Duration = TimeSpan.MaxValue,
			Text = "Test",
			ActionButtonText = "Ok",
			VisualOptions = new SnackbarOptions
			{
				Font = Font.Default,
				BackgroundColor = Colors.Red,
				CharacterSpacing = 10,
				CornerRadius = new CornerRadius(1, 2, 3, 4),
				TextColor = Colors.RosyBrown,
				ActionButtonFont = Font.SystemFontOfSize(5),
				ActionButtonTextColor = Colors.Aqua
			}
		};

		var currentSnackbar = Snackbar.Make(
			"Test",
			action,
			"Ok",
			TimeSpan.MaxValue,
			new SnackbarOptions
			{
				Font = Font.Default,
				BackgroundColor = Colors.Red,
				CharacterSpacing = 10,
				CornerRadius = new CornerRadius(1, 2, 3, 4),
				TextColor = Colors.RosyBrown,
				ActionButtonFont = Font.SystemFontOfSize(5),
				ActionButtonTextColor = Colors.Aqua
			},
			anchor);

		currentSnackbar.Should().BeEquivalentTo(expectedSnackbar);
	}
}