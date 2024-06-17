using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.UnitTests.Mocks;

namespace CommunityToolkit.Maui.UnitTests;

public abstract class BaseHandlerTest : BaseTest
{
	protected BaseHandlerTest()
	{
		CreateAndSetMockApplication(out var serviceProvider);
		ServiceProvider = serviceProvider;
	}

	protected IServiceProvider ServiceProvider { get; }

	protected static TElementHandler CreateElementHandler<TElementHandler>(Microsoft.Maui.IElement view, bool hasMauiContext = true)
		where TElementHandler : IElementHandler, new()
	{
		var mockElementHandler = new TElementHandler();
		mockElementHandler.SetVirtualView(view);

		if (hasMauiContext)
		{
			mockElementHandler.SetMauiContext(Application.Current?.Handler?.MauiContext ?? throw new NullReferenceException());
		}

		return mockElementHandler;
	}

	protected static TViewHandler CreateViewHandler<TViewHandler>(IView view, bool hasMauiContext = true)
		where TViewHandler : IViewHandler, new()
	{
		var mockViewHandler = new TViewHandler();
		mockViewHandler.SetVirtualView(view);

		if (hasMauiContext)
		{
			mockViewHandler.SetMauiContext(Application.Current?.Handler?.MauiContext ?? throw new NullReferenceException());
		}

		return mockViewHandler;
	}

	static void CreateAndSetMockApplication(out IServiceProvider serviceProvider)
	{
		var appBuilder = MauiApp.CreateBuilder()
								.UseMauiCommunityToolkit()
								.UseMauiApp<MockApplication>();

		appBuilder.Services.AddSingleton<ICameraProvider, MockCameraProvider>();

		var mauiApp = appBuilder.Build();

		var application = mauiApp.Services.GetRequiredService<IApplication>();
		serviceProvider = mauiApp.Services;

		IPlatformApplication.Current = (IPlatformApplication)application;

		application.Handler = new ApplicationHandlerStub();
		application.Handler.SetMauiContext(new HandlersContextStub(mauiApp.Services));
	}
}