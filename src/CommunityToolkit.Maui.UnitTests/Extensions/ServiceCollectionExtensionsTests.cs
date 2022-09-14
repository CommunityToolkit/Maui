using CommunityToolkit.Maui.UnitTests.Mocks;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Extensions;

public class ServiceCollectionExtensionsTests : BaseTest
{
	const string customRoute = "//MockCustomRoute";
	readonly Type mockPageType = typeof(MockPage);
	readonly Type mockPageViewModelType = typeof(MockPageViewModel);

	[Fact]
	public void IServiceCollection_VerifyTransient()
	{
		// Arrange
		var services = MauiApp.CreateBuilder().Services;
		const ServiceLifetime expectedServiceLifetime = ServiceLifetime.Transient;

		// Act
		services.AddTransient<MockPage, MockPageViewModel>();
		var serviceProvider = services.BuildServiceProvider();

		// Assert
		var mockPageServiceDescriptor = services.Single(s => s.ServiceType.Equals(mockPageType) && s.Lifetime.Equals(expectedServiceLifetime));
		var mockViewModelServiceDescriptor = services.Single(s => s.ServiceType.Equals(mockPageViewModelType) && s.Lifetime.Equals(expectedServiceLifetime));

		Assert.NotNull(mockPageServiceDescriptor);
		Assert.NotNull(mockViewModelServiceDescriptor);

		Assert.Equal(expectedServiceLifetime, mockPageServiceDescriptor.Lifetime);
		Assert.Equal(expectedServiceLifetime, mockViewModelServiceDescriptor.Lifetime);

		Assert.Equal(mockPageType, mockPageServiceDescriptor.ServiceType);
		Assert.Equal(mockPageViewModelType, mockViewModelServiceDescriptor.ServiceType);

		Assert.Equal(mockPageType, mockPageServiceDescriptor.ImplementationType);
		Assert.Equal(mockPageViewModelType, mockViewModelServiceDescriptor.ImplementationType);

		Assert.IsType<MockPage>(serviceProvider.GetRequiredService<MockPage>());
		Assert.IsType<MockPageViewModel>(serviceProvider.GetRequiredService<MockPageViewModel>());
	}

	[Fact]
	public void IServiceCollection_VerifyTransientShellRouteWithRouteParam()
	{
		// Arrange
		const ServiceLifetime expectedServiceLifetime = ServiceLifetime.Transient;
		const string route = customRoute;
		var services = MauiApp.CreateBuilder().Services;

		// Act
		services.AddTransientWithShellRoute<MockPage, MockPageViewModel>(route);
		var serviceProvider = services.BuildServiceProvider();

		// Assert
		var mockPageServiceDescriptor = services.Single(s => s.ServiceType.Equals(mockPageType) && s.Lifetime.Equals(expectedServiceLifetime));
		var mockViewModelServiceDescriptor = services.Single(s => s.ServiceType.Equals(mockPageViewModelType) && s.Lifetime.Equals(expectedServiceLifetime));

		Assert.NotNull(mockPageServiceDescriptor);
		Assert.NotNull(mockViewModelServiceDescriptor);

		Assert.IsType<MockPage>(Routing.GetOrCreateContent(route, serviceProvider));

		Assert.Equal(expectedServiceLifetime, mockPageServiceDescriptor.Lifetime);
		Assert.Equal(expectedServiceLifetime, mockViewModelServiceDescriptor.Lifetime);

		Assert.Equal(mockPageType, mockPageServiceDescriptor.ServiceType);
		Assert.Equal(mockPageViewModelType, mockViewModelServiceDescriptor.ServiceType);

		Assert.Equal(mockPageType, mockPageServiceDescriptor.ImplementationType);
		Assert.Equal(mockPageViewModelType, mockViewModelServiceDescriptor.ImplementationType);

		Assert.IsType<MockPage>(serviceProvider.GetRequiredService<MockPage>());
		Assert.IsType<MockPageViewModel>(serviceProvider.GetRequiredService<MockPageViewModel>());
	}

	[Fact]
	public void IServiceCollection_VerifyTransientShellRouteWithRouteAndRouteFactoryParam()
	{
		// Arrange
		const ServiceLifetime expectedServiceLifetime = ServiceLifetime.Transient;
		const string route = customRoute;
		var services = MauiApp.CreateBuilder().Services;
		var factory = new MockPageRouteFactory();

		// Act
		services.AddTransientWithShellRoute<MockPage, MockPageViewModel>(route, factory);
		var serviceProvider = services.BuildServiceProvider();

		// Assert
		var mockPageServiceDescriptor = services.Single(s => s.ServiceType.Equals(mockPageType) && s.Lifetime.Equals(expectedServiceLifetime));
		var mockViewModelServiceDescriptor = services.Single(s => s.ServiceType.Equals(mockPageViewModelType) && s.Lifetime.Equals(expectedServiceLifetime));

		Assert.NotNull(mockPageServiceDescriptor);
		Assert.NotNull(mockViewModelServiceDescriptor);

		Assert.IsType<MockPage>(Routing.GetOrCreateContent(route, serviceProvider));

		Assert.Equal(expectedServiceLifetime, mockPageServiceDescriptor.Lifetime);
		Assert.Equal(expectedServiceLifetime, mockViewModelServiceDescriptor.Lifetime);

		Assert.Equal(mockPageType, mockPageServiceDescriptor.ServiceType);
		Assert.Equal(mockPageViewModelType, mockViewModelServiceDescriptor.ServiceType);

		Assert.Equal(mockPageType, mockPageServiceDescriptor.ImplementationType);
		Assert.Equal(mockPageViewModelType, mockViewModelServiceDescriptor.ImplementationType);

		Assert.IsType<MockPage>(serviceProvider.GetRequiredService<MockPage>());
		Assert.IsType<MockPageViewModel>(serviceProvider.GetRequiredService<MockPageViewModel>());
		Assert.True(factory.WasInvoked);
	}

	[Fact]
	public void IServiceCollection_VerifySingleton()
	{
		// Arrange
		const ServiceLifetime expectedServiceLifetime = ServiceLifetime.Singleton;
		var services = MauiApp.CreateBuilder().Services;

		// Act
		services.AddSingleton<MockPage, MockPageViewModel>();
		var serviceProvider = services.BuildServiceProvider();

		// Assert
		var mockPageServiceDescriptor = services.Single(s => s.ServiceType.Equals(mockPageType) && s.Lifetime.Equals(expectedServiceLifetime));
		var mockViewModelServiceDescriptor = services.Single(s => s.ServiceType.Equals(mockPageViewModelType) && s.Lifetime.Equals(expectedServiceLifetime));

		Assert.NotNull(mockPageServiceDescriptor);
		Assert.NotNull(mockViewModelServiceDescriptor);

		Assert.Equal(expectedServiceLifetime, mockPageServiceDescriptor.Lifetime);
		Assert.Equal(expectedServiceLifetime, mockViewModelServiceDescriptor.Lifetime);

		Assert.Equal(mockPageType, mockPageServiceDescriptor.ServiceType);
		Assert.Equal(mockPageViewModelType, mockViewModelServiceDescriptor.ServiceType);

		Assert.Equal(mockPageType, mockPageServiceDescriptor.ImplementationType);
		Assert.Equal(mockPageViewModelType, mockViewModelServiceDescriptor.ImplementationType);

		Assert.IsType<MockPage>(serviceProvider.GetRequiredService<MockPage>());
		Assert.IsType<MockPageViewModel>(serviceProvider.GetRequiredService<MockPageViewModel>());
	}

	[Fact]
	public void IServiceCollection_VerifySingletonShellRouteWithRouteParam()
	{
		// Arrange
		const ServiceLifetime expectedServiceLifetime = ServiceLifetime.Singleton;
		const string route = customRoute;
		var services = MauiApp.CreateBuilder().Services;

		// Act
		services.AddSingletonWithShellRoute<MockPage, MockPageViewModel>(route);
		var serviceProvider = services.BuildServiceProvider();

		// Assert
		var mockPageServiceDescriptor = services.Single(s => s.ServiceType.Equals(mockPageType) && s.Lifetime.Equals(expectedServiceLifetime));
		var mockViewModelServiceDescriptor = services.Single(s => s.ServiceType.Equals(mockPageViewModelType) && s.Lifetime.Equals(expectedServiceLifetime));

		Assert.NotNull(mockPageServiceDescriptor);
		Assert.NotNull(mockViewModelServiceDescriptor);

		Assert.IsType<MockPage>(Routing.GetOrCreateContent(route, serviceProvider));

		Assert.Equal(expectedServiceLifetime, mockPageServiceDescriptor.Lifetime);
		Assert.Equal(expectedServiceLifetime, mockViewModelServiceDescriptor.Lifetime);

		Assert.Equal(mockPageType, mockPageServiceDescriptor.ServiceType);
		Assert.Equal(mockPageViewModelType, mockViewModelServiceDescriptor.ServiceType);

		Assert.Equal(mockPageType, mockPageServiceDescriptor.ImplementationType);
		Assert.Equal(mockPageViewModelType, mockViewModelServiceDescriptor.ImplementationType);

		Assert.IsType<MockPage>(serviceProvider.GetRequiredService<MockPage>());
		Assert.IsType<MockPageViewModel>(serviceProvider.GetRequiredService<MockPageViewModel>());
	}

	[Fact]
	public void IServiceCollection_VerifySingletonShellRouteWithRouteAndRouteFactoryParam()
	{
		// Arrange
		const ServiceLifetime expectedServiceLifetime = ServiceLifetime.Singleton;
		const string route = customRoute;
		var services = MauiApp.CreateBuilder().Services;
		var factory = new MockPageRouteFactory();

		// Act
		services.AddSingletonWithShellRoute<MockPage, MockPageViewModel>(customRoute, factory);
		var serviceProvider = services.BuildServiceProvider();

		// Assert
		var mockPageServiceDescriptor = services.Single(s => s.ServiceType.Equals(mockPageType) && s.Lifetime.Equals(expectedServiceLifetime));
		var mockViewModelServiceDescriptor = services.Single(s => s.ServiceType.Equals(mockPageViewModelType) && s.Lifetime.Equals(expectedServiceLifetime));

		Assert.NotNull(mockPageServiceDescriptor);
		Assert.NotNull(mockViewModelServiceDescriptor);

		Assert.IsType<MockPage>(Routing.GetOrCreateContent(route, serviceProvider));

		Assert.Equal(expectedServiceLifetime, mockPageServiceDescriptor.Lifetime);
		Assert.Equal(expectedServiceLifetime, mockViewModelServiceDescriptor.Lifetime);

		Assert.Equal(mockPageType, mockPageServiceDescriptor.ServiceType);
		Assert.Equal(mockPageViewModelType, mockViewModelServiceDescriptor.ServiceType);

		Assert.Equal(mockPageType, mockPageServiceDescriptor.ImplementationType);
		Assert.Equal(mockPageViewModelType, mockViewModelServiceDescriptor.ImplementationType);

		Assert.IsType<MockPage>(serviceProvider.GetRequiredService<MockPage>());
		Assert.IsType<MockPageViewModel>(serviceProvider.GetRequiredService<MockPageViewModel>());

		Assert.True(factory.WasInvoked);
	}

	[Fact]
	public void IServiceCollection_VerifyScoped()
	{
		// Arrange
		const ServiceLifetime expectedServiceLifetime = ServiceLifetime.Scoped;
		var services = MauiApp.CreateBuilder().Services;

		// Act
		services.AddScoped<MockPage, MockPageViewModel>();
		var serviceProvider = services.BuildServiceProvider();

		// Assert
		var mockPageServiceDescriptor = services.Single(s => s.ServiceType.Equals(mockPageType) && s.Lifetime.Equals(expectedServiceLifetime));
		var mockViewModelServiceDescriptor = services.Single(s => s.ServiceType.Equals(mockPageViewModelType) && s.Lifetime.Equals(expectedServiceLifetime));

		Assert.NotNull(mockPageServiceDescriptor);
		Assert.NotNull(mockViewModelServiceDescriptor);

		Assert.Equal(expectedServiceLifetime, mockPageServiceDescriptor.Lifetime);
		Assert.Equal(expectedServiceLifetime, mockViewModelServiceDescriptor.Lifetime);

		Assert.Equal(mockPageType, mockPageServiceDescriptor.ServiceType);
		Assert.Equal(mockPageViewModelType, mockViewModelServiceDescriptor.ServiceType);

		Assert.Equal(mockPageType, mockPageServiceDescriptor.ImplementationType);
		Assert.Equal(mockPageViewModelType, mockViewModelServiceDescriptor.ImplementationType);

		Assert.IsType<MockPage>(serviceProvider.GetRequiredService<MockPage>());
		Assert.IsType<MockPageViewModel>(serviceProvider.GetRequiredService<MockPageViewModel>());
	}

	[Fact]
	public void IServiceCollection_VerifyScopedShellRouteWithRouteParam()
	{
		// Arrange
		const ServiceLifetime expectedServiceLifetime = ServiceLifetime.Scoped;
		const string route = customRoute;
		var services = MauiApp.CreateBuilder().Services;

		// Act
		services.AddScopedWithShellRoute<MockPage, MockPageViewModel>(route);
		var serviceProvider = services.BuildServiceProvider();

		// Assert
		var mockPageServiceDescriptor = services.Single(s => s.ServiceType.Equals(mockPageType) && s.Lifetime.Equals(expectedServiceLifetime));
		var mockViewModelServiceDescriptor = services.Single(s => s.ServiceType.Equals(mockPageViewModelType) && s.Lifetime.Equals(expectedServiceLifetime));

		Assert.NotNull(mockPageServiceDescriptor);
		Assert.NotNull(mockViewModelServiceDescriptor);

		Assert.IsType<MockPage>(Routing.GetOrCreateContent(route, serviceProvider));

		Assert.Equal(expectedServiceLifetime, mockPageServiceDescriptor.Lifetime);
		Assert.Equal(expectedServiceLifetime, mockViewModelServiceDescriptor.Lifetime);

		Assert.Equal(mockPageType, mockPageServiceDescriptor.ServiceType);
		Assert.Equal(mockPageViewModelType, mockViewModelServiceDescriptor.ServiceType);

		Assert.Equal(mockPageType, mockPageServiceDescriptor.ImplementationType);
		Assert.Equal(mockPageViewModelType, mockViewModelServiceDescriptor.ImplementationType);

		Assert.IsType<MockPage>(serviceProvider.GetRequiredService<MockPage>());
		Assert.IsType<MockPageViewModel>(serviceProvider.GetRequiredService<MockPageViewModel>());
	}

	[Fact]
	public void IServiceCollection_VerifyScopedShellRouteWithRouteAndRouteFactoryParam()
	{
		// Arrange
		const ServiceLifetime expectedServiceLifetime = ServiceLifetime.Scoped;
		const string route = customRoute;
		var services = MauiApp.CreateBuilder().Services;
		var factory = new MockPageRouteFactory();

		// Act
		services.AddScopedWithShellRoute<MockPage, MockPageViewModel>(route, factory);
		var serviceProvider = services.BuildServiceProvider();

		// Assert
		var mockPageServiceDescriptor = services.Single(s => s.ServiceType.Equals(mockPageType) && s.Lifetime.Equals(expectedServiceLifetime));
		var mockViewModelServiceDescriptor = services.Single(s => s.ServiceType.Equals(mockPageViewModelType) && s.Lifetime.Equals(expectedServiceLifetime));

		Assert.NotNull(mockPageServiceDescriptor);
		Assert.NotNull(mockViewModelServiceDescriptor);

		Assert.IsType<MockPage>(Routing.GetOrCreateContent(route, serviceProvider));

		Assert.Equal(expectedServiceLifetime, mockPageServiceDescriptor.Lifetime);
		Assert.Equal(expectedServiceLifetime, mockViewModelServiceDescriptor.Lifetime);

		Assert.Equal(mockPageType, mockPageServiceDescriptor.ServiceType);
		Assert.Equal(mockPageViewModelType, mockViewModelServiceDescriptor.ServiceType);

		Assert.Equal(mockPageType, mockPageServiceDescriptor.ImplementationType);
		Assert.Equal(mockPageViewModelType, mockViewModelServiceDescriptor.ImplementationType);

		Assert.IsType<MockPage>(serviceProvider.GetRequiredService<MockPage>());
		Assert.IsType<MockPageViewModel>(serviceProvider.GetRequiredService<MockPageViewModel>());

		Assert.True(factory.WasInvoked);
	}

	protected override void Dispose(bool isDisposing)
	{
		Routing.Clear();
		base.Dispose(isDisposing);
	}
}