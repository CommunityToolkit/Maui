﻿using System.ComponentModel;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Alerts;

public class ToastTests : BaseTest
{
	readonly IToast toast = new Toast();

	public ToastTests()
	{
		Assert.IsType<IAlert>(toast, exactMatch: false);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ToastShow_CancellationTokenExpires()
	{
		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		// Ensure CancellationToken expires
		await Task.Delay(100, TestContext.Current.CancellationToken);

		await Assert.ThrowsAsync<OperationCanceledException>(() => toast.Show(cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ToastShow_CancellationTokenCanceled()
	{
		var cts = new CancellationTokenSource();

		await Assert.ThrowsAsync<OperationCanceledException>(() =>
		{
			cts.Cancel();
			return toast.Show(cts.Token);
		});
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ToastDismiss_CancellationTokenExpires()
	{
		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		// Ensure CancellationToken expires
		await Task.Delay(100, TestContext.Current.CancellationToken);

		await Assert.ThrowsAsync<OperationCanceledException>(() => toast.Dismiss(cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ToastDismiss_CancellationTokenCanceled()
	{
		var cts = new CancellationTokenSource();

		await Assert.ThrowsAsync<OperationCanceledException>(() =>
		{
			cts.Cancel();
			return toast.Dismiss(cts.Token);
		});
	}

	[Fact]
	public void ToastMake_NewToastCreatedWithValidProperties()
	{
		var expectedToast = new Toast
		{
			TextSize = 30,
			Duration = ToastDuration.Long,
			Text = "Test"
		};

		var currentToast = Toast.Make(
			"Test",
			ToastDuration.Long,
			30);

		currentToast.Should().BeEquivalentTo(expectedToast);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ToastShow_CancellationTokenCancelled_ReceiveException()
	{
		var cancellationTokenSource = new CancellationTokenSource();
		cancellationTokenSource.Cancel();
		await toast.Invoking(x => x.Show(cancellationTokenSource.Token)).Should().ThrowExactlyAsync<OperationCanceledException>();
		cancellationTokenSource.Dispose();
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ToastDismiss_CancellationTokenCancelled_ReceiveException()
	{
		var cancellationTokenSource = new CancellationTokenSource();
		cancellationTokenSource.Cancel();
		await toast.Invoking(x => x.Dismiss(cancellationTokenSource.Token)).Should().ThrowExactlyAsync<OperationCanceledException>();
		cancellationTokenSource.Dispose();
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ToastShow_CancellationTokenNotCancelled_NotReceiveException()
	{
		var cancellationTokenSource = new CancellationTokenSource();
		await toast.Invoking(x => x.Show(cancellationTokenSource.Token)).Should().NotThrowAsync<OperationCanceledException>();
		cancellationTokenSource.Dispose();
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ToastDismiss_CancellationTokenNotCancelled_NotReceiveException()
	{
		var cancellationTokenSource = new CancellationTokenSource();
		await toast.Invoking(x => x.Dismiss(cancellationTokenSource.Token)).Should().NotThrowAsync<OperationCanceledException>();
		cancellationTokenSource.Dispose();
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ToastShow_CancellationTokenNone_NotReceiveException()
	{
		await toast.Invoking(x => x.Show(TestContext.Current.CancellationToken)).Should().NotThrowAsync<OperationCanceledException>();
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ToastDismiss_CancellationTokenNone_NotReceiveException()
	{
		await toast.Invoking(x => x.Dismiss(TestContext.Current.CancellationToken)).Should().NotThrowAsync<OperationCanceledException>();
	}

	[Fact]
	public void ToastMake_NewToastCreatedWithDefaultValues()
	{
		toast.Text.Should().BeEmpty();
		toast.Duration.Should().Be(ToastDuration.Short);
		toast.TextSize.Should().Be(AlertDefaults.FontSize);
	}

	[Fact]
	public void ToastMake_NewToastCreatedWithNullString_ShouldThrowArgumentNullException()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => Toast.Make(null));
		Assert.Throws<ArgumentNullException>(() => new Toast { Text = null });
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Theory]
	[InlineData(int.MinValue)]
	[InlineData(-1)]
	[InlineData(2)]
	[InlineData(int.MaxValue)]
	public void ToastMake_NewToastCreatedWithInvalidToastDuration_ShouldThrowInvalidEnumArgumentException(int duration)
	{
		Assert.Throws<InvalidEnumArgumentException>(() => Toast.Make("Invalid Duration", (ToastDuration)duration));
		Assert.Throws<InvalidEnumArgumentException>(() => new Toast { Duration = (ToastDuration)duration });
	}

	[Theory]
	[InlineData(int.MinValue)]
	[InlineData(-1)]
	[InlineData(0)]
	public void ToastMake_NewToastCreatedWithInvalidFontSize_ShouldThrowArgumentOutOfRangeException(int textSize)
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => Toast.Make("Invalid text size", ToastDuration.Short, textSize));
		Assert.Throws<ArgumentOutOfRangeException>(() => new Toast { TextSize = textSize });
	}
}