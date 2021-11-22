// Inspired by Microsoft.Maui.Controls.Core.UnitTests.AnimationReadyHandler
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Animations;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

class MockAnimationHandler : ViewHandler<IView, object>
{
	MockAnimationHandler(IAnimationManager animationManager) : base(new PropertyMapper<IView>())
	{
		SetMauiContext(new AnimationReadyMauiContext(animationManager));
	}

	MockAnimationHandler() : this(new TestAnimationManager(new BlockingTicker()))
	{
	}

	public IAnimationManager? AnimationManager => ((AnimationReadyMauiContext?)MauiContext)?.AnimationManager;

	public static T Prepare<T>(T view) where T : IView
	{
		view.Handler = new MockAnimationHandler();

		return view;
	}

	protected override object CreateNativeView() => new();

	class AnimationReadyMauiContext : IMauiContext, IServiceProvider
	{
		readonly IAnimationManager _animationManager;

		public AnimationReadyMauiContext(IAnimationManager? manager = null)
		{
			_animationManager = manager ?? new TestAnimationManager();
		}

		public IServiceProvider Services => this;

		public IMauiHandlersServiceProvider Handlers => throw new NotImplementedException();

		public IAnimationManager AnimationManager => _animationManager;

		public object GetService(Type serviceType)
		{
			if (serviceType == typeof(IAnimationManager))
				return _animationManager;

			throw new NotSupportedException();
		}
	}

	class BlockingTicker : Ticker
	{
		bool _enabled;

		public override void Start()
		{
			_enabled = true;

			while (_enabled)
			{
				Fire?.Invoke();
				Task.Delay(16).Wait();
			}
		}

		public override void Stop()
		{
			_enabled = false;
		}
	}

	class TestAnimationManager : IAnimationManager
	{
		readonly List<Microsoft.Maui.Animations.Animation> _animations = new();

		public TestAnimationManager(ITicker? ticker = null)
		{
			Ticker = ticker ?? new BlockingTicker();
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