using System.ComponentModel;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls.Platform;

namespace CommunityToolkit.Maui;

/// <inheritdoc cref="IPopupService"/>
public class PopupService : IPopupService
{
	readonly IServiceProvider serviceProvider;

	static readonly Dictionary<Type, Type> viewModelToViewMappings = [];

	static Page CurrentPage =>
		PageExtensions.GetCurrentPage(
			Application.Current?.MainPage ?? throw new InvalidOperationException("Application.Current.MainPage cannot be null."));

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
		serviceProvider = Application.Current?.Handler?.MauiContext?.Services
							?? throw new InvalidOperationException("Could not locate IServiceProvider");
	}

	internal static void AddTransientPopupContent<TPopupContentView, TPopupViewModel>(IServiceCollection services)
		where TPopupContentView : View
		where TPopupViewModel : INotifyPropertyChanged
	{
		viewModelToViewMappings.Add(typeof(TPopupViewModel), typeof(TPopupContentView));

		services.AddTransient(typeof(TPopupContentView));
		services.AddTransient(typeof(TPopupViewModel));
	}

	internal static void AddTransientPopup<TPopupView, TPopupViewModel>(IServiceCollection services)
		where TPopupView : IPopup
		where TPopupViewModel : INotifyPropertyChanged
	{
		viewModelToViewMappings.Add(typeof(TPopupViewModel), typeof(TPopupView));

		services.AddTransient(typeof(TPopupView));
		services.AddTransient(typeof(TPopupViewModel));
	}

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

		ShowPopup(popup);
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
	{
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

		ShowPopup(popup);
	}

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

		return ShowPopupAsync(popup, token);
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
	{
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
		}
	}

	Popup GetPopup(Type viewModelType)
	{
		var view = serviceProvider.GetService(viewModelToViewMappings[viewModelType]);

		if (view is null)
		{
			throw new InvalidOperationException(
				$"Unable to resolve popup type for {viewModelType} please make sure that you have called {nameof(AddTransientPopup)}");
		}

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

		return popup;
	}
}