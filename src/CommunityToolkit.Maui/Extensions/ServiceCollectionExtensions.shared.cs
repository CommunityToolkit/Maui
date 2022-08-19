using System.ComponentModel;

// Using root CommunityToolkit.Maui namespace so these extension methods
// light up in MauiProgram when CommunityToolkit.Maui namespace is imported 
// for UseMauiCommunityToolkit();
namespace CommunityToolkit.Maui;

/// <summary>
/// Extension methods for registering Views and ViewModels in <see cref="IServiceCollection"/>
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Adds a <see cref="BindableObject"/> of the type specified in <typeparamref name="TView"/> and a ViewModel
	/// of the type specified in <typeparamref name="TViewModel"/> to the specified
	/// <see cref="IServiceCollection"/> with <see cref="ServiceLifetime.Transient"/> lifetime.
	/// </summary>
	/// <typeparam name="TView">The type of the View to add. Constrained to <see cref="BindableObject"/></typeparam>
	/// <typeparam name="TViewModel">The type of the ViewModel to add. Constrained to 
	/// <see cref="INotifyPropertyChanged"/></typeparam>
	/// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
	/// <returns>A reference to this instance after the operation has completed.</returns>
	/// <remarks>Developers are still responsible for assigning the injected instance of 
	/// <typeparamref name="TViewModel" />
	/// to the BindingContext property of <typeparamref name="TView" />.</remarks>
	public static IServiceCollection AddTransient<TView, TViewModel>(this IServiceCollection services)
		where TView : BindableObject
		where TViewModel : class, INotifyPropertyChanged
	{
		return services
			.AddTransient<TViewModel>()
			.AddTransient<TView>();
	}

	/// <summary>
	/// Adds a <see cref="NavigableElement"/> of the type specified in <typeparamref name="TView"/> and a ViewModel
	/// of the type specified in <typeparamref name="TViewModel"/> to the specified
	/// <see cref="IServiceCollection"/> with <see cref="ServiceLifetime.Transient"/> lifetime
	/// and registers a MAUI Shell route in <see cref="Routing"/> using the value of 
	/// <paramref name="route"/> as the route.
	/// </summary>
	/// <typeparam name="TView">The type of the View to add. Constrained to <see cref="NavigableElement"/></typeparam>
	/// <typeparam name="TViewModel">The type of the ViewModel to add. Constrained to 
	/// <see cref="INotifyPropertyChanged"/></typeparam>
	/// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
	/// <param name="route">Route at which this page will be registered within Shell routing.</param>
	/// <param name="factory">RouteFactory to be used while creating the <see cref="NavigableElement"/> 
	/// for the route. Defaults to TypeRouteFactory.</param>
	/// <returns>A reference to this instance after the operation has completed.</returns>
	/// <remarks>Developers are still responsible for assigning the injected instance of <typeparamref name="TViewModel" /> 
	/// to the BindingContext property of <typeparamref name="TView" />.</remarks>
	public static IServiceCollection AddTransientWithShellRoute<TView, TViewModel>(this IServiceCollection services, string route, RouteFactory? factory = null)
		where TView : NavigableElement
		where TViewModel : class, INotifyPropertyChanged
	{
		RegisterShellRoute<TView>(route, factory);
		return services.AddTransient<TView, TViewModel>();
	}

	/// <summary>
	/// Adds a <see cref="BindableObject"/> of the type specified in <typeparamref name="TView"/> and a ViewModel
	/// of the type specified in <typeparamref name="TViewModel"/> to the specified
	/// <see cref="IServiceCollection"/> with <see cref="ServiceLifetime.Singleton"/> lifetime.
	/// </summary>
	/// <typeparam name="TView">The type of the View to add. Constrained to <see cref="BindableObject"/></typeparam>
	/// <typeparam name="TViewModel">The type of the ViewModel to add. Constrained to 
	/// <see cref="INotifyPropertyChanged"/></typeparam>
	/// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
	/// <returns>A reference to this instance after the operation has completed.</returns>
	/// <remarks>Developers are still responsible for assigning the injected instance of <typeparamref name="TViewModel" /> 
	/// to the BindingContext property of <typeparamref name="TView" />.</remarks>
	public static IServiceCollection AddSingleton<TView, TViewModel>(this IServiceCollection services)
		where TView : BindableObject
		where TViewModel : class, INotifyPropertyChanged
	{
		return services
			.AddSingleton<TViewModel>()
			.AddSingleton<TView>();
	}

	/// <summary>
	/// Adds a <see cref="NavigableElement"/> of the type specified in <typeparamref name="TView"/> and a ViewModel
	/// of the type specified in <typeparamref name="TViewModel"/> to the specified
	/// <see cref="IServiceCollection"/> with <see cref="ServiceLifetime.Singleton"/> lifetime
	/// and registers a MAUI Shell route in <see cref="Routing"/> using the value of 
	/// <paramref name="route"/> as the route.
	/// </summary>
	/// <typeparam name="TView">The type of the View to add. Constrained to <see cref="NavigableElement"/></typeparam>
	/// <typeparam name="TViewModel">The type of the ViewModel to add. Constrained to 
	/// <see cref="INotifyPropertyChanged"/></typeparam>
	/// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
	/// <param name="route">Route at which this page will be registered within Shell routing.</param>
	/// <param name="factory">RouteFactory to be used while creating the <see cref="NavigableElement"/> 
	/// for the route. Defaults to TypeRouteFactory.</param>
	/// <returns>A reference to this instance after the operation has completed.</returns>
	/// <remarks>Developers are still responsible for assigning the injected instance of <typeparamref name="TViewModel" /> 
	/// to the BindingContext property of <typeparamref name="TView" />.</remarks>
	public static IServiceCollection AddSingletonWithShellRoute<TView, TViewModel>(this IServiceCollection services, string route, RouteFactory? factory = null)
		where TView : NavigableElement
		where TViewModel : class, INotifyPropertyChanged
	{
		RegisterShellRoute<TView>(route, factory);
		return services.AddSingleton<TView, TViewModel>();
	}

	/// <summary>
	/// Adds a <see cref="BindableObject"/> of the type specified in <typeparamref name="TView"/> and a ViewModel
	/// of the type specified in <typeparamref name="TViewModel"/> to the specified
	/// <see cref="IServiceCollection"/> with <see cref="ServiceLifetime.Scoped"/> lifetime.
	/// </summary>
	/// <typeparam name="TView">The type of the View to add. Constrained to <see cref="BindableObject"/></typeparam>
	/// <typeparam name="TViewModel">The type of the ViewModel to add. Constrained to 
	/// <see cref="INotifyPropertyChanged"/></typeparam>
	/// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
	/// <returns>A reference to this instance after the operation has completed.</returns>
	/// <remarks>Developers are still responsible for assigning the injected instance of <typeparamref name="TViewModel" /> 
	/// to the BindingContext property of <typeparamref name="TView" />.</remarks>
	public static IServiceCollection AddScoped<TView, TViewModel>(this IServiceCollection services)
		where TView : BindableObject
		where TViewModel : class, INotifyPropertyChanged
	{
		return services
			.AddScoped<TViewModel>()
			.AddScoped<TView>();
	}

	/// <summary>
	/// Adds a <see cref="NavigableElement"/> of the type specified in <typeparamref name="TView"/> and a ViewModel
	/// of the type specified in <typeparamref name="TViewModel"/> to the specified
	/// <see cref="IServiceCollection"/> with <see cref="ServiceLifetime.Scoped"/> lifetime
	/// and registers a MAUI Shell route in <see cref="Routing"/> using the value of 
	/// <paramref name="route"/> as the route.
	/// </summary>
	/// <typeparam name="TView">The type of the View to add. Constrained to <see cref="NavigableElement"/></typeparam>
	/// <typeparam name="TViewModel">The type of the ViewModel to add. Constrained to 
	/// <see cref="INotifyPropertyChanged"/></typeparam>
	/// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
	/// <param name="route">Route at which this page will be registered within Shell routing.</param>
	/// <param name="factory">RouteFactory to be used while creating the <see cref="NavigableElement"/> 
	/// for the route. Defaults to TypeRouteFactory.</param>
	/// <returns>A reference to this instance after the operation has completed.</returns>
	/// <remarks>Developers are still responsible for assigning the injected instance of <typeparamref name="TViewModel" /> 
	/// to the BindingContext property of <typeparamref name="TView" />.</remarks>
	public static IServiceCollection AddScopedWithShellRoute<TView, TViewModel>(this IServiceCollection services, string route, RouteFactory? factory = null)
		where TView : NavigableElement
		where TViewModel : class, INotifyPropertyChanged
	{
		RegisterShellRoute<TView>(route, factory);
		return services.AddScoped<TView, TViewModel>();
	}

	/// <summary>
	/// Registers routes in in <see cref="Routing"/> for Views registered using WithShellRouting methods.
	/// </summary>
	/// <typeparam name="TView">The type of the View to add. Constrained to <see cref="NavigableElement"/></typeparam>
	/// <param name="route">Route at which this page will be registered within Shell routing.</param>
	/// <param name="factory">RouteFactory to be used while creating the <see cref="NavigableElement"/> 
	/// for the route. Defaults to TypeRouteFactory.</param>
	static void RegisterShellRoute<TView>(string route, RouteFactory? factory = null)
		where TView : NavigableElement
	{
		if (factory is null)
		{
			Routing.RegisterRoute(route, typeof(TView));
		}
		else
		{
			Routing.RegisterRoute(route, factory);
		}
	}
}