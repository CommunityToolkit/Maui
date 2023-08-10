using System.ComponentModel;

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Provides a mechanism for displaying <see cref="CommunityToolkit.Maui.Core.IPopup"/>s based on the underlying view model.
/// </summary>
public interface IPopupService
{
	/// <summary>
	/// Resolves and displays a <see cref="CommunityToolkit.Maui.Core.IPopup"/> and <typeparamref name="TViewModel"/> pair that was registered with <c>AddTransientPopup</c>.
	/// </summary>
	/// <typeparam name="TViewModel">The type of the view model registered with the <see cref="CommunityToolkit.Maui.Core.IPopup"/>.</typeparam>
	void ShowPopup<TViewModel>() where TViewModel : INotifyPropertyChanged;

	/// <summary>
	/// Resolves and displays a <see cref="CommunityToolkit.Maui.Core.IPopup"/> and <typeparamref name="TViewModel"/> pair that was registered with <c>AddTransientPopup</c>.
	/// The supplied <paramref name="onPresenting"/> provides a mechanism to invoke any methods on your view model in order to load or pass data to it.
	/// </summary>
	/// <typeparam name="TViewModel">The type of the view model registered with the <see cref="CommunityToolkit.Maui.Core.IPopup"/>.</typeparam>
	/// <param name="onPresenting">An <see cref="Action{TViewModel}"/> that will be performed before the popup is presented.</param>
	void ShowPopup<TViewModel>(Action<TViewModel> onPresenting) where TViewModel : INotifyPropertyChanged;

	/// <summary>
	/// Resolves and displays a <see cref="CommunityToolkit.Maui.Core.IPopup"/> and <typeparamref name="TViewModel"/> pair that was registered with <c>AddTransientPopup</c>.
	/// The supplied <paramref name="onPresenting"/> provides a mechanism to invoke any methods on your view model in order to load or pass data to it.
	/// </summary>
	/// <typeparam name="TViewModel">The type of the view model registered with the <see cref="CommunityToolkit.Maui.Core.IPopup"/>.</typeparam>
	/// <param name="onPresenting">An <see cref="Func{TViewModel, Task}"/> that will be performed before the popup is presented.
	/// The <see cref="Task"/> returned from the func will be awaited so it will delay the popup from showing until that operation has completed..</param>
	void ShowPopup<TViewModel>(Func<TViewModel, Task> onPresenting) where TViewModel : INotifyPropertyChanged;

	/// <summary>
	/// Resolves and displays a <see cref="CommunityToolkit.Maui.Core.IPopup"/> and <typeparamref name="TViewModel"/> pair that was registered with <c>AddTransientPopup</c>.
	/// </summary>
	/// <typeparam name="TViewModel">The type of the view model registered with the <see cref="CommunityToolkit.Maui.Core.IPopup"/>.</typeparam>
	/// <param name="viewModel">The view model to use as the <c>BindingContext</c> for the <see cref="CommunityToolkit.Maui.Core.IPopup"/>.</param>
	void ShowPopup<TViewModel>(TViewModel viewModel) where TViewModel : INotifyPropertyChanged;

	/// <summary>
	/// Resolves and displays a <see cref="CommunityToolkit.Maui.Core.IPopup"/> and <typeparamref name="TViewModel"/> pair that was registered with <c>AddTransientPopup</c>.
	/// </summary>
	/// <typeparam name="TViewModel">The type of the view model registered with the <see cref="CommunityToolkit.Maui.Core.IPopup"/>.</typeparam>
	/// <returns>A <see cref="Task"/> that can be awaited to return the result of the <see cref="CommunityToolkit.Maui.Core.IPopup"/> once it has been dismissed.</returns>
	Task<object?> ShowPopupAsync<TViewModel>() where TViewModel : INotifyPropertyChanged;

	/// <summary>
	/// Resolves and displays a <see cref="CommunityToolkit.Maui.Core.IPopup"/> and <typeparamref name="TViewModel"/> pair that was registered with <c>AddTransientPopup</c>.
	/// The supplied <paramref name="onPresenting"/> provides a mechanism to invoke any methods on your view model in order to load or pass data to it.
	/// </summary>
	/// <typeparam name="TViewModel">The type of the view model registered with the <see cref="CommunityToolkit.Maui.Core.IPopup"/>.</typeparam>
	/// <returns>A <see cref="Task"/> that can be awaited to return the result of the <see cref="CommunityToolkit.Maui.Core.IPopup"/> once it has been dismissed.</returns>
	/// <param name="onPresenting">An <see cref="Action{TViewModel}"/> that will be performed before the popup is presented.</param>
	Task<object?> ShowPopupAsync<TViewModel>(Action<TViewModel> onPresenting) where TViewModel : INotifyPropertyChanged;

	/// <summary>
	/// Resolves and displays a <see cref="CommunityToolkit.Maui.Core.IPopup"/> and <typeparamref name="TViewModel"/> pair that was registered with <c>AddTransientPopup</c>.
	/// The supplied <paramref name="onPresenting"/> provides a mechanism to invoke any methods on your view model in order to load or pass data to it.
	/// </summary>
	/// <typeparam name="TViewModel">The type of the view model registered with the <see cref="CommunityToolkit.Maui.Core.IPopup"/>.</typeparam>
	/// <returns>A <see cref="Task"/> that can be awaited to return the result of the <see cref="CommunityToolkit.Maui.Core.IPopup"/> once it has been dismissed.</returns>
	/// <param name="onPresenting">An <see cref="Func{TViewModel, Task}"/> that will be performed before the popup is presented.
	/// The <see cref="Task"/> returned from the func will be awaited so it will delay the popup from showing until that operation has completed..</param>
	Task<object?> ShowPopupAsync<TViewModel>(Func<TViewModel, Task> onPresenting) where TViewModel : INotifyPropertyChanged;

	/// <summary>
	/// Resolves and displays a <see cref="CommunityToolkit.Maui.Core.IPopup"/> and <typeparamref name="TViewModel"/> pair that was registered with <c>AddTransientPopup</c>.
	/// </summary>
	/// <typeparam name="TViewModel">The type of the view model registered with the <see cref="CommunityToolkit.Maui.Core.IPopup"/>.</typeparam>
	/// <param name="viewModel">The view model to use as the <c>BindingContext</c> for the <see cref="CommunityToolkit.Maui.Core.IPopup"/>.</param>
	/// <returns>A <see cref="Task"/> that can be awaited to return the result of the <see cref="CommunityToolkit.Maui.Core.IPopup"/> once it has been dismissed.</returns>
	Task<object?> ShowPopupAsync<TViewModel>(TViewModel viewModel) where TViewModel : INotifyPropertyChanged;
}
