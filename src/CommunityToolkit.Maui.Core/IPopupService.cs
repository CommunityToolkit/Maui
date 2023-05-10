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
	/// The view model will be passed the supplied <paramref name="arguments"/> based on its <see cref="IArgumentsReceiver"/> implementation.
	/// </summary>
	/// <typeparam name="TViewModel">The type of the view model registered with the <see cref="CommunityToolkit.Maui.Core.IPopup"/>.</typeparam>
	/// <param name="arguments">Any parameters that can be passed across to the resolved view model.</param>
	void ShowPopup<TViewModel>(IReadOnlyDictionary<string, object> arguments) where TViewModel : IArgumentsReceiver, INotifyPropertyChanged;

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
	/// The view model will be passed the supplied <paramref name="arguments"/> based on its <see cref="IArgumentsReceiver"/> implementation.
	/// </summary>
	/// <typeparam name="TViewModel">The type of the view model registered with the <see cref="CommunityToolkit.Maui.Core.IPopup"/>.</typeparam>
	/// <returns>A <see cref="Task"/> that can be awaited to return the result of the <see cref="CommunityToolkit.Maui.Core.IPopup"/> once it has been dismissed.</returns>
	/// <param name="arguments">Any parameters that can be passed across to the resolved view model.</param>
	Task<object?> ShowPopupAsync<TViewModel>(IReadOnlyDictionary<string, object> arguments) where TViewModel : IArgumentsReceiver, INotifyPropertyChanged;

	/// <summary>
	/// Resolves and displays a <see cref="CommunityToolkit.Maui.Core.IPopup"/> and <typeparamref name="TViewModel"/> pair that was registered with <c>AddTransientPopup</c>.
	/// </summary>
	/// <typeparam name="TViewModel">The type of the view model registered with the <see cref="CommunityToolkit.Maui.Core.IPopup"/>.</typeparam>
	/// <param name="viewModel">The view model to use as the <c>BindingContext</c> for the <see cref="CommunityToolkit.Maui.Core.IPopup"/>.</param>
	/// <returns>A <see cref="Task"/> that can be awaited to return the result of the <see cref="CommunityToolkit.Maui.Core.IPopup"/> once it has been dismissed.</returns>
	Task<object?> ShowPopupAsync<TViewModel>(TViewModel viewModel) where TViewModel : INotifyPropertyChanged;
}
