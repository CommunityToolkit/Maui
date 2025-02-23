using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Views;

/// <summary>
/// Popup extensions.
/// </summary>
public static class PopupExtensions
{
	/// <summary>
	/// Opens a popup with the specified options.
	/// </summary>
	/// <param name="page">Current page</param>
	/// <param name="view">Popup content</param>
	/// <param name="options"><see cref="PopupOptions"/></param>
	/// <param name="token"><see cref="CancellationToken"/></param>
	/// <returns>Popup Result</returns>
	public static Task<PopupResult<TResult>> ShowPopup<TResult>(this Page page, View view, PopupOptions options, CancellationToken token = default)
	{
		return page.Navigation.ShowPopup<TResult>(view, options, token);
	}
	
	/// <summary>
	/// Opens a popup with the specified options.
	/// </summary>
	/// <param name="navigation">Popup parent</param>
	/// <param name="view">Popup content</param>
	/// <param name="options"><see cref="PopupOptions"/></param>
	/// <param name="token"><see cref="CancellationToken"/></param>
	/// <returns>Popup Result</returns>
	public static async Task<PopupResult<TResult>> ShowPopup<TResult>(this INavigation navigation, View view, PopupOptions options, CancellationToken token = default)
	{
		token.ThrowIfCancellationRequested();
		
		TaskCompletionSource<PopupResult<TResult>> taskCompletionSource = new();
		
		var popupContainer = new PopupContainer<TResult>(view, options, taskCompletionSource);

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
	public static Task<PopupResult> ShowPopup(this Page page, View view, PopupOptions options, CancellationToken token = default)
	{
		return ShowPopup(page.Navigation, view, options, token);
	}

	/// <summary>
	/// Shows a popup with the specified options.
	/// </summary>
	/// <param name="navigation">Popup parent</param>
	/// <param name="view">Popup content</param>
	/// <param name="options"><see cref="PopupOptions"/></param>
	/// <param name="token"><see cref="CancellationToken"/></param>
	/// <returns><see cref="PopupResult"/></returns>
	public static async Task<PopupResult> ShowPopup(this INavigation navigation, View view, PopupOptions options, CancellationToken token = default)
	{
		token.ThrowIfCancellationRequested();
		
		TaskCompletionSource<PopupResult> taskCompletionSource = new();
		
		var popupContainer = new PopupContainer(view, options, taskCompletionSource);

		await navigation.PushModalAsync(popupContainer, false).WaitAsync(token);
		return await taskCompletionSource.Task.WaitAsync(token);
	}
}