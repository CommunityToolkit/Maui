using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CommunityToolkit.Maui.Services;

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

		services.TryAdd(new ServiceDescriptor(typeof(TPopupView), _ => popup, lifetime));
		services.TryAdd(new ServiceDescriptor(typeof(TPopupViewModel), _ => viewModel, lifetime));
	}

	/// <inheritdoc />
	/// <remarks>This is an <see keyword="async"/> <see keyword="void"/> method. Use <see cref="ShowPopupAsync{T}"/> to <see keyword="await"/> this method</remarks>
	public void ShowPopup<T>(INavigation navigation, IPopupOptions? options = null) where T : notnull
	{		
		var popupContent = GetPopupContent(serviceProvider.GetRequiredService<T>());

		navigation.ShowPopup(popupContent, options);
	}

	/// <inheritdoc />
	public Task<IPopupResult> ShowPopupAsync<T>(INavigation navigation, IPopupOptions? options = null, CancellationToken token = default)
		where T : notnull
	{
		token.ThrowIfCancellationRequested();
		
		var popupContent = GetPopupContent(serviceProvider.GetRequiredService<T>());

		return navigation.ShowPopupAsync(popupContent, options, token);
	}

	/// <inheritdoc />
	public Task<IPopupResult<TResult>> ShowPopupAsync<T, TResult>(INavigation navigation,
		IPopupOptions? options = null,
		CancellationToken token = default)
		where T : notnull
	{
		token.ThrowIfCancellationRequested();
		var popupContent = GetPopupContent(serviceProvider.GetRequiredService<T>());

		return navigation.ShowPopupAsync<TResult>(popupContent, options, token);
	}

	/// <inheritdoc />
	public async Task ClosePopupAsync(INavigation navigation, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var popupContainer = GetCurrentPopupContainer(navigation);

		await popupContainer.Close(new PopupResult(false), cancellationToken);
	}

	/// <inheritdoc />
	public async Task ClosePopupAsync<T>(INavigation navigation, T result, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var popupContainer = GetCurrentPopupContainer(navigation);

		await popupContainer.Close(new PopupResult<T>(result, false), cancellationToken);
	}

	// All popups are now displayed in a PopupContainer (ContentPage) that is pushed modally to the screen, e.g. Navigation.PushModalAsync(popupContainer
	// We can use the ModalStack to retrieve the most recent popupContainer by retrieving all PopupContainers and return the most recent from the ModalStack  
	static PopupContainer GetCurrentPopupContainer(INavigation navigation) =>
		navigation.ModalStack.OfType<PopupContainer>().LastOrDefault() ?? throw new InvalidOperationException($"Unable to locate {nameof(Popup)}");

	View GetPopupContent<T>(T bindingContext)
	{
		if (bindingContext is View view)
		{
			return view;
		}

		if (serviceProvider.GetRequiredService(viewModelToViewMappings[typeof(T)]) is View content)
		{
			return content;
		}

		throw new InvalidOperationException($"Could not locate {typeof(T).FullName}");
	}
}