using System.ComponentModel;

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Provides a mechanism for displaying <see cref="CommunityToolkit.Maui.Core.IPopup"/>s based on the underlying view model.
/// </summary>
public interface IPopupService
{
	/// <summary>
	/// Close the currently displayed Popup.
	/// </summary>
	/// <remarks>
	/// This method must be called on the UI thread otherwise it will throw an <see cref="InvalidOperationException"/>.
	/// </remarks>
	/// <param name="result">An optional result to be returned from the popup.</param>
	/// <exception cref="InvalidOperationException">Thrown if this method is not called on the UI thread.</exception>
	void ClosePopup(object? result = null);

	/// <summary>
	/// Close the currently displayed Popup.
	/// </summary>
	/// <remarks>
	/// This method must be called on the UI thread otherwise it will throw an <see cref="InvalidOperationException"/>.
	/// </remarks>
	/// <param name="result">An optional result to be returned from the popup.</param>
	/// <returns>A <see cref="Task"/> that can be awaited.</returns>
	/// <exception cref="InvalidOperationException">Thrown if this method is not called on the UI thread.</exception>
	Task ClosePopupAsync(object? result = null);

	/// <summary>
	/// Resolves and displays a <see cref="CommunityToolkit.Maui.Core.IPopup"/> and <typeparamref name="TViewModel"/> pair that was registered with <c>AddTransientPopup</c>.
	/// </summary>
	/// <remarks>
	/// This method must be called on the UI thread otherwise it will throw an <see cref="InvalidOperationException"/>.
	/// </remarks>
	/// <typeparam name="TViewModel">The type of the view model registered with the <see cref="CommunityToolkit.Maui.Core.IPopup"/>.</typeparam>
	/// <exception cref="InvalidOperationException">Thrown if this method is not called on the UI thread.</exception>
	void ShowPopup<TViewModel>() where TViewModel : INotifyPropertyChanged;

	/// <summary>
	/// Resolves and displays a <see cref="CommunityToolkit.Maui.Core.IPopup"/> and <typeparamref name="TViewModel"/> pair that was registered with <c>AddTransientPopup</c>.
	/// The supplied <paramref name="onPresenting"/> provides a mechanism to invoke any methods on your view model in order to load or pass data to it.
	/// </summary>
	/// <remarks>
	/// This method must be called on the UI thread otherwise it will throw an <see cref="InvalidOperationException"/>.
	/// </remarks>
	/// <typeparam name="TViewModel">The type of the view model registered with the <see cref="CommunityToolkit.Maui.Core.IPopup"/>.</typeparam>
	/// <param name="onPresenting">An <see cref="Action{TViewModel}"/> that will be performed before the popup is presented.</param>
	/// <exception cref="InvalidOperationException">Thrown if this method is not called on the UI thread.</exception>
	void ShowPopup<TViewModel>(Action<TViewModel> onPresenting) where TViewModel : INotifyPropertyChanged;

	/// <summary>
	/// Resolves and displays a <see cref="CommunityToolkit.Maui.Core.IPopup"/> and <typeparamref name="TViewModel"/> pair that was registered with <c>AddTransientPopup</c>.
	/// </summary>
	/// <remarks>
	/// This method must be called on the UI thread otherwise it will throw an <see cref="InvalidOperationException"/>.
	/// </remarks>
	/// <typeparam name="TViewModel">The type of the view model registered with the <see cref="CommunityToolkit.Maui.Core.IPopup"/>.</typeparam>
	/// <param name="token"><see cref="CancellationToken"/> </param>
	/// <returns>A <see cref="Task"/> that can be awaited to return the result of the <see cref="CommunityToolkit.Maui.Core.IPopup"/> once it has been dismissed.</returns>
	/// <exception cref="InvalidOperationException">Thrown if this method is not called on the UI thread.</exception>
	Task<object?> ShowPopupAsync<TViewModel>(CancellationToken token = default) where TViewModel : INotifyPropertyChanged;

	/// <summary>
	/// Resolves and displays a <see cref="CommunityToolkit.Maui.Core.IPopup"/> and <typeparamref name="TViewModel"/> pair that was registered with <c>AddTransientPopup</c>.
	/// The supplied <paramref name="onPresenting"/> provides a mechanism to invoke any methods on your view model in order to load or pass data to it.
	/// </summary>
	/// <remarks>
	/// This method must be called on the UI thread otherwise it will throw an <see cref="InvalidOperationException"/>.
	/// </remarks>
	/// <typeparam name="TViewModel">The type of the view model registered with the <see cref="CommunityToolkit.Maui.Core.IPopup"/>.</typeparam>
	/// <returns>A <see cref="Task"/> that can be awaited to return the result of the <see cref="CommunityToolkit.Maui.Core.IPopup"/> once it has been dismissed.</returns>
	/// <param name="onPresenting">An <see cref="Action{TViewModel}"/> that will be performed before the popup is presented.</param>
	/// <param name="token"><see cref="CancellationToken"/> </param>
	/// <exception cref="InvalidOperationException">Thrown if this method is not called on the UI thread.</exception>
	Task<object?> ShowPopupAsync<TViewModel>(Action<TViewModel> onPresenting, CancellationToken token = default) where TViewModel : INotifyPropertyChanged;
}