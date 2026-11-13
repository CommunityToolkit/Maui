using System.ComponentModel;
using System.Globalization;
using System.Threading.Channels;
using CommunityToolkit.Maui.Animations;
using Xunit.Sdk;

namespace CommunityToolkit.Maui.DeviceTests;

// A cancelled or interrupted run continues on the UI thread from inside the cancellation, and only the platform view shows whether every value
// .NET MAUI writes for the view came back, so these tests need the platform's ticker, UI thread and views
public abstract partial class AnimationTestBase : CommunityToolkitHandlerTestBase
{
	// Leaves time to cancel, trigger again or start another run in the middle of a run, also on an emulator that draws a frame every 100 ms
	protected const uint runLength = 1000;

	protected static readonly TimeSpan waitTimeout = TimeSpan.FromSeconds(10);

	// The restore writes the original value itself, so only floating-point noise is tolerated
	const double valueTolerance = 1e-6;

	const string frameProbeHandle = "CommunityToolkit.Maui.DeviceTests.FrameProbe";

	protected static Border CreateAnimatedView(IEnumerable<(BindableProperty Property, double Value)> originalValues)
	{
		var border = new Border
		{
			WidthRequest = 100,
			HeightRequest = 100,
			BackgroundColor = Colors.Green
		};

		foreach (var (property, value) in originalValues)
		{
			border.SetValue(property, value);
		}

		return border;
	}

	// .NET MAUI writes the transform on iOS and Mac Catalyst only for a view with a parent and a size (TransformationExtensions.UpdateTransformation)
	protected Task RunLoadedAsync(VisualElement view, Func<IPlatformViewHandler, string, Task> action) =>
		AttachAndRun(new Grid { Children = { view } }, async _ =>
		{
			await view.OnLoadedAsync();

			var handler = (IPlatformViewHandler)(view.Handler ?? throw new InvalidOperationException("The view has no handler."));
			await EnableAnimationsAsync(handler);
			await WaitUntilThePlatformViewShowsTheViewAsync(handler, view);

			await action(handler, DescribeNativeState(handler));
		});

	// Continuations run asynchronously: the test continues after the animation frame that met the last condition, as it would after a tap
	protected static Task WaitForValueAsync(VisualElement view, BindableProperty property, params Func<double, bool>[] conditionsInOrder)
	{
		var conditionsMet = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var nextCondition = 0;

		view.PropertyChanged += HandlePropertyChanged;

		return conditionsMet.Task.WaitAsync(waitTimeout);

		void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == property.PropertyName && conditionsInOrder[nextCondition]((double)view.GetValue(property)) && ++nextCondition == conditionsInOrder.Length)
			{
				view.PropertyChanged -= HandlePropertyChanged;
				conditionsMet.SetResult();
			}
		}
	}

	// Frames of the ticker that drives every animation of the view: a run that kept going after it ended would move the view in them
	protected static async Task WaitForTickerFramesAsync(VisualElement view, int frameCount = 3)
	{
		var framesSeen = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var frames = 0;

		new Microsoft.Maui.Controls.Animation(_ =>
		{
			if (++frames >= frameCount)
			{
				framesSeen.TrySetResult();
			}
		}).Commit(view, frameProbeHandle, length: 60_000, finished: (_, _) => framesSeen.TrySetResult());

		try
		{
			await framesSeen.Task.WaitAsync(waitTimeout);
		}
		finally
		{
			view.AbortAnimation(frameProbeHandle);
		}
	}

	protected static string? FindNativeMismatch(IPlatformViewHandler handler, VisualElement view)
	{
		var actual = DescribeNativeValues(handler);
		var expected = DescribeExpectedNativeValues(handler, view);

		return actual == expected ? null : $"The platform view has {actual}, but .NET MAUI writes {expected} for the view's values.";
	}

	protected static string DescribeNativeState(IPlatformViewHandler handler) => $"{DescribeNativeValues(handler)}; {DescribeNativeLayout(handler)}";

	// Rounded and culture-invariant (adding 0.0 turns -0 into 0), so equal native values give equal text on every device and in every culture
	protected static string FormatNativeValue(double value) => (Math.Round(value, 3) + 0.0).ToString("F3", CultureInfo.InvariantCulture);

	protected static void AssertPlatformViewFollows(IPlatformViewHandler handler, VisualElement view, string nativeStateAtRest, string when)
	{
		var mismatch = FindNativeMismatch(handler, view);
		Assert.True(mismatch is null, $"{mismatch} ({when})");

		var nativeState = DescribeNativeState(handler);
		Assert.True(nativeState != nativeStateAtRest, $"The platform view {when} still has its state at rest: {nativeState}.");
	}

	protected static void AssertAtOriginalValues(IPlatformViewHandler handler, VisualElement view, IReadOnlyList<(BindableProperty Property, double Value)> originalValues, string nativeStateAtRest, string when)
	{
		foreach (var (property, originalValue) in originalValues)
		{
			var value = (double)view.GetValue(property);
			Assert.True(Math.Abs(value - originalValue) <= valueTolerance, $"The view's {property.PropertyName} is {value} {when}, expected {originalValue}.");
		}

		var mismatch = FindNativeMismatch(handler, view);
		Assert.True(mismatch is null, $"{mismatch} ({when})");

		var nativeState = DescribeNativeState(handler);
		Assert.True(nativeState == nativeStateAtRest, $"The platform view {when} has {nativeState}; at rest it had {nativeStateAtRest}.");
	}

	protected static async Task AssertStaysAtOriginalValuesAsync(IPlatformViewHandler handler, VisualElement view, IReadOnlyList<(BindableProperty Property, double Value)> originalValues, string nativeStateAtRest, string when)
	{
		AssertAtOriginalValues(handler, view, originalValues, nativeStateAtRest, when);
		await WaitForTickerFramesAsync(view);
		AssertAtOriginalValues(handler, view, originalValues, nativeStateAtRest, $"a few frames {when}");
	}

	static async Task WaitUntilThePlatformViewShowsTheViewAsync(IPlatformViewHandler handler, VisualElement view)
	{
		string? problem = "The view has no parent or no size.";

		try
		{
			await AssertionExtensions.AssertEventually(
				() => view.Parent is not null && view.Frame.Width > 0 && view.Frame.Height > 0 && (problem = FindNativeMismatch(handler, view)) is null,
				timeout: 3000, interval: 20);
		}
		catch (XunitException)
		{
			throw new XunitException($"The platform view did not show the view's values at rest: {problem}");
		}
	}

	protected override async ValueTask DisposeAsyncCore()
	{
		try
		{
			await base.DisposeAsyncCore();
		}
		finally
		{
			await PlatformDisposeAsync();
		}
	}

	// AnimationBehavior starts animations without handing out their Task, so the animation reports each end itself, however it ended
	protected sealed partial class ObservedAnimation(BaseAnimation animation) : BaseAnimation
	{
		readonly Channel<bool> endedAnimations = Channel.CreateUnbounded<bool>();

		public async Task WaitForEndedAnimationsAsync(int count)
		{
			for (var i = 0; i < count; i++)
			{
				await endedAnimations.Reader.ReadAsync().AsTask().WaitAsync(waitTimeout);
			}
		}

		public override async Task Animate(VisualElement view, CancellationToken token = default)
		{
			try
			{
				await animation.Animate(view, token);
			}
			finally
			{
				endedAnimations.Writer.TryWrite(true);
			}
		}
	}
}
