using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Alerts;

public class Toast_Tests : BaseTest
{
	IToast _toast = new Toast();

	[Fact]
	public void ToastMake_NewToastCreatedWithValidProperties()
	{
		var expectedToast = new Toast
		{
			Duration = ToastDuration.Long,
			Text = "Test"
		};

		var currentToast = Toast.Make(
			"Test",
			ToastDuration.Long);

		currentToast.Should().BeEquivalentTo(expectedToast);
	}

	[Fact]
	public async Task ToastShow_CancellationTokenCancelled_ReceiveException()
	{
		var cancellationTokenSource = new CancellationTokenSource();
		cancellationTokenSource.Cancel();
		await _toast.Invoking(x => x.Show(cancellationTokenSource.Token)).Should().ThrowExactlyAsync<OperationCanceledException>();
		cancellationTokenSource.Dispose();
	}

	[Fact]
	public async Task ToastDismiss_CancellationTokenCancelled_ReceiveException()
	{
		var cancellationTokenSource = new CancellationTokenSource();
		cancellationTokenSource.Cancel();
		await _toast.Invoking(x => x.Dismiss(cancellationTokenSource.Token)).Should().ThrowExactlyAsync<OperationCanceledException>();
		cancellationTokenSource.Dispose();
	}

	[Fact]
	public async Task ToastShow_CancellationTokenNotCancelled_NotReceiveException()
	{
		var cancellationTokenSource = new CancellationTokenSource();
		await _toast.Invoking(x => x.Show(cancellationTokenSource.Token)).Should().NotThrowAsync<OperationCanceledException>();
		cancellationTokenSource.Dispose();
	}

	[Fact]
	public async Task ToastDismiss_CancellationTokenNotCancelled_NotReceiveException()
	{
		var cancellationTokenSource = new CancellationTokenSource();
		await _toast.Invoking(x => x.Dismiss(cancellationTokenSource.Token)).Should().NotThrowAsync<OperationCanceledException>();
		cancellationTokenSource.Dispose();
	}

	[Fact]
	public async Task ToastShow_CancellationTokenNone_NotReceiveException()
	{
		await _toast.Invoking(x => x.Show(CancellationToken.None)).Should().NotThrowAsync<OperationCanceledException>();
	}

	[Fact]
	public async Task ToastDismiss_CancellationTokenNone_NotReceiveException()
	{
		await _toast.Invoking(x => x.Dismiss(CancellationToken.None)).Should().NotThrowAsync<OperationCanceledException>();
	}
}