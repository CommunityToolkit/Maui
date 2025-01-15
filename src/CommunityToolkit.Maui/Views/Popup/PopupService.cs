using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using CommunityToolkit.Maui.Core;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CommunityToolkit.Maui.Views;

/// <inheritdoc cref="IPopupService"/>
public class PopupService : IPopupService
{
	static readonly Dictionary<Type, Type> viewModelToViewMappings = [];

	readonly IServiceProvider serviceProvider;
	readonly IDispatcher dispatcher;

	/// <summary>
	/// Creates a new instance of <see cref="PopupService"/>.
	/// </summary>
	/// <param name="serviceProvider">The <see cref="IServiceProvider"/> implementation.</param>
	/// <param name="dispatcherProvider"></param>
	[ActivatorUtilitiesConstructor]
	public PopupService(IServiceProvider serviceProvider, IDispatcherProvider dispatcherProvider)
	{
		this.serviceProvider = serviceProvider;
		dispatcher = dispatcherProvider.GetForCurrentThread()
						?? throw new InvalidOperationException("Could not locate IDispatcher");
	}

	/// <summary>
	/// Creates a new instance of <see cref="PopupService"/>.
	/// </summary>
	public PopupService()
	{
		serviceProvider = IPlatformApplication.Current?.Services
			?? throw new InvalidOperationException("Could not locate IServiceProvider");

		dispatcher = Microsoft.Maui.Controls.Application.Current?.Dispatcher
			?? throw new InvalidOperationException("Could not locate IDispatcher");
	}

	internal static void AddPopup<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TPopupView>(IServiceCollection services, ServiceLifetime lifetime)
		where TPopupView : IView
	{
		Routing.RegisterRoute(typeof(TPopupView).FullName, typeof(TPopupView));

		services.Add(new ServiceDescriptor(typeof(TPopupView), typeof(TPopupView), lifetime));
	}

	internal static void AddPopup<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TPopupView, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TPopupViewModel>(IServiceCollection services, ServiceLifetime lifetime)
		where TPopupView : IView
		where TPopupViewModel : notnull
	{
		viewModelToViewMappings.TryAdd(typeof(TPopupViewModel), typeof(TPopupView));
		Routing.RegisterRoute(typeof(TPopupViewModel).FullName, typeof(TPopupView));

		services.TryAdd(new ServiceDescriptor(typeof(TPopupView), typeof(TPopupView), lifetime));
		services.TryAdd(new ServiceDescriptor(typeof(TPopupViewModel), typeof(TPopupViewModel), lifetime));
	}

	internal static void AddPopup<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TPopupView, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TPopupViewModel>(TPopupView popup, TPopupViewModel viewModel, IServiceCollection services, ServiceLifetime lifetime)
		where TPopupView : IView
		where TPopupViewModel : notnull
	{
		viewModelToViewMappings.TryAdd(typeof(TPopupViewModel), typeof(TPopupView));
		Routing.RegisterRoute(typeof(TPopupViewModel).FullName, typeof(TPopupView));

		services.TryAdd(new ServiceDescriptor(typeof(TPopupView), (_) => popup, lifetime));
		services.TryAdd(new ServiceDescriptor(typeof(TPopupViewModel), (_) => viewModel, lifetime));
	}

	void EnsureMainThreadIsUsed([CallerMemberName] string? callerName = null)
	{
		if (dispatcher.IsDispatchRequired)
		{
			throw new InvalidOperationException($"{callerName} must be called from the main thread.");
		}
	}

	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="TBindingContext"></typeparam>
	/// <param name="options"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public async Task<PopupResult> ShowPopupAsync<TBindingContext>(PopupOptions<TBindingContext> options, CancellationToken cancellationToken)
		where TBindingContext : notnull
	{
		TaskCompletionSource<PopupResult> taskCompletionSource = new();
		var bindingContext = serviceProvider.GetRequiredService<TBindingContext>();
		var popup = GetPopup(options, bindingContext, taskCompletionSource);
		await ShowPopup(popup);
		return await taskCompletionSource.Task.WaitAsync(cancellationToken);
	}

	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="TBindingContext"></typeparam>
	/// <typeparam name="T"></typeparam>
	/// <param name="options"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public async Task<PopupResult<T>> ShowPopupAsync<TBindingContext, T>(PopupOptions<TBindingContext> options, CancellationToken cancellationToken)
		where TBindingContext : notnull
	{
		TaskCompletionSource<PopupResult<T>> taskCompletionSource = new();
		var bindingContext = serviceProvider.GetRequiredService<TBindingContext>();
		var popup = GetPopup(options, bindingContext, taskCompletionSource);
		await ShowPopup(popup);
		return await taskCompletionSource.Task.WaitAsync(cancellationToken);
	}

	/// <summary>
	/// 
	/// </summary>
	public async Task ClosePopup()
	{
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
	public async Task ClosePopup<T>(T result)
	{
		var popupLifecycleController = serviceProvider.GetRequiredService<PopupLifecycleController>();
		var popup = popupLifecycleController.GetCurrentPopup();
		if (popup is null)
		{
			return;
		}

		await popup.Close(new PopupResult<T>(result, false));
		popupLifecycleController.UnregisterPopup(popup);
	}

	async Task ShowPopup(PopupContainer popupContainer)
	{
		EnsureMainThreadIsUsed();
		var popupLifecycleController = serviceProvider.GetRequiredService<PopupLifecycleController>();
		popupLifecycleController.RegisterPopup(popupContainer);
		var navigation = Application.Current?.Windows[^1].Page?.Navigation ?? throw new InvalidOperationException("Unable to get navigation");
		await navigation.PushModalAsync(popupContainer);
	}

	PopupContainer GetPopup<TBindingContext>(PopupOptions<TBindingContext> options, TBindingContext bindingContext, TaskCompletionSource<PopupResult> taskCompletionSource)
	{
		var content = GetPopupContent(bindingContext);
		
		var view = new Grid()
		{
			new Border()
			{
				Content = content
			}
		};
		var popup = new PopupContainer(content, taskCompletionSource)
		{
			BackgroundColor = options.BackgroundColor ?? Color.FromRgba(0, 0, 0, 0.4), // https://rgbacolorpicker.com/rgba-to-hex,
			CanBeDismissedByTappingOutsideOfPopup = options.CanBeDismissedByTappingOutsideOfPopup,
			Content = view,
			BindingContext = bindingContext
		};

		popup.Appearing += (s, e) => options.OnOpened?.Invoke(bindingContext);
		popup.Disappearing += (s, e) => options.OnClosed?.Invoke(bindingContext);
		
		view.BindingContext = bindingContext;

		if (options.CanBeDismissedByTappingOutsideOfPopup)
		{
			view.GestureRecognizers.Add(new TapGestureRecognizer()
			{
				Command = new Command(async () =>
				{
					options.OnTappingOutsideOfPopup?.Invoke();
					await popup.Close(new PopupResult(true));
				})
			});
		}

		return popup;
	}

	Popup GetPopupContent<TBindingContext>(TBindingContext bindingContext)
	{
		if (bindingContext is Popup view)
		{
			return view;
		}

		if (serviceProvider.GetRequiredService(viewModelToViewMappings[typeof(TBindingContext)]) is Popup content)
		{
			return content;
		}

		throw new InvalidOperationException($"Could not locate a view for {typeof(TBindingContext).FullName}");
	}

	PopupContainer<T> GetPopup<TBindingContext, T>(PopupOptions<TBindingContext> options, TBindingContext bindingContext, TaskCompletionSource<PopupResult<T>> taskCompletionSource)
	{
		var content = GetPopupContent<TBindingContext, T>(bindingContext);
		
		var view = new Grid()
		{
			new Border()
			{
				Content = content
			}
		};
		var popup = new PopupContainer<T>(content, taskCompletionSource)
		{
			BackgroundColor = options.BackgroundColor ?? Color.FromRgba(0, 0, 0, 0.4), // https://rgbacolorpicker.com/rgba-to-hex,
			CanBeDismissedByTappingOutsideOfPopup = options.CanBeDismissedByTappingOutsideOfPopup,
			Content = view,
			BindingContext = bindingContext
		};

		popup.Appearing += (s, e) => options.OnOpened?.Invoke(bindingContext);
		popup.Disappearing += (s, e) => options.OnClosed?.Invoke(bindingContext);

		view.BindingContext = bindingContext;

		if (options.CanBeDismissedByTappingOutsideOfPopup)
		{
			view.GestureRecognizers.Add(new TapGestureRecognizer()
			{
				Command = new Command(async () =>
				{
					options.OnTappingOutsideOfPopup?.Invoke();
					await popup.Close(new PopupResult(true));
				})
			});
		}

		return popup;
	}

	Popup<T> GetPopupContent<TBindingContext, T>(TBindingContext bindingContext)
	{
		if (bindingContext is Popup<T> view)
		{
			return view;
		}

		if (serviceProvider.GetRequiredService(viewModelToViewMappings[typeof(TBindingContext)]) is Popup<T> content)
		{
			return content;
		}

		throw new InvalidOperationException($"Could not locate a view for {typeof(TBindingContext).FullName}");
	}
}