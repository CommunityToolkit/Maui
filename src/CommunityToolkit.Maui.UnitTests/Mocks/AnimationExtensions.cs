using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Maui.Animations;
using Microsoft.Maui.Handlers;
using Animation = Microsoft.Maui.Animations.Animation;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

static class AnimationExtensions
{
	public static void EnableAnimations(this IView view) => MockAnimationHandler.Prepare(view);

	// What MAUI's ticker reports when the system's animations are off (iOS Reduce Motion, Android's "Remove animations"): every tween jumps to its end
	public static void DisableSystemAnimations(this IView view) =>
		(view.Handler as MockAnimationHandler ?? throw new InvalidOperationException($"Call {nameof(EnableAnimations)} first")).DisableSystemAnimations();

	public static long GetElapsedAnimationMilliseconds(this IView view) =>
		view.Handler is MockAnimationHandler handler ? handler.ElapsedAnimationMilliseconds : throw new InvalidOperationException($"Call {nameof(EnableAnimations)} first");

	// Reports the mock clock once per ticker frame, after every animation has been ticked, so a test sees values that held still as well as values that changed
	public static IDisposable ObserveAnimationFrames(this IView view, Action<long> onFrame) =>
		view.Handler is MockAnimationHandler handler ? handler.ObserveFrames(onFrame) : throw new InvalidOperationException($"Call {nameof(EnableAnimations)} first");

	// Inspired by Microsoft.Maui.Controls.Core.UnitTests.AnimationReadyHandler
	sealed class MockAnimationHandler : ViewHandler<IView, object>
	{
		const int millisecondTickIncrement = 16;

		readonly TestAnimationManager animationManager;

		MockAnimationHandler(TestAnimationManager animationManager, IDispatcherProvider dispatcherProvider) : base(new PropertyMapper<IView>())
		{
			this.animationManager = animationManager;
			SetMauiContext(new AnimationEnabledMauiContext(animationManager, dispatcherProvider));
		}

		MockAnimationHandler() : this(new TestAnimationManager(new AsyncTicker()), new MockDispatcherProvider())
		{
		}

		// The ticker advances animations a fixed 16 ms per tick however long a tick really takes, so this clock does not depend on the speed of the machine
		public long ElapsedAnimationMilliseconds => animationManager.ElapsedMilliseconds;

		public IDisposable ObserveFrames(Action<long> onFrame) => animationManager.ObserveFrames(onFrame);

		public void DisableSystemAnimations() => ((AsyncTicker)animationManager.Ticker).DisableSystemAnimations();

		public static T Prepare<T>(T view) where T : IView
		{
			view.Handler = new MockAnimationHandler();

			return view;
		}

		protected override object CreatePlatformView() => new();

		class AnimationEnabledMauiContext(IAnimationManager manager, IDispatcherProvider dispatcherProvider) : IMauiContext, IServiceProvider
		{
			public IServiceProvider Services => this;

			public IAnimationManager AnimationManager { get; } = manager;

			public IDispatcherProvider DispatcherProvider { get; } = dispatcherProvider;

			IMauiHandlersFactory IMauiContext.Handlers => throw new NotSupportedException();

			public object? GetService(Type serviceType)
			{
				if (serviceType == typeof(IAnimationManager))
				{
					return AnimationManager;
				}
				else if (serviceType == typeof(IDispatcher))
				{
					return DispatcherProvider.GetForCurrentThread() ?? throw new NullReferenceException();
				}

				return null;
			}
		}

		sealed class AsyncTicker : Ticker, IDisposable
		{
			CancellationTokenSource? cancellationTokenSource;
			bool isRunning;

			// Base Ticker.IsRunning reports on a private Timer this mock's Start never creates, so it stays false;
			// Tweener.Start then launches a second Fire loop per animation segment and ticks run at double speed.
			public override bool IsRunning => isRunning;

			public void DisableSystemAnimations() => SystemEnabled = false;

			public override async void Start()
			{
				if (IsRunning)
				{
					return;
				}

				isRunning = true;

				// The loop keeps its own token even if Stop+Start replaces the field mid-Fire, so the
				// dying loop can never be resurrected by the successor loop's token.
				var cancellationTokenSource = this.cancellationTokenSource = new();

				try
				{
					while (!cancellationTokenSource.IsCancellationRequested)
					{
						try
						{
							Fire?.Invoke();
						}
						catch (Exception e)
						{
							Trace.WriteLine(e);
						}

						if (cancellationTokenSource.IsCancellationRequested)
						{
							break;
						}

						try
						{
							await Task.Delay(TimeSpan.FromMilliseconds(millisecondTickIncrement), cancellationTokenSource.Token);
						}
						catch (TaskCanceledException)
						{
							break;
						}
					}
				}
				finally
				{
					// A Tweener.Start running inside Fire may already have handed off to a fresh loop;
					// don't clear the flag that loop owns.
					if (ReferenceEquals(this.cancellationTokenSource, cancellationTokenSource))
					{
						isRunning = false;
					}
				}
			}

			// Clear the flag before cancelling: the next segment's Tweener.Start often runs inline inside
			// Fire (via the finished callback), and must see the loop as dead so it launches a new one.
			public override void Stop()
			{
				isRunning = false;
				cancellationTokenSource?.Cancel();
			}

			public void Dispose()
			{
				cancellationTokenSource?.Dispose();
				cancellationTokenSource = null;
			}
		}

		class TestAnimationManager : IAnimationManager
		{
			readonly List<Microsoft.Maui.Animations.Animation> animations = [];

			public TestAnimationManager(ITicker ticker)
			{
				Ticker = ticker;
				Ticker.Fire = OnFire;
			}

			readonly List<Action<long>> frameObservers = [];

			public long ElapsedMilliseconds { get; private set; }

			public IDisposable ObserveFrames(Action<long> onFrame)
			{
				frameObservers.Add(onFrame);

				return new FrameObservation(() => frameObservers.Remove(onFrame));
			}

			public double SpeedModifier { get; set; } = 1;

			public bool AutoStartTicker { get; set; } = false;

			public ITicker Ticker { get; }

			public void Add(Microsoft.Maui.Animations.Animation animation)
			{
				animations.Add(animation);
				if (AutoStartTicker && !Ticker.IsRunning)
				{
					Ticker.Start();
				}
			}

			public void Remove(Microsoft.Maui.Animations.Animation animation)
			{
				animations.Remove(animation);
				if (!animations.Any())
				{
					Ticker.Stop();
				}
			}

			void OnFire()
			{
				var hasTicked = false;
				var animationsList = this.animations.ToList();
				animationsList.ForEach(AnimationTick);

				if (hasTicked)
				{
					frameObservers.ToList().ForEach(observer => observer(ElapsedMilliseconds));
				}

				if (this.animations.Count <= 0)
				{
					Ticker.Stop();
				}

				void AnimationTick(Microsoft.Maui.Animations.Animation animation)
				{
					if (animation.HasFinished)
					{
						this.animations.Remove(animation);
						animation.RemoveFromParent();
						return;
					}

					// Counted before the tick: an animation that ends on it can resume its awaiter synchronously, which reads this clock
					if (!hasTicked)
					{
						hasTicked = true;
						ElapsedMilliseconds += millisecondTickIncrement;
					}

					animation.Tick(millisecondTickIncrement);
					if (animation.HasFinished)
					{
						this.animations.Remove(animation);
						animation.RemoveFromParent();
					}
				}
			}
		}
	}

	sealed class FrameObservation(Action stopObserving) : IDisposable
	{
		public void Dispose() => stopObserving();
	}
}