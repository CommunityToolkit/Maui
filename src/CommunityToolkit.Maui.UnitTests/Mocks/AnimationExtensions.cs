using Microsoft.Maui.Animations;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

static class AnimationExtensions
{
	public static void EnableAnimations(this IView view) => MockAnimationHandler.Prepare(view);

	// Inspired by Microsoft.Maui.Controls.Core.UnitTests.AnimationReadyHandler
	class MockAnimationHandler : ViewHandler<IView, object>
	{
		MockAnimationHandler(IAnimationManager animationManager) : base(new PropertyMapper<IView>())
		{
			SetMauiContext(new AnimationEnabledMauiContext(animationManager));
		}

		MockAnimationHandler() : this(new TestAnimationManager(new AsyncTicker()))
		{
		}

		public IAnimationManager? AnimationManager => ((AnimationEnabledMauiContext?)MauiContext)?.AnimationManager;

		public static T Prepare<T>(T view) where T : IView
		{
			view.Handler = new MockAnimationHandler();

			return view;
		}

		protected override object CreatePlatformView() => new();

		class AnimationEnabledMauiContext(IAnimationManager manager) : IMauiContext, IServiceProvider
		{
			public IServiceProvider Services => this;

			public IAnimationManager AnimationManager { get; } = manager;

			IMauiHandlersFactory IMauiContext.Handlers => throw new NotSupportedException();

			public object GetService(Type serviceType)
			{
				if (serviceType == typeof(IAnimationManager))
				{
					return AnimationManager;
				}
				else if (serviceType == typeof(IDispatcher))
				{
					return new MockDispatcherProvider().GetForCurrentThread();
				}

				throw new NotSupportedException();
			}
		}

		sealed class AsyncTicker : Ticker, IDisposable
		{
			CancellationTokenSource? cancellationTokenSource;

			public override async void Start()
			{
				cancellationTokenSource = new();

				while (!cancellationTokenSource.IsCancellationRequested)
				{
					Fire?.Invoke();

					if (!cancellationTokenSource.IsCancellationRequested)
					{
						await Task.Delay(TimeSpan.FromMilliseconds(16));
					}
				}
			}

			public override void Stop() => cancellationTokenSource?.Cancel();

			public void Dispose()
			{
				cancellationTokenSource?.Dispose();
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
				var animations = this.animations.ToList();
				animations.ForEach(AnimationTick);

				if (!this.animations.Any())
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

					animation.Tick(16);
					if (animation.HasFinished)
					{
						this.animations.Remove(animation);
						animation.RemoveFromParent();
					}
				}
			}
		}
	}
}