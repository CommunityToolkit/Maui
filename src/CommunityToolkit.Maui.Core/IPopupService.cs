using System.ComponentModel;
using Microsoft.Maui.Primitives;

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
	/// <param name="canBeDismissedByTappingOutsideOfPopup">A <see cref="bool"/> indicating whether the <see cref="CommunityToolkit.Maui.Core.IPopup"/> can be dismissed by tapping outside of it.</param>
	/// <param name="resultWhenUserTapsOutsideOfPopup">An <see cref="object"/> that will be returned when the user taps outside of the <see cref="CommunityToolkit.Maui.Core.IPopup"/>.</param>
	/// <param name="verticalOptions">The vertical alignment of the <see cref="CommunityToolkit.Maui.Core.IPopup"/>.</param>
	/// <param name="horizontalOptions">The horizontal alignment of the <see cref="CommunityToolkit.Maui.Core.IPopup"/>.</param>
	/// <param name="size">The size of the <see cref="CommunityToolkit.Maui.Core.IPopup"/>.</param>
	/// <param name="color">The background color of the <see cref="CommunityToolkit.Maui.Core.IPopup"/>.</param>
	void ShowPopup<TViewModel>(
		bool canBeDismissedByTappingOutsideOfPopup = true,
		object? resultWhenUserTapsOutsideOfPopup = default,
		LayoutAlignment verticalOptions = LayoutAlignment.Center,
		LayoutAlignment horizontalOptions = LayoutAlignment.Center,
		Size size = default,
		Color? color = default) where TViewModel : INotifyPropertyChanged;

	/// <summary>
	/// Resolves and displays a <see cref="CommunityToolkit.Maui.Core.IPopup"/> and <typeparamref name="TViewModel"/> pair that was registered with <c>AddTransientPopup</c>.
	/// The supplied <paramref name="onPresenting"/> provides a mechanism to invoke any methods on your view model in order to load or pass data to it.
	/// </summary>
	/// <typeparam name="TViewModel">The type of the view model registered with the <see cref="CommunityToolkit.Maui.Core.IPopup"/>.</typeparam>
	/// <param name="onPresenting">An <see cref="Action{TViewModel}"/> that will be performed before the popup is presented.</param>
	/// <param name="canBeDismissedByTappingOutsideOfPopup">A <see cref="bool"/> indicating whether the <see cref="CommunityToolkit.Maui.Core.IPopup"/> can be dismissed by tapping outside of it.</param>
	/// <param name="resultWhenUserTapsOutsideOfPopup">An <see cref="object"/> that will be returned when the user taps outside of the <see cref="CommunityToolkit.Maui.Core.IPopup"/>.</param>
	/// <param name="verticalOptions">The vertical alignment of the <see cref="CommunityToolkit.Maui.Core.IPopup"/>.</param>
	/// <param name="horizontalOptions">The horizontal alignment of the <see cref="CommunityToolkit.Maui.Core.IPopup"/>.</param>
	/// <param name="size">The size of the <see cref="CommunityToolkit.Maui.Core.IPopup"/>.</param>
	/// <param name="color">The background color of the <see cref="CommunityToolkit.Maui.Core.IPopup"/>.</param>
	void ShowPopup<TViewModel>(
		Action<TViewModel> onPresenting,
		bool canBeDismissedByTappingOutsideOfPopup = true,
		object? resultWhenUserTapsOutsideOfPopup = default,
		LayoutAlignment verticalOptions = LayoutAlignment.Center,
		LayoutAlignment horizontalOptions = LayoutAlignment.Center,
		Size size = default,
		Color? color = default) where TViewModel : INotifyPropertyChanged;

	/// <summary>
	/// Resolves and displays a <see cref="CommunityToolkit.Maui.Core.IPopup"/> and <typeparamref name="TViewModel"/> pair that was registered with <c>AddTransientPopup</c>.
	/// </summary>
	/// <typeparam name="TViewModel">The type of the view model registered with the <see cref="CommunityToolkit.Maui.Core.IPopup"/>.</typeparam>
	/// <param name="canBeDismissedByTappingOutsideOfPopup">A <see cref="bool"/> indicating whether the <see cref="CommunityToolkit.Maui.Core.IPopup"/> can be dismissed by tapping outside of it.</param>
	/// <param name="resultWhenUserTapsOutsideOfPopup">An <see cref="object"/> that will be returned when the user taps outside of the <see cref="CommunityToolkit.Maui.Core.IPopup"/>.</param>
	/// <param name="verticalOptions">The vertical alignment of the <see cref="CommunityToolkit.Maui.Core.IPopup"/>.</param>
	/// <param name="horizontalOptions">The horizontal alignment of the <see cref="CommunityToolkit.Maui.Core.IPopup"/>.</param>
	/// <param name="size">The size of the <see cref="CommunityToolkit.Maui.Core.IPopup"/>.</param>
	/// <param name="color">The background color of the <see cref="CommunityToolkit.Maui.Core.IPopup"/>.</param>
	/// <param name="token"><see cref="CancellationToken"/> </param>
	/// <returns>A <see cref="Task"/> that can be awaited to return the result of the <see cref="CommunityToolkit.Maui.Core.IPopup"/> once it has been dismissed.</returns>
	Task<object?> ShowPopupAsync<TViewModel>(
		bool canBeDismissedByTappingOutsideOfPopup = true,
		object? resultWhenUserTapsOutsideOfPopup = default,
		LayoutAlignment verticalOptions = LayoutAlignment.Center,
		LayoutAlignment horizontalOptions = LayoutAlignment.Center,
		Size size = default,
		Color? color = default,
		CancellationToken token = default) where TViewModel : INotifyPropertyChanged;

	/// <summary>
	/// Resolves and displays a <see cref="CommunityToolkit.Maui.Core.IPopup"/> and <typeparamref name="TViewModel"/> pair that was registered with <c>AddTransientPopup</c>.
	/// The supplied <paramref name="onPresenting"/> provides a mechanism to invoke any methods on your view model in order to load or pass data to it.
	/// </summary>
	/// <typeparam name="TViewModel">The type of the view model registered with the <see cref="CommunityToolkit.Maui.Core.IPopup"/>.</typeparam>
	/// <returns>A <see cref="Task"/> that can be awaited to return the result of the <see cref="CommunityToolkit.Maui.Core.IPopup"/> once it has been dismissed.</returns>
	/// <param name="onPresenting">An <see cref="Action{TViewModel}"/> that will be performed before the popup is presented.</param>
	/// <param name="canBeDismissedByTappingOutsideOfPopup">A <see cref="bool"/> indicating whether the <see cref="CommunityToolkit.Maui.Core.IPopup"/> can be dismissed by tapping outside of it.</param>
	/// <param name="resultWhenUserTapsOutsideOfPopup">An <see cref="object"/> that will be returned when the user taps outside of the <see cref="CommunityToolkit.Maui.Core.IPopup"/>.</param>
	/// <param name="verticalOptions">The vertical alignment of the <see cref="CommunityToolkit.Maui.Core.IPopup"/>.</param>
	/// <param name="horizontalOptions">The horizontal alignment of the <see cref="CommunityToolkit.Maui.Core.IPopup"/>.</param>
	/// <param name="size">The size of the <see cref="CommunityToolkit.Maui.Core.IPopup"/>.</param>
	/// <param name="color">The background color of the <see cref="CommunityToolkit.Maui.Core.IPopup"/>.</param>
	/// <param name="token"><see cref="CancellationToken"/> </param>
	Task<object?> ShowPopupAsync<TViewModel>(
		Action<TViewModel> onPresenting,
		bool canBeDismissedByTappingOutsideOfPopup = true,
		object? resultWhenUserTapsOutsideOfPopup = default,
		LayoutAlignment verticalOptions = LayoutAlignment.Center,
		LayoutAlignment horizontalOptions = LayoutAlignment.Center,
		Size size = default,
		Color? color = default,
		CancellationToken token = default) where TViewModel : INotifyPropertyChanged;
}