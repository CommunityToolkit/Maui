using Font = Microsoft.Maui.Font;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Alerts;

public class Snackbar_Tests : BaseTest
{
	readonly ISnackbar snackbar = new Snackbar();

	public Snackbar_Tests()
	{
		Assert.IsAssignableFrom<IAlert>(snackbar);
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
		Snackbar.Shown += (sender, e) =>
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
		Snackbar.Dismissed += (sender, e) =>
		{
			receivedEvents.Add(e);
		};
		await snackbar.Dismiss();
		Assert.Single(receivedEvents);
	}

	[Fact]
	public async Task VisualElement_DisplaySnackbar_ShownEventReceived()
	{
		var receivedEvents = new List<EventArgs>();
		Snackbar.Shown += (_, e) =>
		{
			receivedEvents.Add(e);
		};
		var button = new Button();
		await button.DisplaySnackbar("message");
		Assert.Single(receivedEvents);
	}

	[Fact]
	public void SnackbarMake_NewSnackbarCreatedWithValidProperties()
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

	[Fact]
	public async Task SnackbarShow_CancellationTokenCancelled_ReceiveException()
	{
		using var cancellationTokenSource = new CancellationTokenSource();

		cancellationTokenSource.Cancel();

		await snackbar.Invoking(x => x.Show(cancellationTokenSource.Token)).Should().ThrowExactlyAsync<OperationCanceledException>();
	}

	[Fact]
	public async Task SnackbarDismiss_CancellationTokenCancelled_ReceiveException()
	{
		using var cancellationTokenSource = new CancellationTokenSource();

		cancellationTokenSource.Cancel();

		await snackbar.Invoking(x => x.Dismiss(cancellationTokenSource.Token)).Should().ThrowExactlyAsync<OperationCanceledException>();
	}

	[Fact]
	public async Task SnackbarShow_CancellationTokenNotCancelled_NotReceiveException()
	{
		using var cancellationTokenSource = new CancellationTokenSource();
		await snackbar.Invoking(x => x.Show(cancellationTokenSource.Token)).Should().NotThrowAsync<OperationCanceledException>();
	}

	[Fact]
	public async Task SnackbarDismiss_CancellationTokenNotCancelled_NotReceiveException()
	{
		using var cancellationTokenSource = new CancellationTokenSource();
		await snackbar.Invoking(x => x.Dismiss(cancellationTokenSource.Token)).Should().NotThrowAsync<OperationCanceledException>();
	}

	[Fact]
	public async Task SnackbarShow_CancellationTokenNone_NotReceiveException()
	{
		await snackbar.Invoking(x => x.Show(CancellationToken.None)).Should().NotThrowAsync<OperationCanceledException>();
	}

	[Fact]
	public async Task SnackbarDismiss_CancellationTokenNone_NotReceiveException()
	{
		await snackbar.Invoking(x => x.Dismiss(CancellationToken.None)).Should().NotThrowAsync<OperationCanceledException>();
	}
}