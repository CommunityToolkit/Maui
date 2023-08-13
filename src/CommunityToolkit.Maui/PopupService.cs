using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls.Platform;
using System.ComponentModel;

namespace CommunityToolkit.Maui;

/// <inheritdoc cref="IPopupService"/>
public class PopupService : IPopupService
{
	readonly IServiceProvider serviceProvider;

	static readonly Dictionary<Type, Type> viewModelToViewMappings = new Dictionary<Type, Type>();

	static Page CurrentPage =>
		PageExtensions.GetCurrentPage(
			Application.Current?.MainPage ?? throw new NullReferenceException("MainPage is null."));

	/// <summary>
	/// Creates a new instance of <see cref="PopupService"/>.
	/// </summary>
	/// <param name="serviceProvider">The <see cref="IServiceProvider"/> implementation.</param>
	public PopupService(IServiceProvider serviceProvider)
	{
		this.serviceProvider = serviceProvider;
	}

	internal static void AddTransientPopup<TPopupView, TPopupViewModel>(IServiceCollection services)
		where TPopupView : Popup
		where TPopupViewModel : INotifyPropertyChanged
	{
		viewModelToViewMappings.Add(typeof(TPopupViewModel), typeof(TPopupView));

		services.AddTransient(typeof(TPopupView));
		services.AddTransient(typeof(TPopupViewModel));
	}

	/// <inheritdoc cref="IPopupService.ShowPopup{TViewModel}()"/>
	public void ShowPopup<TViewModel>() where TViewModel : INotifyPropertyChanged =>
		ShowPopup(GetViewModel<TViewModel>());

	/// <inheritdoc cref="IPopupService.ShowPopup{TViewModel}(TViewModel)"/>
	public void ShowPopup<TViewModel>(TViewModel viewModel) where TViewModel : INotifyPropertyChanged
	{
		ArgumentNullException.ThrowIfNull(viewModel);

		var popup = GetPopup(typeof(TViewModel));

		CurrentPage.ShowPopup(popup);
	}

	/// <inheritdoc cref="IPopupService.ShowPopup{TViewModel}(Action{TViewModel})"/>
	public void ShowPopup<TViewModel>(Action<TViewModel> onPresenting) where TViewModel : INotifyPropertyChanged
	{
		ArgumentNullException.ThrowIfNull(onPresenting);

		var popup = GetPopup(typeof(TViewModel));

		var viewModel = AssignBindingContext<TViewModel>(popup);

		onPresenting.Invoke(viewModel);

		CurrentPage.ShowPopup(popup);
	}

	/// <inheritdoc cref="IPopupService.ShowPopup{TViewModel}(Func{TViewModel, Task})"/>
	public async void ShowPopup<TViewModel>(Func<TViewModel, Task> onPresenting) where TViewModel : INotifyPropertyChanged
	{
		ArgumentNullException.ThrowIfNull(onPresenting);

		var popup = GetPopup(typeof(TViewModel));

		var viewModel = AssignBindingContext<TViewModel>(popup);

		await onPresenting.Invoke(viewModel);

		CurrentPage.ShowPopup(popup);
	}

	/// <inheritdoc cref="IPopupService.ShowPopupAsync{TViewModel}()"/>
	public Task<object?> ShowPopupAsync<TViewModel>() where TViewModel : INotifyPropertyChanged =>
		ShowPopupAsync(GetViewModel<TViewModel>());

	/// <inheritdoc cref="IPopupService.ShowPopupAsync{TViewModel}(TViewModel)"/>
	public Task<object?> ShowPopupAsync<TViewModel>(TViewModel viewModel) where TViewModel : INotifyPropertyChanged
	{
		ArgumentNullException.ThrowIfNull(viewModel);

		var popup = GetPopup(typeof(TViewModel));

		return CurrentPage.ShowPopupAsync(popup);
	}

	/// <inheritdoc cref="IPopupService.ShowPopupAsync{TViewModel}(Action{TViewModel})"/>
	public Task<object?> ShowPopupAsync<TViewModel>(Action<TViewModel> onPresenting) where TViewModel : INotifyPropertyChanged
	{
		ArgumentNullException.ThrowIfNull(onPresenting);

		var popup = GetPopup(typeof(TViewModel));

		var viewModel = AssignBindingContext<TViewModel>(popup);

		onPresenting.Invoke(viewModel);

		return CurrentPage.ShowPopupAsync(popup);
	}

	/// <inheritdoc cref="IPopupService.ShowPopupAsync{TViewModel}(Func{TViewModel, Task})"/>
	public async Task<object?> ShowPopupAsync<TViewModel>(Func<TViewModel, Task> onPresenting) where TViewModel : INotifyPropertyChanged
	{
		ArgumentNullException.ThrowIfNull(onPresenting);

		var popup = GetPopup(typeof(TViewModel));

		var viewModel = AssignBindingContext<TViewModel>(popup);

		await onPresenting.Invoke(viewModel);

		return CurrentPage.ShowPopupAsync(popup);
	}

	/// <summary>
	/// Ensures that the BindingContext property of the Popup to present is either not set (in which case we will assign an instance through the magic of DI),
	/// or is of the expected type.
	/// </summary>
	/// <typeparam name="TViewModel"></typeparam>
	/// <param name="popup">The popup to be presented.</param>
	/// <exception cref="InvalidOperationException"></exception>
	TViewModel AssignBindingContext<TViewModel>(Popup popup)
	{
		if (popup.BindingContext is null)
		{
			var viewModel = GetViewModel<TViewModel>();
			
			popup.BindingContext = viewModel;
		}

		if (popup.BindingContext is not TViewModel assignedViewModel)
		{
			throw new InvalidOperationException($"Unexpected type has been assigned to the BindingContext of {popup.GetType()}. Expected type {typeof(TViewModel)} but was {popup.BindingContext?.GetType()}");
		}

		return assignedViewModel;
	}

	Popup GetPopup(Type viewModelType)
	{
		var popup = this.serviceProvider.GetService(viewModelToViewMappings[viewModelType]) as Popup;

		if (popup is null)
		{
			throw new InvalidOperationException(
				$"Unable to resolve popup type for {viewModelType} please make sure that you have called {nameof(AddTransientPopup)}");
		}

		return popup;
	}

	TViewModel GetViewModel<TViewModel>()
	{
		var viewModel = this.serviceProvider.GetService<TViewModel>();

		if (viewModel is null)
		{
			throw new InvalidOperationException(
				$"Unable to resolve type {typeof(TViewModel)} please make sure that you have called {nameof(AddTransientPopup)}");
		}

		return viewModel;
	}
}
