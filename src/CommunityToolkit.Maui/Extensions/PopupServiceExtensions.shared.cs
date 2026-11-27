using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Extensions;

/// <summary>
/// Extension methods that show and close popups through an <see cref="IPopupService"/> and return a <see cref="PopupResultOrException"/> instead of throwing when the operation fails.
/// </summary>
/// <remarks>These methods are extensions, rather than members of <see cref="IPopupService"/>, so existing implementations of the interface keep compiling.</remarks>
public static class PopupServiceExtensions
{
	/// <summary>
	/// Shows a popup and returns a <see cref="PopupResultOrException"/> instead of throwing when the popup fails.
	/// </summary>
	/// <typeparam name="T">Supports both Popup Type or Popup ViewModel Type</typeparam>
	/// <param name="popupService">The <see cref="IPopupService"/> that creates and displays the popup.</param>
	/// <param name="page">The current visible page</param>
	/// <param name="options">The <see cref="IPopupOptions"/> that enable support for customizing the display and behavior of the presented popup.</param>
	/// <param name="cancellationToken">A <see cref="CancellationToken"/> providing support for canceling the wait for a result to be returned. This will <b>not</b> close the popup.</param>
	/// <returns>A <see cref="PopupResultOrException"/> that holds the <see cref="IPopupResult"/> when the popup is closed, or the <see cref="Exception"/> when the popup fails. Also returns a dismissed result when navigation removes the popup's page without <c>CloseAsync</c>. Failures include an <see cref="OperationCanceledException"/> when the <paramref name="cancellationToken"/> is canceled (the popup stays open) or when the app cancels the navigation that displays the popup, for example in a <see cref="Shell.Navigating"/> handler (the popup is not displayed), and any exception thrown while the popup starts, such as an <see cref="InvalidOperationException"/> when <typeparamref name="T"/> is not registered. Use pattern matching to handle each case, and check <see cref="IPopupResult.WasDismissedByTappingOutsideOfPopup"/> to determine how the popup was closed. Invalid arguments still throw; see the exceptions below.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="popupService"/> or <paramref name="page"/> is <see langword="null"/>.</exception>
	public static Task<PopupResultOrException> TryShowPopupAsync<T>(this IPopupService popupService, Page page, IPopupOptions? options = null, CancellationToken cancellationToken = default)
		where T : notnull
	{
		ArgumentNullException.ThrowIfNull(popupService);
		ArgumentNullException.ThrowIfNull(page);

		return TryShowPopupCoreAsync<T>(popupService, page, options, cancellationToken);
	}

	/// <summary>
	/// Shows a popup and returns a <see cref="PopupResultOrException"/> instead of throwing when the popup fails.
	/// </summary>
	/// <typeparam name="T">Supports both Popup Type or Popup ViewModel Type</typeparam>
	/// <param name="popupService">The <see cref="IPopupService"/> that creates and displays the popup.</param>
	/// <param name="navigation">The <see cref="INavigation"/> implementation responsible for displaying the popup. Make sure to use the one associated with the <see cref="Window"/> that you wish the popup to be displayed on.</param>
	/// <param name="options">The <see cref="IPopupOptions"/> that enable support for customizing the display and behavior of the presented popup.</param>
	/// <param name="cancellationToken">A <see cref="CancellationToken"/> providing support for canceling the wait for a result to be returned. This will <b>not</b> close the popup.</param>
	/// <returns>A <see cref="PopupResultOrException"/> that holds the <see cref="IPopupResult"/> when the popup is closed, or the <see cref="Exception"/> when the popup fails. Also returns a dismissed result when navigation removes the popup's page without <c>CloseAsync</c>. Failures include an <see cref="OperationCanceledException"/> when the <paramref name="cancellationToken"/> is canceled (the popup stays open) or when the app cancels the navigation that displays the popup, for example in a <see cref="Shell.Navigating"/> handler (the popup is not displayed), and any exception thrown while the popup starts, such as an <see cref="InvalidOperationException"/> when <typeparamref name="T"/> is not registered. Use pattern matching to handle each case, and check <see cref="IPopupResult.WasDismissedByTappingOutsideOfPopup"/> to determine how the popup was closed. Invalid arguments still throw; see the exceptions below.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="popupService"/> or <paramref name="navigation"/> is <see langword="null"/>.</exception>
	public static Task<PopupResultOrException> TryShowPopupAsync<T>(this IPopupService popupService, INavigation navigation, IPopupOptions? options = null, CancellationToken cancellationToken = default)
		where T : notnull
	{
		ArgumentNullException.ThrowIfNull(popupService);
		ArgumentNullException.ThrowIfNull(navigation);

		return TryShowPopupCoreAsync<T>(popupService, navigation, options, cancellationToken);
	}

	/// <summary>
	/// Shows a popup and returns a <see cref="PopupResultOrException"/> instead of throwing when the popup fails.
	/// </summary>
	/// <typeparam name="T">Supports both Popup Type or Popup ViewModel Type</typeparam>
	/// <param name="popupService">The <see cref="IPopupService"/> that creates and displays the popup.</param>
	/// <param name="shell">Current <see cref="Shell"/></param>
	/// <param name="options">The <see cref="IPopupOptions"/> that enable support for customizing the display and behavior of the presented popup.</param>
	/// <param name="shellParameters">Parameters that will be passed into the view or its associated BindingContext if they implement <see cref="IQueryAttributable"/>.</param>
	/// <param name="cancellationToken">A <see cref="CancellationToken"/> providing support for canceling the wait for a result to be returned. This will <b>not</b> close the popup.</param>
	/// <returns>A <see cref="PopupResultOrException"/> that holds the <see cref="IPopupResult"/> when the popup is closed, or the <see cref="Exception"/> when the popup fails. Also returns a dismissed result when navigation removes the popup's page without <c>CloseAsync</c>. Failures include an <see cref="OperationCanceledException"/> when the <paramref name="cancellationToken"/> is canceled (the popup stays open) or when the app cancels the navigation that displays the popup, for example in a <see cref="Shell.Navigating"/> handler (the popup is not displayed), and any exception thrown while the popup starts, such as an <see cref="InvalidOperationException"/> when <typeparamref name="T"/> is not registered. Use pattern matching to handle each case, and check <see cref="IPopupResult.WasDismissedByTappingOutsideOfPopup"/> to determine how the popup was closed. Invalid arguments still throw; see the exceptions below.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="popupService"/> or <paramref name="shell"/> is <see langword="null"/>.</exception>
	public static Task<PopupResultOrException> TryShowPopupAsync<T>(this IPopupService popupService, Shell shell, IPopupOptions? options = null, IDictionary<string, object>? shellParameters = null, CancellationToken cancellationToken = default)
		where T : notnull
	{
		ArgumentNullException.ThrowIfNull(popupService);
		ArgumentNullException.ThrowIfNull(shell);

		return TryShowPopupCoreAsync<T>(popupService, shell, options, shellParameters, cancellationToken);
	}

	/// <summary>
	/// Shows a popup and returns a <see cref="PopupResultOrException{TResult}"/> instead of throwing when the popup fails.
	/// </summary>
	/// <typeparam name="T">Supports both Popup Type or Popup ViewModel Type</typeparam>
	/// <typeparam name="TResult">Popup Result Type</typeparam>
	/// <param name="popupService">The <see cref="IPopupService"/> that creates and displays the popup.</param>
	/// <param name="page">The current visible page</param>
	/// <param name="options">The <see cref="IPopupOptions"/> that enable support for customizing the display and behavior of the presented popup.</param>
	/// <param name="cancellationToken">A <see cref="CancellationToken"/> providing support for canceling the wait for a result to be returned. This will <b>not</b> close the popup.</param>
	/// <returns>A <see cref="PopupResultOrException{TResult}"/> that holds the <see cref="IPopupResult{TResult}"/> when the popup is closed, or the <see cref="Exception"/> when the popup fails. Also returns a dismissed result when navigation removes the popup's page without <c>CloseAsync</c>. Failures include an <see cref="OperationCanceledException"/> when the <paramref name="cancellationToken"/> is canceled (the popup stays open) or when the app cancels the navigation that displays the popup, for example in a <see cref="Shell.Navigating"/> handler (the popup is not displayed), and any exception thrown while the popup starts, such as an <see cref="InvalidOperationException"/> when <typeparamref name="T"/> is not registered. Use pattern matching to handle each case, and check <see cref="IPopupResult.WasDismissedByTappingOutsideOfPopup"/> to determine how the popup was closed. Invalid arguments still throw; see the exceptions below.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="popupService"/> or <paramref name="page"/> is <see langword="null"/>.</exception>
	public static Task<PopupResultOrException<TResult>> TryShowPopupAsync<T, TResult>(this IPopupService popupService, Page page, IPopupOptions? options = null, CancellationToken cancellationToken = default)
		where T : notnull
	{
		ArgumentNullException.ThrowIfNull(popupService);
		ArgumentNullException.ThrowIfNull(page);

		return TryShowPopupCoreAsync<T, TResult>(popupService, page, options, cancellationToken);
	}

	/// <summary>
	/// Shows a popup and returns a <see cref="PopupResultOrException{TResult}"/> instead of throwing when the popup fails.
	/// </summary>
	/// <typeparam name="T">Supports both Popup Type or Popup ViewModel Type</typeparam>
	/// <typeparam name="TResult">Popup Result Type</typeparam>
	/// <param name="popupService">The <see cref="IPopupService"/> that creates and displays the popup.</param>
	/// <param name="navigation">The <see cref="INavigation"/> implementation responsible for displaying the popup. Make sure to use the one associated with the <see cref="Window"/> that you wish the popup to be displayed on.</param>
	/// <param name="options">The <see cref="IPopupOptions"/> that enable support for customizing the display and behavior of the presented popup.</param>
	/// <param name="cancellationToken">A <see cref="CancellationToken"/> providing support for canceling the wait for a result to be returned. This will <b>not</b> close the popup.</param>
	/// <returns>A <see cref="PopupResultOrException{TResult}"/> that holds the <see cref="IPopupResult{TResult}"/> when the popup is closed, or the <see cref="Exception"/> when the popup fails. Also returns a dismissed result when navigation removes the popup's page without <c>CloseAsync</c>. Failures include an <see cref="OperationCanceledException"/> when the <paramref name="cancellationToken"/> is canceled (the popup stays open) or when the app cancels the navigation that displays the popup, for example in a <see cref="Shell.Navigating"/> handler (the popup is not displayed), and any exception thrown while the popup starts, such as an <see cref="InvalidOperationException"/> when <typeparamref name="T"/> is not registered. Use pattern matching to handle each case, and check <see cref="IPopupResult.WasDismissedByTappingOutsideOfPopup"/> to determine how the popup was closed. Invalid arguments still throw; see the exceptions below.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="popupService"/> or <paramref name="navigation"/> is <see langword="null"/>.</exception>
	public static Task<PopupResultOrException<TResult>> TryShowPopupAsync<T, TResult>(this IPopupService popupService, INavigation navigation, IPopupOptions? options = null, CancellationToken cancellationToken = default)
		where T : notnull
	{
		ArgumentNullException.ThrowIfNull(popupService);
		ArgumentNullException.ThrowIfNull(navigation);

		return TryShowPopupCoreAsync<T, TResult>(popupService, navigation, options, cancellationToken);
	}

	/// <summary>
	/// Shows a popup and returns a <see cref="PopupResultOrException{TResult}"/> instead of throwing when the popup fails.
	/// </summary>
	/// <typeparam name="T">Supports both Popup Type or Popup ViewModel Type</typeparam>
	/// <typeparam name="TResult">Popup Result Type</typeparam>
	/// <param name="popupService">The <see cref="IPopupService"/> that creates and displays the popup.</param>
	/// <param name="shell">Current <see cref="Shell"/></param>
	/// <param name="options">The <see cref="IPopupOptions"/> that enable support for customizing the display and behavior of the presented popup.</param>
	/// <param name="shellParameters">Parameters that will be passed into the view or its associated BindingContext if they implement <see cref="IQueryAttributable"/>.</param>
	/// <param name="cancellationToken">A <see cref="CancellationToken"/> providing support for canceling the wait for a result to be returned. This will <b>not</b> close the popup.</param>
	/// <returns>A <see cref="PopupResultOrException{TResult}"/> that holds the <see cref="IPopupResult{TResult}"/> when the popup is closed, or the <see cref="Exception"/> when the popup fails. Also returns a dismissed result when navigation removes the popup's page without <c>CloseAsync</c>. Failures include an <see cref="OperationCanceledException"/> when the <paramref name="cancellationToken"/> is canceled (the popup stays open) or when the app cancels the navigation that displays the popup, for example in a <see cref="Shell.Navigating"/> handler (the popup is not displayed), and any exception thrown while the popup starts, such as an <see cref="InvalidOperationException"/> when <typeparamref name="T"/> is not registered. Use pattern matching to handle each case, and check <see cref="IPopupResult.WasDismissedByTappingOutsideOfPopup"/> to determine how the popup was closed. Invalid arguments still throw; see the exceptions below.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="popupService"/> or <paramref name="shell"/> is <see langword="null"/>.</exception>
	public static Task<PopupResultOrException<TResult>> TryShowPopupAsync<T, TResult>(this IPopupService popupService, Shell shell, IPopupOptions? options = null, IDictionary<string, object>? shellParameters = null, CancellationToken cancellationToken = default)
		where T : notnull
	{
		ArgumentNullException.ThrowIfNull(popupService);
		ArgumentNullException.ThrowIfNull(shell);

		return TryShowPopupCoreAsync<T, TResult>(popupService, shell, options, shellParameters, cancellationToken);
	}

	/// <summary>
	/// Closes the most recent popup and returns a <see cref="PopupResultOrException"/> that holds details about the closure, or the exception when the popup cannot be closed.
	/// </summary>
	/// <param name="popupService">The <see cref="IPopupService"/> that creates and displays the popup.</param>
	/// <param name="page">The current visible page</param>
	/// <param name="cancellationToken">A <see cref="CancellationToken"/> providing support for canceling the operation.</param>
	/// <returns>A <see cref="PopupResultOrException"/> that holds the <see cref="IPopupResult"/> when the popup is closed, or the <see cref="Exception"/> when the popup cannot be closed: a <see cref="PopupNotFoundException"/> when the navigation shows no modal page, a <see cref="PopupBlockedException"/> when the top modal page is not a popup, or an <see cref="OperationCanceledException"/> when the <paramref name="cancellationToken"/> is canceled or when the app cancels the navigation that removes the popup, for example in a <see cref="Window.ModalPopping"/>, <see cref="Microsoft.Maui.Controls.Application.ModalPopping"/> or <see cref="Shell.Navigating"/> handler (the popup stays open). Invalid arguments still throw; see the exceptions below.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="popupService"/> or <paramref name="page"/> is <see langword="null"/>.</exception>
	public static Task<PopupResultOrException> TryClosePopupAsync(this IPopupService popupService, Page page, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(popupService);
		ArgumentNullException.ThrowIfNull(page);

		return TryClosePopupCoreAsync(popupService, page, cancellationToken);
	}

	/// <summary>
	/// Closes the most recent popup with a result and returns a <see cref="PopupResultOrException{T}"/> that holds details about the closure, or the exception when the popup cannot be closed.
	/// </summary>
	/// <typeparam name="T">The type of the result that the popup returns.</typeparam>
	/// <param name="popupService">The <see cref="IPopupService"/> that creates and displays the popup.</param>
	/// <param name="page">The current visible page</param>
	/// <param name="result">The result that will be returned to the caller of <c>ShowPopupAsync</c>.</param>
	/// <param name="cancellationToken">A <see cref="CancellationToken"/> providing support for canceling the operation.</param>
	/// <returns>A <see cref="PopupResultOrException{T}"/> that holds the <see cref="IPopupResult{T}"/> when the popup is closed, or the <see cref="Exception"/> when the popup cannot be closed: a <see cref="PopupNotFoundException"/> when the navigation shows no modal page, a <see cref="PopupBlockedException"/> when the top modal page is not a popup, or an <see cref="OperationCanceledException"/> when the <paramref name="cancellationToken"/> is canceled or when the app cancels the navigation that removes the popup, for example in a <see cref="Window.ModalPopping"/>, <see cref="Microsoft.Maui.Controls.Application.ModalPopping"/> or <see cref="Shell.Navigating"/> handler (the popup stays open). Invalid arguments still throw; see the exceptions below.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="popupService"/> or <paramref name="page"/> is <see langword="null"/>.</exception>
	public static Task<PopupResultOrException<T>> TryClosePopupAsync<T>(this IPopupService popupService, Page page, T result, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(popupService);
		ArgumentNullException.ThrowIfNull(page);

		return TryClosePopupCoreAsync<T>(popupService, page, result, cancellationToken);
	}

	/// <summary>
	/// Closes the most recent popup and returns a <see cref="PopupResultOrException"/> that holds details about the closure, or the exception when the popup cannot be closed.
	/// </summary>
	/// <param name="popupService">The <see cref="IPopupService"/> that creates and displays the popup.</param>
	/// <param name="navigation">The active <see cref="INavigation"/>, a property commonly found in <see cref="Microsoft.Maui.Controls.VisualElement"/></param>
	/// <param name="cancellationToken">A <see cref="CancellationToken"/> providing support for canceling the operation.</param>
	/// <returns>A <see cref="PopupResultOrException"/> that holds the <see cref="IPopupResult"/> when the popup is closed, or the <see cref="Exception"/> when the popup cannot be closed: a <see cref="PopupNotFoundException"/> when the navigation shows no modal page, a <see cref="PopupBlockedException"/> when the top modal page is not a popup, or an <see cref="OperationCanceledException"/> when the <paramref name="cancellationToken"/> is canceled or when the app cancels the navigation that removes the popup, for example in a <see cref="Window.ModalPopping"/>, <see cref="Microsoft.Maui.Controls.Application.ModalPopping"/> or <see cref="Shell.Navigating"/> handler (the popup stays open). Invalid arguments still throw; see the exceptions below.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="popupService"/> or <paramref name="navigation"/> is <see langword="null"/>.</exception>
	public static Task<PopupResultOrException> TryClosePopupAsync(this IPopupService popupService, INavigation navigation, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(popupService);
		ArgumentNullException.ThrowIfNull(navigation);

		return TryClosePopupCoreAsync(popupService, navigation, cancellationToken);
	}

	/// <summary>
	/// Closes the most recent popup with a result and returns a <see cref="PopupResultOrException{T}"/> that holds details about the closure, or the exception when the popup cannot be closed.
	/// </summary>
	/// <typeparam name="T">The type of the result that the popup returns.</typeparam>
	/// <param name="popupService">The <see cref="IPopupService"/> that creates and displays the popup.</param>
	/// <param name="navigation">The active <see cref="INavigation"/>, a property commonly found in <see cref="Microsoft.Maui.Controls.VisualElement"/></param>
	/// <param name="result">The result that will be returned to the caller of <c>ShowPopupAsync</c>.</param>
	/// <param name="cancellationToken">A <see cref="CancellationToken"/> providing support for canceling the operation.</param>
	/// <returns>A <see cref="PopupResultOrException{T}"/> that holds the <see cref="IPopupResult{T}"/> when the popup is closed, or the <see cref="Exception"/> when the popup cannot be closed: a <see cref="PopupNotFoundException"/> when the navigation shows no modal page, a <see cref="PopupBlockedException"/> when the top modal page is not a popup, or an <see cref="OperationCanceledException"/> when the <paramref name="cancellationToken"/> is canceled or when the app cancels the navigation that removes the popup, for example in a <see cref="Window.ModalPopping"/>, <see cref="Microsoft.Maui.Controls.Application.ModalPopping"/> or <see cref="Shell.Navigating"/> handler (the popup stays open). Invalid arguments still throw; see the exceptions below.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="popupService"/> or <paramref name="navigation"/> is <see langword="null"/>.</exception>
	public static Task<PopupResultOrException<T>> TryClosePopupAsync<T>(this IPopupService popupService, INavigation navigation, T result, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(popupService);
		ArgumentNullException.ThrowIfNull(navigation);

		return TryClosePopupCoreAsync<T>(popupService, navigation, result, cancellationToken);
	}

	// The public members validate their arguments before they call these methods, so the try/catch in each core cannot turn an invalid argument into a returned exception
	static async Task<PopupResultOrException> TryShowPopupCoreAsync<T>(IPopupService popupService, Page page, IPopupOptions? options, CancellationToken cancellationToken)
		where T : notnull
	{
		try
		{
			return await popupService.ShowPopupAsync<T>(page, options, cancellationToken);
		}
		catch (Exception e)
		{
			return e;
		}
	}

	static async Task<PopupResultOrException> TryShowPopupCoreAsync<T>(IPopupService popupService, INavigation navigation, IPopupOptions? options, CancellationToken cancellationToken)
		where T : notnull
	{
		try
		{
			return await popupService.ShowPopupAsync<T>(navigation, options, cancellationToken);
		}
		catch (Exception e)
		{
			return e;
		}
	}

	static async Task<PopupResultOrException> TryShowPopupCoreAsync<T>(IPopupService popupService, Shell shell, IPopupOptions? options, IDictionary<string, object>? shellParameters, CancellationToken cancellationToken)
		where T : notnull
	{
		try
		{
			return await popupService.ShowPopupAsync<T>(shell, options, shellParameters, cancellationToken);
		}
		catch (Exception e)
		{
			return e;
		}
	}

	static async Task<PopupResultOrException<TResult>> TryShowPopupCoreAsync<T, TResult>(IPopupService popupService, Page page, IPopupOptions? options, CancellationToken cancellationToken)
		where T : notnull
	{
		try
		{
			return await popupService.ShowPopupAsync<T, TResult>(page, options, cancellationToken);
		}
		catch (Exception e)
		{
			return e;
		}
	}

	static async Task<PopupResultOrException<TResult>> TryShowPopupCoreAsync<T, TResult>(IPopupService popupService, INavigation navigation, IPopupOptions? options, CancellationToken cancellationToken)
		where T : notnull
	{
		try
		{
			return await popupService.ShowPopupAsync<T, TResult>(navigation, options, cancellationToken);
		}
		catch (Exception e)
		{
			return e;
		}
	}

	static async Task<PopupResultOrException<TResult>> TryShowPopupCoreAsync<T, TResult>(IPopupService popupService, Shell shell, IPopupOptions? options, IDictionary<string, object>? shellParameters, CancellationToken cancellationToken)
		where T : notnull
	{
		try
		{
			return await popupService.ShowPopupAsync<T, TResult>(shell, options, shellParameters, cancellationToken);
		}
		catch (Exception e)
		{
			return e;
		}
	}

	static async Task<PopupResultOrException> TryClosePopupCoreAsync(IPopupService popupService, Page page, CancellationToken cancellationToken)
	{
		try
		{
			return await popupService.ClosePopupAsync(page, cancellationToken);
		}
		catch (Exception e)
		{
			return e;
		}
	}

	static async Task<PopupResultOrException<T>> TryClosePopupCoreAsync<T>(IPopupService popupService, Page page, T result, CancellationToken cancellationToken)
	{
		try
		{
			return await popupService.ClosePopupAsync(page, result, cancellationToken);
		}
		catch (Exception e)
		{
			return e;
		}
	}

	static async Task<PopupResultOrException> TryClosePopupCoreAsync(IPopupService popupService, INavigation navigation, CancellationToken cancellationToken)
	{
		try
		{
			return await popupService.ClosePopupAsync(navigation, cancellationToken);
		}
		catch (Exception e)
		{
			return e;
		}
	}

	static async Task<PopupResultOrException<T>> TryClosePopupCoreAsync<T>(IPopupService popupService, INavigation navigation, T result, CancellationToken cancellationToken)
	{
		try
		{
			return await popupService.ClosePopupAsync(navigation, result, cancellationToken);
		}
		catch (Exception e)
		{
			return e;
		}
	}
}