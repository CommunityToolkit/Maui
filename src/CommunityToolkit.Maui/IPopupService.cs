using System.ComponentModel;

namespace CommunityToolkit.Maui;

/// <summary>
/// Provides a mechanism for displaying <see cref="CommunityToolkit.Maui.Views.Popup"/>s based on the underlying view model.
/// </summary>
public interface IPopupService
{
	/// <summary>
	/// Resolves and displays a <see cref="CommunityToolkit.Maui.Views.Popup"/> and <typeparamref name="TViewModel"/> pair that was registered with <c>AddTransientPopup</c>.
	/// </summary>
	/// <typeparam name="TViewModel">The type of the view model registered with the <see cref="CommunityToolkit.Maui.Views.Popup"/>.</typeparam>
	void ShowPopup<TViewModel>() where TViewModel : INotifyPropertyChanged;

	/// <summary>
	/// Resolves and displays a <see cref="CommunityToolkit.Maui.Views.Popup"/> and <typeparamref name="TViewModel"/> pair that was registered with <c>AddTransientPopup</c>.
	/// </summary>
	/// <typeparam name="TViewModel">The type of the view model registered with the <see cref="CommunityToolkit.Maui.Views.Popup"/>.</typeparam>
	/// <param name="query">Any parameters that can be passed across to the Popup or underlying view model.</param>
	void ShowPopup<TViewModel>(IDictionary<string, object> query) where TViewModel : IQueryAttributable;

	/// <summary>
	/// Resolves and displays a <see cref="CommunityToolkit.Maui.Views.Popup"/> and <typeparamref name="TViewModel"/> pair that was registered with <c>AddTransientPopup</c>.
	/// </summary>
	/// <typeparam name="TViewModel">The type of the view model registered with the <see cref="CommunityToolkit.Maui.Views.Popup"/>.</typeparam>
	/// <param name="viewModel">The view model to use as the <c>BindingContext</c> for the <see cref="CommunityToolkit.Maui.Views.Popup"/>.</param>
	void ShowPopup<TViewModel>(TViewModel viewModel) where TViewModel : INotifyPropertyChanged;

	/// <summary>
	/// Resolves and displays a <see cref="CommunityToolkit.Maui.Views.Popup"/> and <typeparamref name="TViewModel"/> pair that was registered with <c>AddTransientPopup</c>.
	/// </summary>
	/// <typeparam name="TViewModel">The type of the view model registered with the <see cref="CommunityToolkit.Maui.Views.Popup"/>.</typeparam>
	/// <returns>A <see cref="Task"/> that can be awaited to return the result of the <see cref="CommunityToolkit.Maui.Views.Popup"/> once it has been dismissed.</returns>
	Task<object?> ShowPopupAsync<TViewModel>() where TViewModel : INotifyPropertyChanged;

	/// <summary>
	/// Resolves and displays a <see cref="CommunityToolkit.Maui.Views.Popup"/> and <typeparamref name="TViewModel"/> pair that was registered with <c>AddTransientPopup</c>.
	/// </summary>
	/// <typeparam name="TViewModel">The type of the view model registered with the <see cref="CommunityToolkit.Maui.Views.Popup"/>.</typeparam>
	/// <returns>A <see cref="Task"/> that can be awaited to return the result of the <see cref="CommunityToolkit.Maui.Views.Popup"/> once it has been dismissed.</returns>
	/// <param name="query">Any parameters that can be passed across to the Popup or underlying view model.</param>
	Task<object?> ShowPopupAsync<TViewModel>(IDictionary<string, object> query) where TViewModel : IQueryAttributable;

	/// <summary>
	/// Resolves and displays a <see cref="CommunityToolkit.Maui.Views.Popup"/> and <typeparamref name="TViewModel"/> pair that was registered with <c>AddTransientPopup</c>.
	/// </summary>
	/// <typeparam name="TViewModel">The type of the view model registered with the <see cref="CommunityToolkit.Maui.Views.Popup"/>.</typeparam>
	/// <param name="viewModel">The view model to use as the <c>BindingContext</c> for the <see cref="CommunityToolkit.Maui.Views.Popup"/>.</param>
	/// <returns>A <see cref="Task"/> that can be awaited to return the result of the <see cref="CommunityToolkit.Maui.Views.Popup"/> once it has been dismissed.</returns>
	Task<object?> ShowPopupAsync<TViewModel>(TViewModel viewModel) where TViewModel : INotifyPropertyChanged;
}
