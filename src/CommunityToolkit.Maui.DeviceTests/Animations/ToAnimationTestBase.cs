using System.ComponentModel;
using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Behaviors;

namespace CommunityToolkit.Maui.DeviceTests;

// A "To" animation runs under .NET MAUI's own animation handles, so a run started while another one runs interrupts it without AnimationBehavior's help
public abstract class ToAnimationTestBase<TAnimation> : AnimationContractTestBase<TAnimation> where TAnimation : BaseAnimation
{
	protected abstract Task<bool> AppToAnimation(VisualElement view, double target);

	[Fact]
	public async Task AnimationStartedMidRunStopsTheRunningOneAndReturnsTheViewToItsOriginalValues()
	{
		SetupBuilder();

		var view = CreateAnimatedView(OriginalValues);

		await RunLoadedAsync(view, async (handler, nativeStateAtRest) =>
		{
			var midRun = WaitForMidRunAsync(view);
			var firstRun = CreateAnimation().Animate(view);
			await midRun;

			var peakAgain = WaitForPeakAsync(view);
			var secondRun = CreateAnimation().Animate(view);
			await Task.WhenAll(firstRun, secondRun).WaitAsync(waitTimeout);

			// The wait task completes through a continuation, so it can trail the end of the run by a moment
			var peakError = await Record.ExceptionAsync(() => peakAgain);
			Assert.True(peakError is null, "The second animation did not reach its peak.");
			AssertAtOriginalValues(handler, view, OriginalValues, nativeStateAtRest, "after both animations ended");
		});
	}

	[Fact]
	public async Task SystemAnimationsSwitchedOffMidRunReturnTheViewToItsOriginalValueBeforeTheSwitchReturns()
	{
		SetupBuilder();

		var view = CreateAnimatedView(OriginalValues);
		var originalValue = OriginalValues.Single(original => original.Property == PeakProperty).Value;

		await RunLoadedAsync(view, async (handler, nativeStateAtRest) =>
		{
			var midRun = WaitForMidRunAsync(view);
			var run = CreateAnimation().Animate(view);
			await midRun;

			var valueRightAfterTheSwitch = double.NaN;
			await DisableSystemAnimationsAsync(handler, () => valueRightAfterTheSwitch = (double)view.GetValue(PeakProperty));

			// MAUI's ticker force-finishes the tween inside the switch, so no frame may show the view at the animation's target in between
			Assert.True(Math.Abs(valueRightAfterTheSwitch - originalValue) <= 1e-6, $"The view's {PeakProperty.PropertyName} is {valueRightAfterTheSwitch} right after the system's animations were switched off, expected the original {originalValue}.");

			await run.WaitAsync(waitTimeout);
			Assert.True(run.IsCompletedSuccessfully, $"The run ended {run.Status}, expected it to complete.");
			AssertAtOriginalValues(handler, view, OriginalValues, nativeStateAtRest, "after the system's animations were switched off mid-run");
		});
	}

	[Fact]
	public async Task CancelAnimationsMidRunReturnsTheViewToItsOriginalValuesAndCompletesTheTask()
	{
		SetupBuilder();

		var view = CreateAnimatedView(OriginalValues);

		await RunLoadedAsync(view, async (handler, nativeStateAtRest) =>
		{
			var midRun = WaitForMidRunAsync(view);
			var run = CreateAnimation().Animate(view);
			await midRun;

			// On the UI thread, as from a button's Clicked handler: nothing takes over, so the run returns the view itself
			view.CancelAnimations();

			await run.WaitAsync(waitTimeout);
			Assert.True(run.IsCompletedSuccessfully, $"The run ended {run.Status}; a run stopped by CancelAnimations completes.");
			await AssertStaysAtOriginalValuesAsync(handler, view, OriginalValues, nativeStateAtRest, "after CancelAnimations stopped the run");
		});
	}

	[Fact]
	public async Task AnimationBehaviorTriggeredMidRunTakesOverWithoutReturningTheViewFirst()
	{
		SetupBuilder();

		var animation = new ObservedAnimation(CreateAnimation());
		var behavior = new AnimationBehavior { AnimationType = animation };
		var view = CreateAnimatedView(OriginalValues);
		view.Behaviors.Add(behavior);
		var originalValue = OriginalValues.Single(original => original.Property == PeakProperty).Value;

		await RunLoadedAsync(view, async (handler, nativeStateAtRest) =>
		{
			var midRun = WaitForMidRunAsync(view);
			behavior.AnimateCommand.Execute(CancellationToken.None);
			await midRun;

			var restoreWrites = 0;
			var peakReached = false;
			view.PropertyChanged += CountRestoreWritesUntilThePeak;
			try
			{
				behavior.AnimateCommand.Execute(CancellationToken.None);
				await animation.WaitForEndedAnimationsAsync(2);
			}
			finally
			{
				view.PropertyChanged -= CountRestoreWritesUntilThePeak;
			}

			Assert.True(peakReached, "The animation triggered again did not reach its peak.");
			Assert.True(restoreWrites is 0, $"The interrupted run wrote the original value {restoreWrites} time(s) before the new run's peak; it must leave the view to the new run.");
			AssertAtOriginalValues(handler, view, OriginalValues, nativeStateAtRest, "after the animation triggered again");

			// A restore writes the original value itself, while the new run moves from where the view is toward the peak
			void CountRestoreWritesUntilThePeak(object? sender, PropertyChangedEventArgs e)
			{
				if (peakReached || e.PropertyName != PeakProperty.PropertyName)
				{
					return;
				}

				var value = (double)view.GetValue(PeakProperty);
				if (value == originalValue)
				{
					restoreWrites++;
				}

				peakReached = Math.Abs(value - PeakValue) <= peakTolerance;
			}
		});
	}

	[Fact]
	public async Task AnimationBehaviorTriggeredOffTheUIThreadMidRunTakesOverWithoutReturningToRest()
	{
		SetupBuilder();

		var animation = new ObservedAnimation(CreateAnimation());
		var behavior = new AnimationBehavior { AnimationType = animation };
		var view = CreateAnimatedView(OriginalValues);
		view.Behaviors.Add(behavior);
		var originalValue = OriginalValues.Single(original => original.Property == PeakProperty).Value;

		await RunLoadedAsync(view, async (handler, nativeStateAtRest) =>
		{
			var midRun = WaitForMidRunAsync(view);
			behavior.AnimateCommand.Execute(CancellationToken.None);
			await midRun;

			var restoreWrites = 0;
			var writesOffTheUIThread = 0;
			var peakReached = false;
			view.PropertyChanged += CountWritesUntilThePeak;
			try
			{
				await Task.Run(() => behavior.AnimateCommand.Execute(CancellationToken.None));
				await animation.WaitForEndedAnimationsAsync(2);
			}
			finally
			{
				view.PropertyChanged -= CountWritesUntilThePeak;
			}

			Assert.True(peakReached, "The animation triggered from a background thread did not reach its peak.");
			Assert.True(restoreWrites is 0, $"The interrupted run wrote the original value {restoreWrites} time(s) before the new run's peak; it must leave the view to the new run.");
			Assert.True(writesOffTheUIThread is 0, $"The view changed {writesOffTheUIThread} time(s) off the UI thread.");
			AssertAtOriginalValues(handler, view, OriginalValues, nativeStateAtRest, "after the animation triggered from a background thread");

			void CountWritesUntilThePeak(object? sender, PropertyChangedEventArgs e)
			{
				if (view.Dispatcher.IsDispatchRequired)
				{
					writesOffTheUIThread++;
				}

				if (peakReached || e.PropertyName != PeakProperty.PropertyName)
				{
					return;
				}

				var value = (double)view.GetValue(PeakProperty);
				if (value == originalValue)
				{
					restoreWrites++;
				}

				peakReached = Math.Abs(value - PeakValue) <= peakTolerance;
			}
		});
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task AppsOwnToAnimationHoldingTheCurrentValueTakesOverWithoutTheInterruptedRunReturningTheView(bool cancelAnimationsFirst)
	{
		SetupBuilder();

		var view = CreateAnimatedView(OriginalValues);
		var originalValue = OriginalValues.Single(original => original.Property == PeakProperty).Value;

		await RunLoadedAsync(view, async (handler, nativeStateAtRest) =>
		{
			var midRun = WaitForMidRunAsync(view);
			var run = CreateAnimation().Animate(view);
			await midRun;

			var restoreWrites = 0;
			var appTarget = (double)view.GetValue(PeakProperty);
			view.PropertyChanged += CountOriginalValueWrites;
			try
			{
				if (cancelAnimationsFirst)
				{
					view.CancelAnimations();
				}

				var appRun = AppToAnimation(view, appTarget);
				await Task.WhenAll(run, appRun).WaitAsync(waitTimeout);
			}
			finally
			{
				view.PropertyChanged -= CountOriginalValueWrites;
			}

			Assert.True(restoreWrites is 0, $"The interrupted run wrote the original value {restoreWrites} time(s) under the app's own To animation; it must leave the view to it.");
			var valueAtTheEnd = (double)view.GetValue(PeakProperty);
			Assert.True(Math.Abs(valueAtTheEnd - appTarget) <= peakTolerance, $"The view ended at {valueAtTheEnd}, expected the app's target {appTarget}.");

			void CountOriginalValueWrites(object? sender, PropertyChangedEventArgs e)
			{
				if (e.PropertyName == PeakProperty.PropertyName && (double)view.GetValue(PeakProperty) == originalValue)
				{
					restoreWrites++;
				}
			}
		});
	}

	[Fact]
	public async Task AnimationStartedAfterTheAppSetTheValueInTheSameTurnAsCancelAnimationsReturnsToTheAppsValue()
	{
		SetupBuilder();

		var view = CreateAnimatedView(OriginalValues);
		var originalValue = OriginalValues.Single(original => original.Property == PeakProperty).Value;
		var appValue = (originalValue + PeakValue) / 2;

		await RunLoadedAsync(view, async (handler, nativeStateAtRest) =>
		{
			var midRun = WaitForMidRunAsync(view);
			var interruptedRun = CreateAnimation().Animate(view);
			await midRun;

			view.CancelAnimations();
			view.SetValue(PeakProperty, appValue);
			await interruptedRun.WaitAsync(waitTimeout);

			await CreateAnimation().Animate(view).WaitAsync(waitTimeout);

			var valueAfterSecondRun = (double)view.GetValue(PeakProperty);
			Assert.True(Math.Abs(valueAfterSecondRun - appValue) <= peakTolerance, $"The second run ended at {valueAfterSecondRun}; it must return to the value {appValue} the app set.");
		});
	}
}
