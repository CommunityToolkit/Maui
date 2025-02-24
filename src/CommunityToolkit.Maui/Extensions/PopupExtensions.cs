using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Views;

/// <summary>
/// Popup extensions.
/// </summary>
public static class PopupExtensions
{
	/// <summary>
	/// Shows a popup with the specified options.
	/// </summary>
	/// <param name="page">Current page</param>
	/// <param name="view">Popup content</param>
	/// <param name="options"><see cref="PopupOptions"/></param>
	/// <returns><see cref="PopupResult"/></returns>
	/// <remarks>This is an <see keyword="async"/> <see keyword="void"/> method. Use <see cref="ShowPopupAsync(Microsoft.Maui.Controls.Page,Microsoft.Maui.Controls.View,CommunityToolkit.Maui.Core.PopupOptions?,System.Threading.CancellationToken)"/> to <see keyword="await"/> this method and return <see cref="PopupResult{T}"/> </remarks>
	public static void ShowPopup(this Page page, View view, PopupOptions? options = null)
	{
		ShowPopup(page.Navigation, view, options);
	}

	/// <summary>
	/// Shows a popup with the specified options.
	/// </summary>
	/// <param name="navigation">Popup parent</param>
	/// <param name="view">Popup content</param>
	/// <param name="options"><see cref="PopupOptions"/></param>
	/// <returns><see cref="PopupResult"/></returns>
	/// <remarks>This is an <see keyword="async"/> <see keyword="void"/> method. Use <see cref="ShowPopupAsync(Microsoft.Maui.Controls.Page,Microsoft.Maui.Controls.View,CommunityToolkit.Maui.Core.PopupOptions?,System.Threading.CancellationToken)"/> to <see keyword="await"/> this method</remarks>
	public static async void ShowPopup(this INavigation navigation, View view, PopupOptions? options)
	{
		var popupContainer = new PopupContainer(view, options ?? new(), null);

		await navigation.PushModalAsync(popupContainer, false);
	}

	/// <summary>
	/// Opens a popup with the specified options.
	/// </summary>
	/// <param name="page">Current page</param>
	/// <param name="view">Popup content</param>
	/// <param name="options"><see cref="PopupOptions"/></param>
	/// <param name="token"><see cref="CancellationToken"/></param>
	/// <returns>Popup Result</returns>
	public static Task<PopupResult<TResult>> ShowPopupAsync<TResult>(this Page page, View view, PopupOptions? options = null, CancellationToken token = default)
	{
		return page.Navigation.ShowPopupAsync<TResult>(view, options, token);
	}

	/// <summary>
	/// Opens a popup with the specified options.
	/// </summary>
	/// <param name="navigation">Popup parent</param>
	/// <param name="view">Popup content</param>
	/// <param name="options"><see cref="PopupOptions"/></param>
	/// <param name="token"><see cref="CancellationToken"/></param>
	/// <returns>Popup Result</returns>
	public static async Task<PopupResult<TResult>> ShowPopupAsync<TResult>(this INavigation navigation, View view, PopupOptions? options = null, CancellationToken token = default)
	{
		token.ThrowIfCancellationRequested();

		TaskCompletionSource<PopupResult<TResult>> taskCompletionSource = new();

		var popupContainer = new PopupContainer<TResult>(view, options ?? new(), taskCompletionSource);

		await navigation.PushModalAsync(popupContainer, false).WaitAsync(token);
		return await taskCompletionSource.Task.WaitAsync(token);
	}

	/// <summary>
	/// Shows a popup with the specified options.
	/// </summary>
	/// <param name="page">Current page</param>
	/// <param name="view">Popup content</param>
	/// <param name="options"><see cref="PopupOptions"/></param>
	/// <param name="token"><see cref="CancellationToken"/></param>
	/// <returns><see cref="PopupResult"/></returns>
	public static Task<PopupResult> ShowPopupAsync(this Page page, View view, PopupOptions? options = null, CancellationToken token = default)
	{
		return ShowPopupAsync(page.Navigation, view, options, token);
	}

	/// <summary>
	/// Shows a popup with the specified options.
	/// </summary>
	/// <param name="navigation">Popup parent</param>
	/// <param name="view">Popup content</param>
	/// <param name="options"><see cref="PopupOptions"/></param>
	/// <param name="token"><see cref="CancellationToken"/></param>
	/// <returns><see cref="PopupResult"/></returns>
	public static async Task<PopupResult> ShowPopupAsync(this INavigation navigation, View view, PopupOptions? options, CancellationToken token = default)
	{
		token.ThrowIfCancellationRequested();

		TaskCompletionSource<PopupResult> taskCompletionSource = new();

		var popupContainer = new PopupContainer(view, options ?? new(), taskCompletionSource);

		await navigation.PushModalAsync(popupContainer, false).WaitAsync(token);
		return await taskCompletionSource.Task.WaitAsync(token);
	}
}