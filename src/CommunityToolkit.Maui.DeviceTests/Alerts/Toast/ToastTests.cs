using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using static CommunityToolkit.Maui.DeviceTests.AssertHelpers;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.Toast)]
[Collection(TestCollections.DisableParallelization)]
public partial class ToastTests : AlertsTestBase
{
	[Fact]
	public async Task ShowDisplaysTheText()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			await ShowAndWaitForDisplayAsync(Toast.Make("Hello toast", ToastDuration.Long));

			Assert.True(await IsDisplayedAsync());
			Assert.Equal(new[] { "Hello toast" }, await GetDisplayedTextsAsync());
		});
	}

	[Fact]
	public async Task DismissHidesTheToast()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var toast = Toast.Make("Dismiss me", ToastDuration.Long);
			await ShowAndWaitForDisplayAsync(toast);

			await toast.Dismiss();

			await AssertEventually(async () => !await IsDisplayedAsync(), timeout: 10000, message: "The toast was still displayed after Dismiss.");
		});
	}

	[Fact]
	public async Task DismissOnAnotherInstanceHidesTheSameToast()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			await ShowAndWaitForDisplayAsync(Toast.Make("Shared platform toast", ToastDuration.Long));

			await Toast.Make("Other instance").Dismiss();

			await AssertEventually(async () => !await IsDisplayedAsync(), timeout: 10000, message: "The toast was still displayed after Dismiss.");
		});
	}

	[Fact]
	public async Task ShowReplacesTheToastThatIsAlreadyDisplayed()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			await ShowAndWaitForDisplayAsync(Toast.Make("First toast", ToastDuration.Long));

			await ShowAndWaitForDisplayAsync(Toast.Make("Second toast", ToastDuration.Long));

			await AssertEventually(async () => (await GetDisplayedTextsAsync()) is ["Second toast"], timeout: 10000, message: "The second toast did not replace the first one.");
		});
	}

	[Fact]
	public async Task DismissWithNothingShownDoesNotThrow()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			ResetToolkitAlertState();

			var exception = await Record.ExceptionAsync(() => Toast.Make("Nothing to dismiss").Dismiss());

			Assert.Null(exception);
			Assert.False(await IsDisplayedAsync());
		});
	}

	// On Windows, Show returns the task of the asynchronous ShowPlatform, so a cancelled token surfaces in the returned task. Elsewhere the
	// synchronous ShowPlatform runs before Show returns a completed task, so Show itself throws.
	[Fact]
	public async Task ShowWithACancelledTokenThrowsOperationCanceledAndCreatesNoPlatformToast()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			ResetToolkitAlertState();
			using var cancellationTokenSource = new CancellationTokenSource();
			await cancellationTokenSource.CancelAsync();
			var toast = Toast.Make("Cancelled");

#if WINDOWS
			var showTask = toast.Show(cancellationTokenSource.Token);
			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => showTask);
#else
			Assert.ThrowsAny<OperationCanceledException>(() => { _ = toast.Show(cancellationTokenSource.Token); });
#endif

			Assert.Null(GetPlatformToast());
			Assert.False(await IsDisplayedAsync());
		});
	}

	[Fact]
	public async Task ShowWithACancelledTokenWhileAToastIsDisplayedThrowsAndKeepsTheDisplayedToast()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			await ShowAndWaitForDisplayAsync(Toast.Make("Keep me", ToastDuration.Long));
			using var cancellationTokenSource = new CancellationTokenSource();
			await cancellationTokenSource.CancelAsync();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Toast.Make("Cancelled").Show(cancellationTokenSource.Token));

			Assert.True(await IsDisplayedAsync());
			Assert.Equal(new[] { "Keep me" }, await GetDisplayedTextsAsync());
		});
	}

	// Like Show, Dismiss returns the task of DismissPlatform on Windows only; elsewhere the exception is thrown by Dismiss itself.
	[Fact]
	public async Task DismissWithACancelledTokenWhileAToastIsDisplayedThrowsAndKeepsTheToast()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var toast = Toast.Make("Keep me too", ToastDuration.Long);
			await ShowAndWaitForDisplayAsync(toast);
			using var cancellationTokenSource = new CancellationTokenSource();
			await cancellationTokenSource.CancelAsync();

#if WINDOWS
			var dismissTask = toast.Dismiss(cancellationTokenSource.Token);
			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => dismissTask);
#else
			Assert.ThrowsAny<OperationCanceledException>(() => { _ = toast.Dismiss(cancellationTokenSource.Token); });
#endif

			Assert.True(await IsDisplayedAsync());
		});
	}

	[Fact]
	public async Task DismissWithACancelledTokenAndNothingShownThrowsOperationCanceled()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			ResetToolkitAlertState();
			using var cancellationTokenSource = new CancellationTokenSource();
			await cancellationTokenSource.CancelAsync();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Toast.Make("Nothing shown").Dismiss(cancellationTokenSource.Token));
		});
	}

	[Fact]
	public async Task DisposeTwiceDoesNotThrow()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var toast = new Toast { Text = "Dispose twice", Duration = ToastDuration.Long };
			await ShowAndWaitForDisplayAsync(toast);
			await TakeTheToastOffScreenBeforeItIsDisposedAsync();

			var exception = Record.Exception(() =>
			{
				toast.Dispose();
				toast.Dispose();
			});

			Assert.Null(exception);
		});
	}

	[Fact]
	public async Task ShowAfterTheShownToastWasDisposedDisplaysTheNextToast()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var first = Toast.Make("Disposed after it was shown", ToastDuration.Long);
			await ShowAndWaitForDisplayAsync(first);
			await TakeTheToastOffScreenBeforeItIsDisposedAsync();
			first.Dispose();

			var exception = await Record.ExceptionAsync(() => ShowAndWaitForDisplayAsync(Toast.Make("Next toast", ToastDuration.Long)));

			Assert.Null(exception);
			Assert.Equal(new[] { "Next toast" }, await GetDisplayedTextsAsync());
		});
	}

	[Fact]
	public async Task DismissAfterTheShownToastWasDisposedDoesNotThrow()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var toast = Toast.Make("Disposed, then dismissed", ToastDuration.Long);
			await ShowAndWaitForDisplayAsync(toast);
			await TakeTheToastOffScreenBeforeItIsDisposedAsync();
			toast.Dispose();

			var exception = await Record.ExceptionAsync(() => toast.Dismiss());

			Assert.Null(exception);
		});
	}

	[Fact]
	public async Task DisposeBeforeAnyToastWasShownDoesNotThrow()
	{
		SetupBuilder();
		await PreparePlatformAsync();

		await InvokeOnMainThreadAsync(() =>
		{
			ResetToolkitAlertState();
			Assert.Null(GetPlatformToast());

			var exception = Record.Exception(() => new Toast { Text = "Never shown" }.Dispose());

			Assert.Null(exception);
		});
	}
}