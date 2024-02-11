using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls.Platform;

namespace CommunityToolkit.Maui;

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
		serviceProvider = Application.Current?.Handler?.MauiContext?.Services
							?? throw new InvalidOperationException("Could not locate IServiceProvider");
	}

	internal static void AddTransientPopup<TPopupView, TPopupViewModel>(IServiceCollection services)
		where TPopupView : IPopup
		where TPopupViewModel : INotifyPropertyChanged
	{
		viewModelToViewMappings.Add(typeof(TPopupViewModel), typeof(TPopupView));

		services.AddTransient(typeof(TPopupView));
		services.AddTransient(typeof(TPopupViewModel));
	}

<<<<<<< HEAD
	internal static void AddTransientPopup<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TPopupView, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TPopupViewModel>(TPopupView popup, TPopupViewModel popupViewModel, IServiceCollection services)
		where TPopupView : class, IPopup
		where TPopupViewModel : class, INotifyPropertyChanged
	{
		viewModelToViewMappings.Add(typeof(TPopupViewModel), typeof(TPopupView));

		services.AddTransient<TPopupView>(_ => popup);
		services.AddTransient<TPopupViewModel>(_ => popupViewModel);
	}

	/// <inheritdoc cref="IPopupService.ClosePopup(object?)" />
	public void ClosePopup(object? result = null)
	{
		EnsureMainThreadIsUsed();

		PopupLifecycleController.GetCurrentPopup()?.Close(result);
	}

	/// <inheritdoc cref="IPopupService.ClosePopupAsync(object?)" />
	public Task ClosePopupAsync(object? result = null)
	{
		EnsureMainThreadIsUsed();

		var popup = PopupLifecycleController.GetCurrentPopup();

		return popup?.CloseAsync(result) ?? Task.CompletedTask;
	}

	/// <inheritdoc cref="IPopupService.ShowPopup{TViewModel}()"/>
	public void ShowPopup<TViewModel>() where TViewModel : INotifyPropertyChanged
	{
		EnsureMainThreadIsUsed();

		var popup = GetPopup(typeof(TViewModel));

		ValidateBindingContext<TViewModel>(popup, out _);

		InitializePopup(popup);

		ShowPopup(popup);
	}

	/// <inheritdoc cref="IPopupService.ShowPopup{TViewModel}(Action{TViewModel})"/>
	public void ShowPopup<TViewModel>(Action<TViewModel> onPresenting) where TViewModel : INotifyPropertyChanged
=======
	/// <inheritdoc cref="IPopupService.ShowPopup{TViewModel}(bool, object?, Microsoft.Maui.Primitives.LayoutAlignment, Microsoft.Maui.Primitives.LayoutAlignment, Size, Color?)"/>
	public void ShowPopup<TViewModel>(
		bool canBeDismissedByTappingOutsideOfPopup = true,
		object? resultWhenUserTapsOutsideOfPopup = default,
		Microsoft.Maui.Primitives.LayoutAlignment verticalOptions = Microsoft.Maui.Primitives.LayoutAlignment.Center,
		Microsoft.Maui.Primitives.LayoutAlignment horizontalOptions = Microsoft.Maui.Primitives.LayoutAlignment.Center,
		Size size = default,
		Color? color = default
	) where TViewModel : INotifyPropertyChanged
	{
		var popup = GetPopup(typeof(TViewModel));

		ApplyPopupProperties(
			popup,
			canBeDismissedByTappingOutsideOfPopup,
			resultWhenUserTapsOutsideOfPopup,
			verticalOptions,
			horizontalOptions,
			size,
			color);

		ValidateBindingContext<TViewModel>(popup, out _);

		CurrentPage.ShowPopup(popup);
	}

	/// <inheritdoc cref="IPopupService.ShowPopup{TViewModel}(Action{TViewModel}, bool, object?, Microsoft.Maui.Primitives.LayoutAlignment, Microsoft.Maui.Primitives.LayoutAlignment, Size, Color?)"/>
	public void ShowPopup<TViewModel>(
		Action<TViewModel> onPresenting,
		bool canBeDismissedByTappingOutsideOfPopup = true,
		object? resultWhenUserTapsOutsideOfPopup = default,
		Microsoft.Maui.Primitives.LayoutAlignment verticalOptions = Microsoft.Maui.Primitives.LayoutAlignment.Center,
		Microsoft.Maui.Primitives.LayoutAlignment horizontalOptions = Microsoft.Maui.Primitives.LayoutAlignment.Center,
		Size size = default,
		Color? color = default) where TViewModel : INotifyPropertyChanged
>>>>>>> f7fb5810 (Expand IPopupService API to enable the ability to supply the configurable properties)
	{
		EnsureMainThreadIsUsed();

		ArgumentNullException.ThrowIfNull(onPresenting);

		var popup = GetPopup(typeof(TViewModel));

		ApplyPopupProperties(
			popup,
			canBeDismissedByTappingOutsideOfPopup,
			resultWhenUserTapsOutsideOfPopup,
			verticalOptions,
			horizontalOptions,
			size,
			color);

		ValidateBindingContext(popup, out TViewModel viewModel);

		onPresenting.Invoke(viewModel);

		InitializePopup(popup);

		ShowPopup(popup);
	}

	/// <inheritdoc cref="IPopupService.ShowPopupAsync{TViewModel}(bool, object?, Microsoft.Maui.Primitives.LayoutAlignment, Microsoft.Maui.Primitives.LayoutAlignment, Size, Color?, CancellationToken)"/>
	public Task<object?> ShowPopupAsync<TViewModel>(
		bool canBeDismissedByTappingOutsideOfPopup = true,
		object? resultWhenUserTapsOutsideOfPopup = default,
		Microsoft.Maui.Primitives.LayoutAlignment verticalOptions = Microsoft.Maui.Primitives.LayoutAlignment.Center,
		Microsoft.Maui.Primitives.LayoutAlignment horizontalOptions = Microsoft.Maui.Primitives.LayoutAlignment.Center,
		Size size = default,
		Color? color = default,
		CancellationToken token = default) where TViewModel : INotifyPropertyChanged
	{
<<<<<<< HEAD
		EnsureMainThreadIsUsed();

		var popup = GetPopup(typeof(TViewModel));

		ValidateBindingContext<TViewModel>(popup, out _);

		InitializePopup(popup);

		return ShowPopupAsync(popup, token);
	}

	/// <inheritdoc cref="IPopupService.ShowPopupAsync{TViewModel}(Action{TViewModel}, CancellationToken)"/>
	public Task<object?> ShowPopupAsync<TViewModel>(Action<TViewModel> onPresenting, CancellationToken token = default) where TViewModel : INotifyPropertyChanged
=======
		var popup = GetPopup(typeof(TViewModel));

		ApplyPopupProperties(
			popup,
			canBeDismissedByTappingOutsideOfPopup,
			resultWhenUserTapsOutsideOfPopup,
			verticalOptions,
			horizontalOptions,
			size,
			color);

		ValidateBindingContext<TViewModel>(popup, out _);

		return CurrentPage.ShowPopupAsync(popup, token);
	}

	/// <inheritdoc cref="IPopupService.ShowPopupAsync{TViewModel}(Action{TViewModel}, bool, object?, Microsoft.Maui.Primitives.LayoutAlignment, Microsoft.Maui.Primitives.LayoutAlignment, Size, Color?, CancellationToken)"/>
	public Task<object?> ShowPopupAsync<TViewModel>(
		Action<TViewModel> onPresenting,
		bool canBeDismissedByTappingOutsideOfPopup = true,
		object? resultWhenUserTapsOutsideOfPopup = default,
		Microsoft.Maui.Primitives.LayoutAlignment verticalOptions = Microsoft.Maui.Primitives.LayoutAlignment.Center,
		Microsoft.Maui.Primitives.LayoutAlignment horizontalOptions = Microsoft.Maui.Primitives.LayoutAlignment.Center,
		Size size = default,
		Color? color = default,
		CancellationToken token = default) where TViewModel : INotifyPropertyChanged
>>>>>>> f7fb5810 (Expand IPopupService API to enable the ability to supply the configurable properties)
	{
		EnsureMainThreadIsUsed();

		ArgumentNullException.ThrowIfNull(onPresenting);

		var popup = GetPopup(typeof(TViewModel));

		ApplyPopupProperties(
			popup,
			canBeDismissedByTappingOutsideOfPopup,
			resultWhenUserTapsOutsideOfPopup,
			verticalOptions,
			horizontalOptions,
			size,
			color);

		ValidateBindingContext(popup, out TViewModel viewModel);

		onPresenting.Invoke(viewModel);

		InitializePopup(popup);

		return ShowPopupAsync(popup, token);
	}

	static Task<object?> ShowPopupAsync(Popup popup, CancellationToken token)
	{
#if WINDOWS
		if (Application.Current is Application app)
		{
			if (app.Windows.FirstOrDefault(x => x.IsActivated) is Window activeWindow)
			{
				if (activeWindow.Page is Page page)
				{
					return page.ShowPopupAsync(popup, token);
				}
			}
		}
		return CurrentPage.ShowPopupAsync(popup, token);
#else
		return CurrentPage.ShowPopupAsync(popup, token);
#endif
	}

	/// <summary>
	/// Ensures that the BindingContext property of the Popup to present is properly assigned and of the expected type.
	/// </summary>
	/// <typeparam name="TViewModel"></typeparam>
	/// <param name="popup">The popup to be presented.</param>
	/// <param name="bindingContext">Validated View Model</param>
	/// <exception cref="InvalidOperationException"></exception>
	static void ValidateBindingContext<TViewModel>(Popup popup, out TViewModel bindingContext)
	{
		if (popup.BindingContext is TViewModel viewModel)
		{
			bindingContext = viewModel;
			return;			
		}
		else if (popup.Content?.BindingContext is TViewModel contentViewModel)
		{
			bindingContext = contentViewModel;
			return;
		}

		throw new InvalidOperationException($"Unexpected type has been assigned to the BindingContext of {popup.GetType().FullName}. Expected type {typeof(TViewModel).FullName} but was {popup.BindingContext?.GetType().FullName ?? "null"}");
	}

<<<<<<< HEAD
	static void ShowPopup(Popup popup)
	{
#if WINDOWS
		if (Application.Current is Application app)
		{
			if (app.Windows.FirstOrDefault(x => x.IsActivated) is Window activeWindow)
			{
				if (activeWindow.Page is Page page)
				{
					page.ShowPopup(popup);
					return;
				}
			}
		}
		CurrentPage.ShowPopup(popup);
#else
		CurrentPage.ShowPopup(popup);
#endif
	}

	void EnsureMainThreadIsUsed([CallerMemberName] string? callerName = default)
	{
		if (dispatcher.IsDispatchRequired)
		{
			throw new InvalidOperationException($"{callerName} must be called from the main thread.");
=======
	static void ApplyPopupProperties(
		Popup popup,
		bool canBeDismissedByTappingOutsideOfPopup = true,
		object? resultWhenUserTapsOutsideOfPopup = default,
		Microsoft.Maui.Primitives.LayoutAlignment verticalOptions = Microsoft.Maui.Primitives.LayoutAlignment.Center,
		Microsoft.Maui.Primitives.LayoutAlignment horizontalOptions = Microsoft.Maui.Primitives.LayoutAlignment.Center,
		Size size = default,
		Color? color = default)
	{
		popup.CanBeDismissedByTappingOutsideOfPopup = canBeDismissedByTappingOutsideOfPopup;
		popup.ResultWhenUserTapsOutsideOfPopup = resultWhenUserTapsOutsideOfPopup;
		popup.VerticalOptions = verticalOptions;
		popup.HorizontalOptions = horizontalOptions;
		popup.Size = size;

		if (color is not null)
		{
			popup.Color = color;
>>>>>>> f7fb5810 (Expand IPopupService API to enable the ability to supply the configurable properties)
		}
	}

	Popup GetPopup(Type viewModelType)
	{
		var popup = serviceProvider.GetService(viewModelToViewMappings[viewModelType]) as Popup;

		if (popup is null)
		{
			throw new InvalidOperationException(
				$"Unable to resolve popup type for {viewModelType} please make sure that you have called {nameof(AddTransientPopup)}");
		}

<<<<<<< HEAD
=======
		Popup popup;

		if (view is View visualElement)
		{
			popup = new Popup
			{
				Content = visualElement
			};

			// Binding the BindingContext property up from the view to the popup so that it is nicely handled on macOS.
			popup.SetBinding(Popup.BindingContextProperty, new Binding { Source = view, Path = Popup.BindingContextProperty.PropertyName });
		}
		else if (view is Popup viewPopup)
		{
			popup = viewPopup;
		}
		else
		{
			throw new InvalidOperationException(
				$"Invalid type of view being used to present a Popup. Expected either IPopup or View.");
		}

>>>>>>> fdd77156 (Remove commented code)
		return popup;
	}

	void InitializePopup(Popup popup)
	{
		PopupLifecycleController.OnShowPopup(popup);
	}
}