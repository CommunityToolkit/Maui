using CommunityToolkit.Maui.UnitTests.Mocks;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Extensions;

public class IServiceCollection_Tests : BaseTest
{
	readonly Type mockPageType = typeof(MockPage);
	readonly Type mockPageViewModelType = typeof(MockPageViewModel);
	const string customRoute = "MockCustomRoute";

	[Fact]
	public async Task IServiceCollection_VerifyTransient()
	{
		// Arrange
		var services = MauiApp.CreateBuilder().Services;
		var lifetime = ServiceLifetime.Transient;

		// Act
		services.AddTransient<MockPage, MockPageViewModel>();

		// Assert
		Assert.NotNull(services.Single(s => s.ServiceType.Equals(mockPageType) && s.Lifetime.Equals(lifetime)));
		Assert.NotNull(services.Single(s => s.ServiceType.Equals(mockPageViewModelType) && s.Lifetime.Equals(lifetime)));
	}

	[Fact]
	public async Task IServiceCollection_VerifyTransientShellRouteWithRouteParam()
	{
		// Arrange
		var services = MauiApp.CreateBuilder().Services;
		var lifetime = ServiceLifetime.Transient;
		var route = customRoute;

		// Act
		services.AddTransientWithShellRoute<MockPage, MockPageViewModel>(route);
		var resolvedPage = Routing.GetOrCreateContent(route, services.BuildServiceProvider());

		// Assert
		Assert.NotNull(services.Single(s => s.ServiceType.Equals(mockPageType) && s.Lifetime.Equals(lifetime)));
		Assert.NotNull(services.Single(s => s.ServiceType.Equals(mockPageViewModelType) && s.Lifetime.Equals(lifetime)));
		Assert.Equal(mockPageType, resolvedPage.GetType());
		Assert.Equal(mockPageViewModelType, resolvedPage.BindingContext.GetType());

		Routing.Clear();
	}

	[Fact]
	public async Task IServiceCollection_VerifyTransientShellRouteWithRouteAndRouteFactoryParam()
	{
		// Arrange
		var services = MauiApp.CreateBuilder().Services;
		var lifetime = ServiceLifetime.Transient;
		var factory = new MockPageRouteFactory();
		var route = customRoute;

		// Act
		services.AddTransientWithShellRoute<MockPage, MockPageViewModel>(route, factory);
		var resolvedPage = Routing.GetOrCreateContent(route, services.BuildServiceProvider());

		// Assert
		Assert.NotNull(services.Single(s => s.ServiceType.Equals(mockPageType) && s.Lifetime.Equals(lifetime)));
		Assert.NotNull(services.Single(s => s.ServiceType.Equals(mockPageViewModelType) && s.Lifetime.Equals(lifetime)));
		Assert.Equal(mockPageType, resolvedPage.GetType());
		Assert.Equal(mockPageViewModelType, resolvedPage.BindingContext.GetType());
		Assert.True(factory.WasInvoked);

		Routing.Clear();
	}

	[Fact]
	public async Task IServiceCollection_VerifySingleton()
	{
		// Arrange
		var services = MauiApp.CreateBuilder().Services;
		var lifetime = ServiceLifetime.Singleton;

		// Act
		services.AddSingleton<MockPage, MockPageViewModel>();

		// Assert
		Assert.NotNull(services.Single(s => s.ServiceType.Equals(mockPageType) && s.Lifetime.Equals(lifetime)));
		Assert.NotNull(services.Single(s => s.ServiceType.Equals(mockPageViewModelType) && s.Lifetime.Equals(lifetime)));
	}

	[Fact]
	public async Task IServiceCollection_VerifySingletonShellRouteWithRouteParam()
	{
		// Arrange
		var services = MauiApp.CreateBuilder().Services;
		var lifetime = ServiceLifetime.Singleton;
		var route = customRoute;

		// Act
		services.AddSingletonWithShellRoute<MockPage, MockPageViewModel>(route);
		var resolvedPage = Routing.GetOrCreateContent(route, services.BuildServiceProvider());

		// Assert
		Assert.NotNull(services.Single(s => s.ServiceType.Equals(mockPageType) && s.Lifetime.Equals(lifetime)));
		Assert.NotNull(services.Single(s => s.ServiceType.Equals(mockPageViewModelType) && s.Lifetime.Equals(lifetime)));
		Assert.Equal(mockPageType, resolvedPage.GetType());
		Assert.Equal(mockPageViewModelType, resolvedPage.BindingContext.GetType());

		Routing.Clear();
	}

	[Fact]
	public async Task IServiceCollection_VerifySingletonShellRouteWithRouteAndRouteFactoryParam()
	{
		// Arrange
		var services = MauiApp.CreateBuilder().Services;
		var lifetime = ServiceLifetime.Singleton;
		var factory = new MockPageRouteFactory();
		var route = customRoute;

		// Act
		services.AddSingletonWithShellRoute<MockPage, MockPageViewModel>(customRoute, factory);
		var resolvedPage = Routing.GetOrCreateContent(route, services.BuildServiceProvider());

		// Assert
		Assert.NotNull(services.Single(s => s.ServiceType.Equals(mockPageType) && s.Lifetime.Equals(lifetime)));
		Assert.NotNull(services.Single(s => s.ServiceType.Equals(mockPageViewModelType) && s.Lifetime.Equals(lifetime)));
		Assert.Equal(mockPageType, resolvedPage.GetType());
		Assert.Equal(mockPageViewModelType, resolvedPage.BindingContext.GetType());
		Assert.True(factory.WasInvoked);

		Routing.Clear();
	}

	[Fact]
	public async Task IServiceCollection_VerifyScoped()
	{
		// Arrange
		var services = MauiApp.CreateBuilder().Services;
		var lifetime = ServiceLifetime.Scoped;

		// Act
		services.AddScoped<MockPage, MockPageViewModel>();

		// Assert
		Assert.NotNull(services.Single(s => s.ServiceType.Equals(mockPageType) && s.Lifetime.Equals(lifetime)));
		Assert.NotNull(services.Single(s => s.ServiceType.Equals(mockPageViewModelType) && s.Lifetime.Equals(lifetime)));
	}

	[Fact]
	public async Task IServiceCollection_VerifyScopedShellRouteWithRouteParam()
	{
		// Arrange
		var services = MauiApp.CreateBuilder().Services;
		var lifetime = ServiceLifetime.Scoped;
		var route = customRoute;

		// Act
		services.AddScopedWithShellRoute<MockPage, MockPageViewModel>(route);
		var resolvedPage = Routing.GetOrCreateContent(route, services.BuildServiceProvider());

		// Assert
		Assert.NotNull(services.Single(s => s.ServiceType.Equals(mockPageType) && s.Lifetime.Equals(lifetime)));
		Assert.NotNull(services.Single(s => s.ServiceType.Equals(mockPageViewModelType) && s.Lifetime.Equals(lifetime)));
		Assert.Equal(mockPageType, resolvedPage.GetType());
		Assert.Equal(mockPageViewModelType, resolvedPage.BindingContext.GetType());

		Routing.Clear();
	}

	[Fact]
	public async Task IServiceCollection_VerifyScopedShellRouteWithRouteAndRouteFactoryParam()
	{
		// Arrange
		var services = MauiApp.CreateBuilder().Services;
		var lifetime = ServiceLifetime.Scoped;
		var factory = new MockPageRouteFactory();
		var route = customRoute;

		// Act
		services.AddScopedWithShellRoute<MockPage, MockPageViewModel>(route, factory);
		var resolvedPage = Routing.GetOrCreateContent(route, services.BuildServiceProvider());

		// Assert
		Assert.NotNull(services.Single(s => s.ServiceType.Equals(mockPageType) && s.Lifetime.Equals(lifetime)));
		Assert.NotNull(services.Single(s => s.ServiceType.Equals(mockPageViewModelType) && s.Lifetime.Equals(lifetime)));
		Assert.Equal(mockPageType, resolvedPage.GetType());
		Assert.Equal(mockPageViewModelType, resolvedPage.BindingContext.GetType());
		Assert.True(factory.WasInvoked);

		Routing.Clear();
	}
}
