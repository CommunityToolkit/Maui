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
	/// </summary>
	/// <typeparam name="TViewModel">The type of the view model registered with the <see cref="CommunityToolkit.Maui.Core.IPopup"/>.</typeparam>
	/// <param name="viewModel">The view model to use as the <c>BindingContext</c> for the <see cref="CommunityToolkit.Maui.Core.IPopup"/>.</param>
	void ShowPopup<TViewModel>(TViewModel viewModel) where TViewModel : INotifyPropertyChanged;

	/// <summary>
	/// Resolves and displays a <see cref="CommunityToolkit.Maui.Core.IPopup"/> and <typeparamref name="TViewModel"/> pair that was registered with <c>AddTransientPopup</c>.
	/// </summary>
	/// <typeparam name="TViewModel">The type of the view model registered with the <see cref="CommunityToolkit.Maui.Core.IPopup"/>.</typeparam>
	/// <param name="token"><see cref="CancellationToken"/> </param>
	/// <returns>A <see cref="Task"/> that can be awaited to return the result of the <see cref="CommunityToolkit.Maui.Core.IPopup"/> once it has been dismissed.</returns>
	Task<object?> ShowPopupAsync<TViewModel>(CancellationToken token = default) where TViewModel : INotifyPropertyChanged;

	/// <summary>
	/// Resolves and displays a <see cref="CommunityToolkit.Maui.Core.IPopup"/> and <typeparamref name="TViewModel"/> pair that was registered with <c>AddTransientPopup</c>.
	/// The supplied <paramref name="onPresenting"/> provides a mechanism to invoke any methods on your view model in order to load or pass data to it.
	/// </summary>
	/// <typeparam name="TViewModel">The type of the view model registered with the <see cref="CommunityToolkit.Maui.Core.IPopup"/>.</typeparam>
	/// <returns>A <see cref="Task"/> that can be awaited to return the result of the <see cref="CommunityToolkit.Maui.Core.IPopup"/> once it has been dismissed.</returns>
	/// <param name="onPresenting">An <see cref="Action{TViewModel}"/> that will be performed before the popup is presented.</param>
	/// <param name="token"><see cref="CancellationToken"/> </param>
	Task<object?> ShowPopupAsync<TViewModel>(Action<TViewModel> onPresenting, CancellationToken token = default) where TViewModel : INotifyPropertyChanged;

	/// <summary>
	/// Resolves and displays a <see cref="CommunityToolkit.Maui.Core.IPopup"/> and <typeparamref name="TViewModel"/> pair that was registered with <c>AddTransientPopup</c>.
	/// </summary>
	/// <typeparam name="TViewModel">The type of the view model registered with the <see cref="CommunityToolkit.Maui.Core.IPopup"/>.</typeparam>
	/// <param name="viewModel">The view model to use as the <c>BindingContext</c> for the <see cref="CommunityToolkit.Maui.Core.IPopup"/>.</param>
	/// <param name="token"><see cref="CancellationToken"/> </param>
	/// <returns>A <see cref="Task"/> that can be awaited to return the result of the <see cref="CommunityToolkit.Maui.Core.IPopup"/> once it has been dismissed.</returns>
	Task<object?> ShowPopupAsync<TViewModel>(TViewModel viewModel, CancellationToken token = default) where TViewModel : INotifyPropertyChanged;
}