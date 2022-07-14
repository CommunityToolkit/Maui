using CommunityToolkit.Maui.UnitTests.Mocks;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Extensions;

public class IServiceCollection_Tests : BaseTest
{
	readonly Type mockPageType = typeof(MockPage);
	readonly Type mockPageViewModelType = typeof(MockPageViewModel);
	const string customRoute = "MyCustomRoute";

	[Fact]
	public async Task IServiceCollection_VerifyTransient()
	{
		// Arrange
		var services = new ServiceCollection();
		var lifetime = ServiceLifetime.Transient;

		// Act
		services.AddTransient<MockPage, MockPageViewModel>();

		// Assert
		Assert.NotNull(services.Single(s => s.ServiceType.Equals(mockPageType) && s.Lifetime.Equals(lifetime)));
		Assert.NotNull(services.Single(s => s.ServiceType.Equals(mockPageViewModelType) && s.Lifetime.Equals(lifetime)));
	}

	[Fact]
	public async Task IServiceCollection_VerifyTransientShellRoute()
	{
		// Arrange
		var services = new ServiceCollection();
		var lifetime = ServiceLifetime.Transient;
		var route = mockPageType.Name;

		// Act
		services.AddTransientWithShellRoute<MockPage, MockPageViewModel>();
		var resolvedPage = Routing.GetOrCreateContent(route, services.BuildServiceProvider());

		// Assert
		Assert.NotNull(services.Single(s => s.ServiceType.Equals(mockPageType) && s.Lifetime.Equals(lifetime)));
		Assert.NotNull(services.Single(s => s.ServiceType.Equals(mockPageViewModelType) && s.Lifetime.Equals(lifetime)));
		Assert.Equal(mockPageType, resolvedPage.GetType());
		Assert.Equal(mockPageViewModelType, resolvedPage.BindingContext.GetType());

		Routing.Clear();
	}

	[Fact]
	public async Task IServiceCollection_VerifyTransientShellRouteWithRouteParam()
	{
		// Arrange
		var services = new ServiceCollection();
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
	public async Task IServiceCollection_VerifyTransientShellRouteWithRouteFactoryParam()
	{
		// Arrange
		var services = new ServiceCollection();
		var lifetime = ServiceLifetime.Transient;
		var factory = new MockRouteFactory();
		var route = mockPageType.Name;

		// Act
		services.AddTransientWithShellRoute<MockPage, MockPageViewModel>(factory: factory);
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
	public async Task IServiceCollection_VerifyTransientShellRouteWithRouteAndRouteFactoryParam()
	{
		// Arrange
		var services = new ServiceCollection();
		var lifetime = ServiceLifetime.Transient;
		var factory = new MockRouteFactory();
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
		var services = new ServiceCollection();
		var lifetime = ServiceLifetime.Singleton;

		// Act
		services.AddSingleton<MockPage, MockPageViewModel>();

		// Assert
		Assert.NotNull(services.Single(s => s.ServiceType.Equals(mockPageType) && s.Lifetime.Equals(lifetime)));
		Assert.NotNull(services.Single(s => s.ServiceType.Equals(mockPageViewModelType) && s.Lifetime.Equals(lifetime)));
	}

	[Fact]
	public async Task IServiceCollection_VerifySingletonShellRoute()
	{
		// Arrange
		var services = new ServiceCollection();
		var lifetime = ServiceLifetime.Singleton;
		var route = mockPageType.Name;

		// Act
		services.AddSingletonWithShellRoute<MockPage, MockPageViewModel>();
		var resolvedPage = Routing.GetOrCreateContent(route, services.BuildServiceProvider());

		// Assert
		Assert.NotNull(services.Single(s => s.ServiceType.Equals(mockPageType) && s.Lifetime.Equals(lifetime)));
		Assert.NotNull(services.Single(s => s.ServiceType.Equals(mockPageViewModelType) && s.Lifetime.Equals(lifetime)));
		Assert.Equal(mockPageType, resolvedPage.GetType());
		Assert.Equal(mockPageViewModelType, resolvedPage.BindingContext.GetType());

		Routing.Clear();
	}

	[Fact]
	public async Task IServiceCollection_VerifySingletonShellRouteWithRouteParam()
	{
		// Arrange
		var services = new ServiceCollection();
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
	public async Task IServiceCollection_VerifySingletonShellRouteWithRouteFactoryParam()
	{
		// Arrange
		var services = new ServiceCollection();
		var lifetime = ServiceLifetime.Singleton;
		var factory = new MockRouteFactory();
		var route = mockPageType.Name;

		// Act
		services.AddSingletonWithShellRoute<MockPage, MockPageViewModel>(factory: factory);
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
	public async Task IServiceCollection_VerifySingletonShellRouteWithRouteAndRouteFactoryParam()
	{
		// Arrange
		var services = new ServiceCollection();
		var lifetime = ServiceLifetime.Singleton;
		var factory = new MockRouteFactory();
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
		var services = new ServiceCollection();
		var lifetime = ServiceLifetime.Scoped;

		// Act
		services.AddScoped<MockPage, MockPageViewModel>();

		// Assert
		Assert.NotNull(services.Single(s => s.ServiceType.Equals(mockPageType) && s.Lifetime.Equals(lifetime)));
		Assert.NotNull(services.Single(s => s.ServiceType.Equals(mockPageViewModelType) && s.Lifetime.Equals(lifetime)));
	}

	[Fact]
	public async Task IServiceCollection_VerifyScopedShellRoute()
	{
		// Arrange
		var services = new ServiceCollection();
		var lifetime = ServiceLifetime.Scoped;
		var route = mockPageType.Name;

		// Act
		services.AddScopedWithShellRoute<MockPage, MockPageViewModel>();
		var resolvedPage = Routing.GetOrCreateContent(route, services.BuildServiceProvider());

		// Assert
		Assert.NotNull(services.Single(s => s.ServiceType.Equals(mockPageType) && s.Lifetime.Equals(lifetime)));
		Assert.NotNull(services.Single(s => s.ServiceType.Equals(mockPageViewModelType) && s.Lifetime.Equals(lifetime)));
		Assert.Equal(mockPageType, resolvedPage.GetType());
		Assert.Equal(mockPageViewModelType, resolvedPage.BindingContext.GetType());

		Routing.Clear();
	}

	[Fact]
	public async Task IServiceCollection_VerifyScopedShellRouteWithRouteParam()
	{
		// Arrange
		var services = new ServiceCollection();
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
	public async Task IServiceCollection_VerifyScopedShellRouteWithRouteFactoryParam()
	{
		// Arrange
		var services = new ServiceCollection();
		var lifetime = ServiceLifetime.Scoped;
		var factory = new MockRouteFactory();
		var route = mockPageType.Name;

		// Act
		services.AddScopedWithShellRoute<MockPage, MockPageViewModel>(factory: factory);
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
	public async Task IServiceCollection_VerifyScopedShellRouteWithRouteAndRouteFactoryParam()
	{
		// Arrange
		var services = new ServiceCollection();
		var lifetime = ServiceLifetime.Scoped;
		var factory = new MockRouteFactory();
		var route = mockPageType.Name;

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
