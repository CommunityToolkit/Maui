using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui;

/// <summary>
/// Provides a mechanism for displaying Popups based on the underlying view model.
/// </summary>
public interface IPopupService
{
	/// <summary>
	/// Opens a popup with the specified options.
	/// </summary>
	/// <typeparam name="T">Supports both Popup Type or Popup ViewModel Type</typeparam>
	/// <param name="navigation">The parent of the popup</param>
	/// <param name="options"><see cref="IPopupOptions"/></param>
	void ShowPopup<T>(INavigation navigation, IPopupOptions? options = null)
		where T : notnull;

	/// <summary>
	/// Opens a popup with the specified options.
	/// </summary>
	/// <typeparam name="T">Supports both Popup Type or Popup ViewModel Type</typeparam>
	/// <param name="navigation">The parent of the popup</param>
	/// <param name="options"><see cref="IPopupOptions"/></param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="PopupResult"/></returns>
	Task<PopupResult> ShowPopupAsync<T>(INavigation navigation, IPopupOptions? options = null, CancellationToken cancellationToken = default)
		where T : notnull;

	/// <summary>
	/// Opens a popup with the specified options.
	/// </summary>
	/// <typeparam name="T">Supports both Popup Type or Popup ViewModel Type</typeparam>
	/// <typeparam name="TResult">Popup Result Type</typeparam>
	/// <param name="navigation">The active <see cref="INavigation"/>, a property commonly found in <see cref="Microsoft.Maui.Controls.VisualElement"/></param>
	/// <param name="options"><see cref="IPopupOptions"/></param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="PopupResult"/></returns>
	Task<PopupResult<TResult>> ShowPopupAsync<T, TResult>(INavigation navigation, IPopupOptions? options = null, CancellationToken cancellationToken = default)
		where T : notnull;

	/// <summary>
	/// Closes the current popup.
	/// </summary>
	/// <param name="navigation">The active <see cref="INavigation"/>, a property commonly found in <see cref="Microsoft.Maui.Controls.VisualElement"/></param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	Task ClosePopupAsync(INavigation navigation, CancellationToken cancellationToken = default);

	/// <summary>
	/// Closes the current popup with a result.
	/// </summary>
	/// <param name="navigation">The active <see cref="INavigation"/>, a property commonly found in <see cref="Microsoft.Maui.Controls.VisualElement"/></param>
	/// <param name="result">The result of a popup.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	Task ClosePopupAsync<T>(INavigation navigation, T result, CancellationToken cancellationToken = default);
}