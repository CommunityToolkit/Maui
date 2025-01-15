using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using CommunityToolkit.Maui.Core;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Maui.Controls.Shapes;

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
		viewModelToViewMappings.TryAdd(typeof(TPopupView), typeof(TPopupView));
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
		await taskCompletionSource.Task
			.WaitAsync(cancellationToken)
			.ContinueWith((_) => taskCompletionSource.SetResult(new PopupResult(false)), TaskContinuationOptions.OnlyOnCanceled);
		return await taskCompletionSource.Task;
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
		await taskCompletionSource.Task
			.WaitAsync(cancellationToken)
			.ContinueWith((_) => taskCompletionSource.SetResult(new PopupResult<T>(default, false)), TaskContinuationOptions.OnlyOnCanceled);
		return await taskCompletionSource.Task;
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
		var popup = GetPopupContent(bindingContext);

		var view = new Grid()
		{
			BackgroundColor = null
		};
		view.Children.Add(new Border()
		{
			Content = popup,
			Background = popup.Background,
			WidthRequest = popup.WidthRequest,
			HeightRequest = popup.HeightRequest,
			BackgroundColor = popup.BackgroundColor,
			VerticalOptions = LayoutOptions.Center,
			StrokeShape = new RoundRectangle() { CornerRadius = new CornerRadius(15) },
			Margin = 30,
			Padding = 15
		});
		var popupContainer = new PopupContainer(bindingContext as Popup ?? new Popup() { Content = popup }, taskCompletionSource)
		{
			BackgroundColor = options.BackgroundColor ?? Color.FromRgba(0, 0, 0, 0.4), // https://rgbacolorpicker.com/rgba-to-hex,
			CanBeDismissedByTappingOutsideOfPopup = options.CanBeDismissedByTappingOutsideOfPopup,
			Content = view,
			BindingContext = bindingContext
		};

		popupContainer.Appearing += (s, e) => options.OnOpened?.Invoke(bindingContext);
		popupContainer.Disappearing += (s, e) => options.OnClosed?.Invoke(bindingContext);

		view.BindingContext = bindingContext;

		if (options.CanBeDismissedByTappingOutsideOfPopup)
		{
			view.GestureRecognizers.Add(new TapGestureRecognizer()
			{
				Command = new Command(async () =>
				{
					options.OnTappingOutsideOfPopup?.Invoke();
					await popupContainer.Close(new PopupResult(true));
				})
			});
		}

		return popupContainer;
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

	PopupContainer<T> GetPopup<TBindingContext, T>(PopupOptions<TBindingContext> options, TBindingContext bindingContext, TaskCompletionSource<PopupResult<T>> taskCompletionSource)
	{
		var popup = GetPopupContent(bindingContext);

		var view = new Grid()
		{
			BackgroundColor = null
		};
		view.Children.Add(new Border()
		{
			Content = popup,
			Background = popup.Background,
			WidthRequest = popup.WidthRequest,
			HeightRequest = popup.HeightRequest,
			BackgroundColor = popup.BackgroundColor,
			VerticalOptions = LayoutOptions.Center,
			HorizontalOptions = LayoutOptions.Center,
			StrokeShape = new RoundRectangle() { CornerRadius = new CornerRadius(15) },
			Margin = 30,
			Padding = 15
		});
		var popupContainer = new PopupContainer<T>(bindingContext as Popup<T> ?? new Popup<T>() { Content = popup }, taskCompletionSource)
		{
			BackgroundColor = options.BackgroundColor ?? Color.FromRgba(0, 0, 0, 0.4), // https://rgbacolorpicker.com/rgba-to-hex,
			CanBeDismissedByTappingOutsideOfPopup = options.CanBeDismissedByTappingOutsideOfPopup,
			Content = view,
			BindingContext = bindingContext
		};

		popupContainer.Appearing += (s, e) => options.OnOpened?.Invoke(bindingContext);
		popupContainer.Disappearing += (s, e) => options.OnClosed?.Invoke(bindingContext);

		view.BindingContext = bindingContext;

		if (options.CanBeDismissedByTappingOutsideOfPopup)
		{
			view.GestureRecognizers.Add(new TapGestureRecognizer()
			{
				Command = new Command(async () =>
				{
					options.OnTappingOutsideOfPopup?.Invoke();
					await popupContainer.Close(new PopupResult(true));
				})
			});
		}

		return popupContainer;
	}
}