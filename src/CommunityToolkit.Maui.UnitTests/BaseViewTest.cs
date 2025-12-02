using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.UnitTests.Mocks;
using CommunityToolkit.Maui.UnitTests.Services;
using FluentAssertions;

namespace CommunityToolkit.Maui.UnitTests;

public abstract class BaseViewTest : BaseTest
{
	protected BaseViewTest()
	{
		InitializeServicesAndSetMockApplication(out var serviceProvider);
		ServiceProvider = serviceProvider;
	}

	protected IServiceProvider ServiceProvider { get; }

	protected static TElementHandler CreateElementHandler<TElementHandler>(IElement view, bool doesRequireMauiContext = true)
		where TElementHandler : IElementHandler, new()
	{
		var mockElementHandler = new TElementHandler();
		mockElementHandler.SetVirtualView(view);

		if (doesRequireMauiContext)
		{
			mockElementHandler.SetMauiContext(Application.Current?.Handler?.MauiContext ?? throw new NullReferenceException());
		}

		return mockElementHandler;
	}

	protected static TViewHandler CreateViewHandler<TViewHandler>(IView view, bool doesRequireMauiContext = true)
		where TViewHandler : IViewHandler, new()
	{
		var mockViewHandler = new TViewHandler();
		mockViewHandler.SetVirtualView(view);

		if (doesRequireMauiContext)
		{
			mockViewHandler.SetMauiContext(Application.Current?.Handler?.MauiContext ?? throw new NullReferenceException());
		}

		return mockViewHandler;
	}

	static void InitializeServicesAndSetMockApplication(out IServiceProvider serviceProvider)
	{
#pragma warning disable CA1416 // Validate platform compatibility
		var appBuilder = MauiApp.CreateBuilder()
			.UseMauiCommunityToolkit()
			.UseMauiApp<MockApplication>();
#pragma warning restore CA1416 // Validate platform compatibility

		#region Register Services for CameraTests

		appBuilder.Services.AddSingleton<ICameraProvider, MockCameraProvider>();

		#endregion

		#region Register Services for PopupServiceTests
		appBuilder.Services.AddTransientPopup<LongLivedSelfClosingPopup, LongLivedMockPageViewModel>();
		appBuilder.Services.AddTransientPopup<ShortLivedSelfClosingPopup, ShortLivedMockPageViewModel>();
		appBuilder.Services.AddTransientPopup<GarbageCollectionHeavySelfClosingPopup, MockPageViewModel>();

		appBuilder.Services.AddTransientPopup<MockPopup>();
		#endregion

		var mauiApp = appBuilder.Build();
		serviceProvider = mauiApp.Services;

		var shell = new Shell();
		shell.Items.Add(new ContentPage());

		var application = (MockApplication)mauiApp.Services.GetRequiredService<IApplication>();
		application.AddWindow(new Window { Page = shell });

		IPlatformApplication.Current = application;

		application.Handler = new ApplicationHandlerStub();
		application.Handler.SetMauiContext(new HandlersContextStub(serviceProvider));

		CreateViewHandler<MockPageHandler>(shell);
	}
}