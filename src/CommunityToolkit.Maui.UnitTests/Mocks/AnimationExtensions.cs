using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Animations;
using Microsoft.Maui.Controls;
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

		protected override object CreateNativeView() => new();

		class AnimationEnabledMauiContext : IMauiContext, IServiceProvider
		{
			public AnimationEnabledMauiContext(IAnimationManager manager)
			{
				AnimationManager = manager;
			}

			public IServiceProvider Services => this;

			public IMauiHandlersServiceProvider Handlers => throw new NotImplementedException();

			public IAnimationManager AnimationManager { get; }

			public object GetService(Type serviceType)
			{
				if (serviceType == typeof(IAnimationManager))
					return AnimationManager;

				throw new NotSupportedException();
			}
		}

		class AsyncTicker : Ticker
		{
			CancellationTokenSource? _cancellationTokenSource;

			public override async void Start()
			{
				_cancellationTokenSource = new();

				while (!_cancellationTokenSource.IsCancellationRequested)
				{
					Fire?.Invoke();

					if (!_cancellationTokenSource.IsCancellationRequested)
						await Task.Delay(TimeSpan.FromMilliseconds(16));
				}
			}

			public override void Stop() => _cancellationTokenSource?.Cancel();
		}

		class TestAnimationManager : IAnimationManager
		{
			readonly List<Microsoft.Maui.Animations.Animation> _animations = new();

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
				_animations.Add(animation);
				if (AutoStartTicker && !Ticker.IsRunning)
					Ticker.Start();
			}

			public void Remove(Microsoft.Maui.Animations.Animation animation)
			{
				_animations.Remove(animation);
				if (!_animations.Any())
					Ticker.Stop();
			}

			void OnFire()
			{
				var animations = _animations.ToList();
				animations.ForEach(animationTick);

				if (!_animations.Any())
					Ticker.Stop();

				void animationTick(Microsoft.Maui.Animations.Animation animation)
				{
					if (animation.HasFinished)
					{
						_animations.Remove(animation);
						animation.RemoveFromParent();
						return;
					}

					animation.Tick(16);
					if (animation.HasFinished)
					{
						_animations.Remove(animation);
						animation.RemoveFromParent();
					}
				}
			}
		}
	}
}