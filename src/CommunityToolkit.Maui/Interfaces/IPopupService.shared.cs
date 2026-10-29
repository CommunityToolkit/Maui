using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui;

/// <summary>
/// Provides a mechanism for displaying Popups based on the underlying view model.
/// </summary>
public interface IPopupService
{
	/// <summary>
	/// Shows a popup with the specified options.
	/// </summary>
	/// <typeparam name="T">Supports both Popup Type or Popup ViewModel Type</typeparam>
	/// <param name="page">The current visible page</param>
	/// <param name="options">The <see cref="IPopupOptions"/> that enable support for customizing the display and behavior of the presented popup.</param>
	void ShowPopup<T>(Page page, IPopupOptions? options = null)
		where T : notnull;

	/// <summary>
	/// Shows a popup with the specified options.
	/// </summary>
	/// <typeparam name="T">Supports both Popup Type or Popup ViewModel Type</typeparam>
	/// <param name="navigation">The <see cref="INavigation"/> implementation responsible for displaying the popup. Make sure to use the one associated with the <see cref="Window"/> that you wish the popup to be displayed on.</param>
	/// <param name="options">The <see cref="IPopupOptions"/> that enable support for customizing the display and behavior of the presented popup.</param>
	void ShowPopup<T>(INavigation navigation, IPopupOptions? options = null)
		where T : notnull;

	/// <summary>
	/// Shows a popup with the specified options.
	/// </summary>
	/// <typeparam name="T">Supports both Popup Type or Popup ViewModel Type</typeparam>
	/// <param name="shell">Current <see cref="Shell"/></param>
	/// <param name="options">The <see cref="IPopupOptions"/> that enable support for customizing the display and behavior of the presented popup.</param>
	/// <param name="shellParameters">Parameters that will be passed into the view or its associated BindingContext if they implement <see cref="IQueryAttributable"/>.</param>
	void ShowPopup<T>(Shell shell, IPopupOptions? options = null, IDictionary<string, object>? shellParameters = null)
		where T : notnull;

	/// <summary>
	/// Shows a popup with the specified options.
	/// </summary>
	/// <typeparam name="T">Supports both Popup Type or Popup ViewModel Type</typeparam>
	/// <param name="page">The current visible page</param>
	/// <param name="options">The <see cref="IPopupOptions"/> that enable support for customizing the display and behavior of the presented popup.</param>
	/// <param name="cancellationToken">A <see cref="CancellationToken"/> providing support for canceling the wait for a result to be returned. This will <b>not</b> close the popup.</param>
	/// <returns>An <see cref="IPopupResult"/> when the popup is closed or the <paramref name="cancellationToken"/> is cancelled. Make sure to check the <see cref="IPopupResult.WasDismissed"/> value to determine how the popup was closed.</returns>
	Task<IPopupResult> ShowPopupAsync<T>(Page page, IPopupOptions? options = null, CancellationToken cancellationToken = default)
		where T : notnull;

	/// <summary>
	/// Shows a popup with the specified options.
	/// </summary>
	/// <typeparam name="T">Supports both Popup Type or Popup ViewModel Type</typeparam>
	/// <param name="navigation">The <see cref="INavigation"/> implementation responsible for displaying the popup. Make sure to use the one associated with the <see cref="Window"/> that you wish the popup to be displayed on.</param>
	/// <param name="options">The <see cref="IPopupOptions"/> that enable support for customizing the display and behavior of the presented popup.</param>
	/// <param name="cancellationToken">A <see cref="CancellationToken"/> providing support for canceling the wait for a result to be returned. This will <b>not</b> close the popup.</param>
	/// <returns>An <see cref="IPopupResult"/> when the popup is closed or the <paramref name="cancellationToken"/> is cancelled. Make sure to check the <see cref="IPopupResult.WasDismissed"/> value to determine how the popup was closed.</returns>
	Task<IPopupResult> ShowPopupAsync<T>(INavigation navigation, IPopupOptions? options = null, CancellationToken cancellationToken = default)
		where T : notnull;

	/// <summary>
	/// Shows a popup with the specified options.
	/// </summary>
	/// <typeparam name="T">Supports both Popup Type or Popup ViewModel Type</typeparam>
	/// <param name="shell">Current <see cref="Shell"/></param>
	/// <param name="options">The <see cref="IPopupOptions"/> that enable support for customizing the display and behavior of the presented popup.</param>
	/// <param name="shellParameters">Parameters that will be passed into the view or its associated BindingContext if they implement <see cref="IQueryAttributable"/>.</param>
	/// <param name="cancellationToken">A <see cref="CancellationToken"/> providing support for canceling the wait for a result to be returned. This will <b>not</b> close the popup.</param>
	/// <returns>An <see cref="IPopupResult"/> when the popup is closed or the <paramref name="cancellationToken"/> is cancelled. Make sure to check the <see cref="IPopupResult.WasDismissed"/> value to determine how the popup was closed.</returns>
	Task<IPopupResult> ShowPopupAsync<T>(Shell shell, IPopupOptions? options, IDictionary<string, object>? shellParameters = null, CancellationToken cancellationToken = default)
		where T : notnull;

	/// <summary>
	/// Shows a popup with the specified options.
	/// </summary>
	/// <typeparam name="T">Supports both Popup Type or Popup ViewModel Type</typeparam>
	/// <typeparam name="TResult">Popup Result Type</typeparam>
	/// <param name="page">The current visible page</param>
	/// <param name="options">The <see cref="IPopupOptions"/> that enable support for customizing the display and behavior of the presented popup.</param>
	/// <param name="cancellationToken">A <see cref="CancellationToken"/> providing support for canceling the wait for a result to be returned. This will <b>not</b> close the popup.</param>
	/// <returns>An <see cref="IPopupResult{TResult}"/> when the popup is closed or the <paramref name="cancellationToken"/> is cancelled. Make sure to check the <see cref="IPopupResult.WasDismissed"/> value to determine how the popup was closed.</returns>
	Task<IPopupResult<TResult>> ShowPopupAsync<T, TResult>(Page page, IPopupOptions? options = null, CancellationToken cancellationToken = default)
		where T : notnull;

	/// <summary>
	/// Shows a popup with the specified options.
	/// </summary>
	/// <typeparam name="T">Supports both Popup Type or Popup ViewModel Type</typeparam>
	/// <typeparam name="TResult">Popup Result Type</typeparam>
	/// <param name="navigation">The active <see cref="INavigation"/>, a property commonly found in <see cref="Microsoft.Maui.Controls.VisualElement"/></param>
	/// <param name="options">The <see cref="IPopupOptions"/> that enable support for customizing the display and behavior of the presented popup.</param>
	/// <param name="cancellationToken">A <see cref="CancellationToken"/> providing support for canceling the wait for a result to be returned. This will <b>not</b> close the popup.</param>
	/// <returns>An <see cref="IPopupResult{TResult}"/> when the popup is closed or the <paramref name="cancellationToken"/> is cancelled. Make sure to check the <see cref="IPopupResult.WasDismissed"/> value to determine how the popup was closed.</returns>
	Task<IPopupResult<TResult>> ShowPopupAsync<T, TResult>(INavigation navigation, IPopupOptions? options = null, CancellationToken cancellationToken = default)
		where T : notnull;

	/// <summary>
	/// Shows a popup with the specified options.
	/// </summary>
	/// <typeparam name="T">Supports both Popup Type or Popup ViewModel Type</typeparam>
	/// <typeparam name="TResult">Popup Result Type</typeparam>
	/// <param name="shell">Current <see cref="Shell"/></param>
	/// <param name="options">The <see cref="IPopupOptions"/> that enable support for customizing the display and behavior of the presented popup.</param>
	/// <param name="shellParameters">Parameters that will be passed into the view or its associated BindingContext if they implement <see cref="IQueryAttributable"/>.</param>
	/// <param name="cancellationToken">A <see cref="CancellationToken"/> providing support for canceling the wait for a result to be returned. This will <b>not</b> close the popup.</param>
	/// <returns>An <see cref="IPopupResult"/> when the popup is closed or the <paramref name="cancellationToken"/> is cancelled. Make sure to check the <see cref="IPopupResult.WasDismissed"/> value to determine how the popup was closed.</returns>
	Task<IPopupResult<TResult>> ShowPopupAsync<T, TResult>(Shell shell, IPopupOptions? options = null, IDictionary<string, object>? shellParameters = null, CancellationToken cancellationToken = default)
		where T : notnull;

	/// <summary>
	/// Closes the current popup.
	/// </summary>
	/// <param name="page">The current visible page</param>
	/// <param name="cancellationToken">A <see cref="CancellationToken"/> providing support for canceling the close of the current popup.</param>
	Task<IPopupResult> ClosePopupAsync(Page page, CancellationToken cancellationToken = default);

	/// <summary>
	/// Closes the current popup with a result.
	/// </summary>
	/// <param name="page">The current visible page</param>
	/// <param name="result">The result that will be returned to the caller of <c>ShowPopupAsync</c>.</param>
	/// <param name="cancellationToken">A <see cref="CancellationToken"/> providing support for canceling the close of the current popup.</param>
	Task<IPopupResult<T>> ClosePopupAsync<T>(Page page, T result, CancellationToken cancellationToken = default);

	/// <summary>
	/// Closes the current popup.
	/// </summary>
	/// <param name="navigation">The active <see cref="INavigation"/>, a property commonly found in <see cref="Microsoft.Maui.Controls.VisualElement"/></param>
	/// <param name="cancellationToken">A <see cref="CancellationToken"/> providing support for canceling the close of the current popup.</param>
	Task<IPopupResult> ClosePopupAsync(INavigation navigation, CancellationToken cancellationToken = default);

	/// <summary>
	/// Closes the current popup with a result.
	/// </summary>
	/// <param name="navigation">The active <see cref="INavigation"/>, a property commonly found in <see cref="Microsoft.Maui.Controls.VisualElement"/></param>
	/// <param name="result">The result that will be returned to the caller of <c>ShowPopupAsync</c>.</param>
	/// <param name="cancellationToken">A <see cref="CancellationToken"/> providing support for canceling the close of the current popup.</param>
	Task<IPopupResult<T>> ClosePopupAsync<T>(INavigation navigation, T result, CancellationToken cancellationToken = default);
}