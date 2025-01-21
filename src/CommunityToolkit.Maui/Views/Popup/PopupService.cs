using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Maui.Core;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CommunityToolkit.Maui.Views;

/// <inheritdoc cref="IPopupService"/>
public class PopupService : IPopupService
{
	static readonly Dictionary<Type, Type> viewModelToViewMappings = [];

	readonly IServiceProvider serviceProvider;

	/// <summary>
	/// Creates a new instance of <see cref="PopupService"/>.
	/// </summary>
	/// <param name="serviceProvider">The <see cref="IServiceProvider"/> implementation.</param>
	[ActivatorUtilitiesConstructor]
	public PopupService(IServiceProvider serviceProvider)
	{
		this.serviceProvider = serviceProvider;
	}

	/// <summary>
	/// Creates a new instance of <see cref="PopupService"/>.
	/// </summary>
	public PopupService()
	{
		serviceProvider = IPlatformApplication.Current?.Services
		                  ?? throw new InvalidOperationException("Could not locate IServiceProvider");
	}

	internal static void AddPopup<
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TPopupView>(
		IServiceCollection services, ServiceLifetime lifetime)
		where TPopupView : IView
	{
		viewModelToViewMappings.TryAdd(typeof(TPopupView), typeof(TPopupView));
		Routing.RegisterRoute(typeof(TPopupView).FullName, typeof(TPopupView));

		services.Add(new ServiceDescriptor(typeof(TPopupView), typeof(TPopupView), lifetime));
	}

	internal static void AddPopup<
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TPopupView,
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TPopupViewModel>(
		IServiceCollection services, ServiceLifetime lifetime)
		where TPopupView : IView
		where TPopupViewModel : notnull
	{
		viewModelToViewMappings.TryAdd(typeof(TPopupViewModel), typeof(TPopupView));
		Routing.RegisterRoute(typeof(TPopupViewModel).FullName, typeof(TPopupView));

		services.TryAdd(new ServiceDescriptor(typeof(TPopupView), typeof(TPopupView), lifetime));
		services.TryAdd(new ServiceDescriptor(typeof(TPopupViewModel), typeof(TPopupViewModel), lifetime));
	}

	internal static void AddPopup<
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TPopupView,
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TPopupViewModel>(
		TPopupView popup, TPopupViewModel viewModel, IServiceCollection services, ServiceLifetime lifetime)
		where TPopupView : IView
		where TPopupViewModel : notnull
	{
		viewModelToViewMappings.TryAdd(typeof(TPopupViewModel), typeof(TPopupView));
		Routing.RegisterRoute(typeof(TPopupViewModel).FullName, typeof(TPopupView));

		services.TryAdd(new ServiceDescriptor(typeof(TPopupView), (_) => popup, lifetime));
		services.TryAdd(new ServiceDescriptor(typeof(TPopupViewModel), (_) => viewModel, lifetime));
	}

	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="TBindingContext"></typeparam>
	/// <param name="navigation"></param>
	/// <param name="options"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public async Task<PopupResult> ShowPopupAsync<TBindingContext>(INavigation navigation, PopupOptions options, CancellationToken cancellationToken)
		where TBindingContext : notnull
	{
		cancellationToken.ThrowIfCancellationRequested();
		var bindingContext = serviceProvider.GetRequiredService<TBindingContext>();
		var popupContent = GetPopupContent(bindingContext);

		return await navigation.ShowPopup(popupContent, options);
	}

	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="TBindingContext"></typeparam>
	/// <typeparam name="T"></typeparam>
	/// <param name="navigation"></param>
	/// <param name="options"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public async Task<PopupResult<T>> ShowPopupAsync<TBindingContext, T>(INavigation navigation, PopupOptions options,
		CancellationToken cancellationToken)
		where TBindingContext : notnull
	{
		cancellationToken.ThrowIfCancellationRequested();
		var bindingContext = serviceProvider.GetRequiredService<TBindingContext>();
		var popupContent = GetPopupContent(bindingContext);

		return await navigation.ShowPopup<T>(popupContent, options);
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="cancellationToken"></param>
	public async Task ClosePopupAsync(CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var popupLifecycleController = serviceProvider.GetRequiredService<PopupLifecycleController>();
		var popup = popupLifecycleController.GetCurrentPopup();
		if (popup is null)
		{
			return;
		}

		await popup.Close(new PopupResult(false));
		popupLifecycleController.UnregisterPopup(popup);
	}

	/// <summary>
	/// 
	/// </summary>
	public async Task ClosePopupAsync<T>(T result, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var popupLifecycleController = serviceProvider.GetRequiredService<PopupLifecycleController>();
		var popup = popupLifecycleController.GetCurrentPopup();
		if (popup is null)
		{
			return;
		}

		await popup.Close(new PopupResult<T>(result, false));
		popupLifecycleController.UnregisterPopup(popup);
	}

	View GetPopupContent<TBindingContext>(TBindingContext bindingContext)
	{
		if (bindingContext is View view)
		{
			return view;
		}

		if (serviceProvider.GetRequiredService(viewModelToViewMappings[typeof(TBindingContext)]) is View content)
		{
			return content;
		}

		throw new InvalidOperationException($"Could not locate a view for {typeof(TBindingContext).FullName}");
	}
}