using Microsoft.Maui.Animations;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

class HandlersContextStub : IMauiContext
{
	public HandlersContextStub(IServiceProvider services)
	{
		Services = services;
		Handlers = Services.GetRequiredService<IMauiHandlersFactory>();
		AnimationManager = services.GetService<IAnimationManager>() ?? throw new NullReferenceException();
	}

	public IServiceProvider Services { get; }

	public IMauiHandlersFactory Handlers { get; }

	public IAnimationManager AnimationManager { get; }
}