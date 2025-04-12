using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Maui.Animations;
using Microsoft.Maui.Handlers;
using Animation = Microsoft.Maui.Animations.Animation;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

static class AnimationExtensions
{
	public static void EnableAnimations(this IView view) => MockAnimationHandler.Prepare(view);

	// Inspired by Microsoft.Maui.Controls.Core.UnitTests.AnimationReadyHandler
	sealed class MockAnimationHandler : ViewHandler<IView, object>
	{
		const int millisecondTickIncrement = 16;

		MockAnimationHandler(IAnimationManager animationManager, IDispatcherProvider dispatcherProvider) : base(new PropertyMapper<IView>())
		{
			SetMauiContext(new AnimationEnabledMauiContext(animationManager, dispatcherProvider));
		}

		MockAnimationHandler() : this(new TestAnimationManager(new AsyncTicker()), new MockDispatcherProvider())
		{
		}

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

			public object GetService(Type serviceType)
			{
				if (serviceType == typeof(IAnimationManager))
				{
					return AnimationManager;
				}
				else if (serviceType == typeof(IDispatcher))
				{
					return DispatcherProvider.GetForCurrentThread() ?? throw new NullReferenceException();
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
					try
					{
						Fire?.Invoke();
					}
					catch (Exception e)
					{
						Trace.WriteLine(e);
					}

					if (!cancellationTokenSource.IsCancellationRequested)
					{
						await Task.Delay(TimeSpan.FromMilliseconds(millisecondTickIncrement));
					}
				}
			}

			public override void Stop() => cancellationTokenSource?.Cancel();

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
				var animationsList = this.animations.ToList();
				animationsList.ForEach(AnimationTick);

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
}