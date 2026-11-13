using System.ComponentModel;
using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Behaviors;

namespace CommunityToolkit.Maui.DeviceTests;

// A "To" animation runs under .NET MAUI's own animation handles, so a run started while another one runs interrupts it without AnimationBehavior's help
public abstract class ToAnimationTestBase<TAnimation> : AnimationContractTestBase<TAnimation> where TAnimation : BaseAnimation
{
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
}
