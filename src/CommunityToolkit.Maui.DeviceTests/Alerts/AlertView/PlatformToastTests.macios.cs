using System.Runtime.CompilerServices;
using CommunityToolkit.Maui.Core.Views;
using CoreGraphics;
using CoreText;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.AlertView)]
[Collection(TestCollections.DisableParallelization)]
public class PlatformToastTests : AlertsTestBase
{
	[Fact]
	public async Task ConstructorAppliesTheMessageColorFontAndBackgroundToTheAlertView()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var toast = new TestPlatformToast("Toast message", UIColor.Red, new CGRect(1, 2, 3, 4), UIColor.Blue, UIFont.SystemFontOfSize(20)!, 0, 6);

			var label = toast.View.GetLabel();
			Assert.Equal("Toast message", toast.Message);
			Assert.Equal("Toast message", label.Text);
			AlertsApple.AssertColor(0, 0, 1, 1, toast.TextColor);
			AlertsApple.AssertColor(0, 0, 1, 1, label.TextColor);
			Assert.Equal(20, (double)toast.Font.PointSize);
			Assert.Equal(20, (double)label.Font.PointSize);
			Assert.Equal(0, (int)label.Lines);
			AlertsApple.AssertColor(1, 0, 0, 1, toast.View.VisualOptions.BackgroundColor);
			Assert.Equal(new CGRect(1, 2, 3, 4), toast.View.VisualOptions.CornerRadius);
			Assert.Same(label, Assert.Single(toast.View.Children));
		});
	}

	[Fact]
	public async Task ConstructorAddsTheDefaultPaddingToTheRequestedPadding()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var toast = new TestPlatformToast("padding", UIColor.White, CGRect.Empty, UIColor.Black, UIFont.SystemFontOfSize(14)!, 0, 6);

			var label = toast.View.GetLabel();
			Assert.Equal(16, (double)label.LeftPadding);
			Assert.Equal(16, (double)label.TopPadding);
			Assert.Equal(16, (double)label.RightPadding);
			Assert.Equal(16, (double)label.BottomPadding);
		});
	}

	// The amount of kerning is pinned by CharacterSpacingKernsTheMessageBy100TimesTheValueOverThePointSize.
	[Fact]
	public async Task CharacterSpacingIsAppliedToTheWholeMessageAsKerning()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var toast = new TestPlatformToast("kerning", UIColor.White, CGRect.Empty, UIColor.Black, UIFont.SystemFontOfSize(20)!, 5, 0);

			var attributedText = toast.View.GetLabel().AttributedText!;
			var attributes = attributedText.GetCoreTextAttributes(0, out var range);
			Assert.Equal("kerning", attributedText.Value);
			Assert.Equal("kerning".Length, (int)range.Length);
			Assert.True(attributes!.KerningAdjustment > 0, $"The message is not spaced: KerningAdjustment is {attributes.KerningAdjustment}.");
		});
	}

	// A font without a point size (UIFont.SystemFontOfSize(0)) takes the other arm of the conversion, since dividing by the zero size would
	// give an infinite kerning.
	[Fact]
	public async Task CharacterSpacingWithAFontWithoutAPointSizeKeepsTheMessage()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var font = UIFont.SystemFontOfSize(0)!;
			Assert.NotNull(font);
			Assert.Equal(0, (double)font.PointSize);

			var toast = new TestPlatformToast("no point size", UIColor.White, CGRect.Empty, UIColor.Black, font, 5, 0);

			var attributedText = toast.View.GetLabel().AttributedText!;
			var attributes = attributedText.GetCoreTextAttributes(0, out var range);
			Assert.Equal("no point size", attributedText.Value);
			Assert.Equal("no point size".Length, (int)range.Length);
			Assert.NotNull(attributes!.KerningAdjustment);
			Assert.True(float.IsFinite(attributes.KerningAdjustment.Value), $"The kerning is {attributes.KerningAdjustment}.");
		});
	}

	// The expected values follow the unit documented on PlatformToast.CharacterSpacing; a MAUI Label is kerned by CharacterSpacing points
	// instead.
	[Theory]
	[InlineData(20.0, 25.0)]
	[InlineData(10.0, 50.0)]
	[InlineData(0.0, 0.0)]
	public async Task CharacterSpacingKernsTheMessageBy100TimesTheValueOverThePointSize(double fontSize, double expectedKerning)
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			const double characterSpacing = 5;
			var toast = new TestPlatformToast("kerning", UIColor.White, CGRect.Empty, UIColor.Black, UIFont.SystemFontOfSize((nfloat)fontSize)!, characterSpacing, 0);

			Assert.Equal(fontSize, (double)toast.Font.PointSize);
			var attributes = toast.View.GetLabel().AttributedText!.GetCoreTextAttributes(0, out _);
			Assert.Equal(expectedKerning, attributes!.KerningAdjustment!.Value, 3);
		});
	}

	[Fact]
	public async Task EmptyMessageIsKeptAsAnEmptyLabel()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var toast = new TestPlatformToast(string.Empty, UIColor.White, CGRect.Empty, UIColor.Black, UIFont.SystemFontOfSize(14)!, 0, 0);

			Assert.Equal(string.Empty, toast.Message);
			Assert.Equal(string.Empty, toast.View.GetLabel().Text);
		});
	}

	[Fact]
	public async Task NullMessageReadsBackAsAnEmptyString()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var toast = new TestPlatformToast(null!, UIColor.White, CGRect.Empty, UIColor.Black, UIFont.SystemFontOfSize(14)!, 0, 0);

			Assert.Equal(string.Empty, toast.Message);
		});
	}

	[Fact]
	public async Task ShowAddsTheToastToTheKeyWindowAndDismissRemovesIt()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var toast = new TestPlatformToast("show", UIColor.White, CGRect.Empty, UIColor.Black, UIFont.SystemFontOfSize(14)!, 0, 0) { Duration = TimeSpan.FromSeconds(30) };
			Assert.Same(AlertsApple.KeyWindow, toast.View.Superview);

			toast.Show();
			Assert.Same(toast.View, AlertsApple.GetSingleAlertView());

			toast.Dismiss();
			Assert.Empty(AlertsApple.GetAlertViews());
		});
	}

	[Fact]
	public async Task DisposeDisposesTheManagedPeerOfTheMessageLabelAndLeavesTheNativeLabelInTheAlertView()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var toast = new TestPlatformToast("dispose", UIColor.White, CGRect.Empty, UIColor.Black, UIFont.SystemFontOfSize(14)!, 0, 0);
			var label = toast.View.GetLabel();
			Assert.False(AlertsApple.IsManagedPeerDisposed(label));
			Assert.False(AlertsApple.IsDisposed(toast));

			toast.Dispose();

			Assert.True(AlertsApple.IsManagedPeerDisposed(label));
			Assert.True(AlertsApple.IsDisposed(toast));
			Assert.Same(label, Assert.Single(toast.View.Children));
			Assert.IsType<UIStackView>(label.Superview);
		});
	}

	[Fact]
	public async Task DisposeTwiceDoesNotThrow()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var toast = new TestPlatformToast("dispose twice", UIColor.White, CGRect.Empty, UIColor.Black, UIFont.SystemFontOfSize(14)!, 0, 0);

			var exception = Record.Exception(() =>
			{
				toast.Dispose();
				toast.Dispose();
			});

			Assert.Null(exception);
			Assert.True(AlertsApple.IsManagedPeerDisposed(toast.View.GetLabel()));
		});
	}

	// Without the isDisposed guard, the later Dispose() would dispose the label.
	[Fact]
	public async Task DisposeWithoutDisposingManagedStateDoesNotThrowAndALaterDisposeIsANoOp()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var toast = new TestPlatformToast("finalizer path", UIColor.White, CGRect.Empty, UIColor.Black, UIFont.SystemFontOfSize(14)!, 0, 0);
			var label = toast.View.GetLabel();

			var exception = Record.Exception(() => toast.CallDispose(false));

			Assert.Null(exception);
			Assert.True(AlertsApple.IsDisposed(toast));
			Assert.False(AlertsApple.IsManagedPeerDisposed(label));

			exception = Record.Exception(toast.Dispose);

			Assert.Null(exception);
			Assert.False(AlertsApple.IsManagedPeerDisposed(label));
			Assert.Same(label, Assert.Single(toast.View.Children));
		});
	}

	[Fact]
	public async Task FinalizerDisposesWithoutManagedStateWhenTheToastIsNeverDisposed()
	{
		SetupBuilder();
		var finalizerCalls = new StrongBox<int>();

		await InvokeOnMainThreadAsync(() => CreateFinalizableToast(finalizerCalls));

		for (var attempt = 0; attempt < 40 && Volatile.Read(ref finalizerCalls.Value) == 0; attempt++)
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
			await InvokeOnMainThreadAsync(() => { });
		}

		Assert.Equal(1, Volatile.Read(ref finalizerCalls.Value));
	}

	[Fact]
	public async Task PlatformToastDoesNotLeakAfterItIsDismissed()
	{
		SetupBuilder();
		WeakReference toastReference = null!, alertViewReference = null!, labelReference = null!, backgroundReference = null!;

		await InvokeOnMainThreadAsync(() =>
		{
			var toast = new TestPlatformToast("leak", UIColor.White, CGRect.Empty, UIColor.Black, UIFont.SystemFontOfSize(14)!, 0, 0) { Duration = TimeSpan.FromSeconds(30) };
			toastReference = new WeakReference(toast);
			alertViewReference = new WeakReference(toast.View);
			labelReference = new WeakReference(toast.View.GetLabel());

			toast.Show();
			backgroundReference = new WeakReference(toast.View.GetBackground());
			toast.Dismiss();
		});

		await AssertionExtensions.WaitForGC(toastReference, alertViewReference, labelReference, backgroundReference);
	}

	// The toast is created in its own method so that nothing on the caller's stack keeps it alive.
	[MethodImpl(MethodImplOptions.NoInlining)]
	static void CreateFinalizableToast(StrongBox<int> finalizerCalls) =>
		_ = new FinalizableToast(finalizerCalls);

	sealed class FinalizableToast(StrongBox<int> finalizerCalls)
		: PlatformToast("finalizer", UIColor.White, CGRect.Empty, UIColor.Black, UIFont.SystemFontOfSize(14)!, 0, 0)
	{
		protected override void Dispose(bool isDisposing)
		{
			if (!isDisposing)
			{
				Interlocked.Increment(ref finalizerCalls.Value);
			}

			base.Dispose(isDisposing);
		}
	}
}