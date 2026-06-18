using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Extensions;

/// <summary>
/// Extension methods that provide support for the display of a popup.
/// </summary>
public static class PopupExtensions
{
	/// <summary>
	/// Shows a popup with the specified options.
	/// </summary>
	/// <param name="page">The current page that provides access to the <see cref="INavigation"/> implementation.</param>
	/// <param name="view">The <see cref="View"/> that will be displayed as the content in the popup.</param>
	/// <param name="options">The <see cref="IPopupOptions"/> that enable support for customizing the display and behavior of the presented popup.</param>
	/// <remarks>This is an <see langword="async"/> <see langword="void"/> method. Use <see cref="ShowPopupAsync(Page,View,CommunityToolkit.Maui.IPopupOptions?,CancellationToken)"/> to <see langword="await"/> this method and return <see cref="PopupResult{T}"/> </remarks>
	public static void ShowPopup(this Page page, View view, IPopupOptions? options = null)
	{
		ArgumentNullException.ThrowIfNull(page);

		page.Navigation.ShowPopup(view, options);
	}

	/// <summary>
	/// Shows a popup with the specified options.
	/// </summary>
	/// <param name="navigation">The <see cref="INavigation"/> implementation responsible for displaying the popup. Make sure to use the one associated with the <see cref="Window"/> that you wish the popup to be displayed on.</param>
	/// <param name="view">The <see cref="View"/> that will be displayed as the content in the popup.</param>
	/// <param name="options">The <see cref="IPopupOptions"/> that enable support for customizing the display and behavior of the presented popup.</param>
	/// <remarks>This is an <see langword="async"/> <see langword="void"/> method. Use <see cref="ShowPopupAsync(Page,View,CommunityToolkit.Maui.IPopupOptions?,CancellationToken)"/> to <see langword="await"/> this method</remarks>
	public static async void ShowPopup(this INavigation navigation, View view, IPopupOptions? options = null)
	{
		ArgumentNullException.ThrowIfNull(navigation);
		ArgumentNullException.ThrowIfNull(view);

		var popupPage = new PopupPage(view, options);

		await popupPage.ShowAsync(navigation);
	}

	/// <summary>
	/// Shows a popup with the specified options.
	/// </summary>
	/// <param name="shell">Current <see cref="Shell"/></param>
	/// <param name="view">The <see cref="View"/> that will be displayed as the content in the popup.</param>
	/// <param name="options">The <see cref="IPopupOptions"/> that enable support for customizing the display and behavior of the presented popup.</param>
	/// <param name="shellParameters">Parameters that will be passed into the <paramref name="view"/> or its associated BindingContext if they implement <see cref="IQueryAttributable"/>.</param>
	/// <remarks>This is an <see langword="async"/> <see langword="void"/> method. Use <see cref="ShowPopupAsync(Page,View,CommunityToolkit.Maui.IPopupOptions?,CancellationToken)"/> to <see langword="await"/> this method</remarks>
	public static async void ShowPopup(this Shell shell, View view, IPopupOptions? options = null, IDictionary<string, object>? shellParameters = null)
	{
		ArgumentNullException.ThrowIfNull(shell);
		ArgumentNullException.ThrowIfNull(view);

		await shell.ShowPopupAsync(view, options, shellParameters);
	}
	
	/// <summary>
	/// Shows a popup with the specified options.
	/// </summary>
	/// <param name="page">The current page that provides access to the <see cref="INavigation"/> implementation.</param>
	/// <param name="view">The <see cref="View"/> that will be displayed as the content in the popup.</param>
	/// <param name="options">The <see cref="IPopupOptions"/> that enable support for customizing the display and behavior of the presented popup.</param>
	/// <param name="token">A <see cref="CancellationToken"/> providing support for canceling the wait for a result to be returned. This will <b>not</b> close the popup.</param>
	/// <returns>An <see cref="IShowPopupResult{TResult}"/> when the popup is closed or the <paramref name="token"/> is canceled. Make sure to check the <see cref="IPopupResult.WasDismissedByTappingOutsideOfPopup"/> value to determine how the popup was closed.</returns>
	public static Task<IShowPopupResult<TResult>> TryShowPopupAsync<TResult>(this Page page, View view, IPopupOptions? options = null, CancellationToken token = default)
	{
		ArgumentNullException.ThrowIfNull(page);

		return page.Navigation.TryShowPopupAsync<TResult>(view, options, token);
	}

	/// <summary>
	/// Shows a popup with the specified options.
	/// </summary>
	/// <param name="page">The current page that provides access to the <see cref="INavigation"/> implementation.</param>
	/// <param name="view">The <see cref="View"/> that will be displayed as the content in the popup.</param>
	/// <param name="options">The <see cref="IPopupOptions"/> that enable support for customizing the display and behavior of the presented popup.</param>
	/// <param name="token">A <see cref="CancellationToken"/> providing support for canceling the wait for a result to be returned. This will <b>not</b> close the popup.</param>
	/// <returns>An <see cref="IPopupResult"/> when the popup is closed or the <paramref name="token"/> is canceled. Make sure to check the <see cref="IPopupResult.WasDismissedByTappingOutsideOfPopup"/> value to determine how the popup was closed.</returns>
	public static Task<IPopupResult<TResult>> ShowPopupAsync<TResult>(this Page page, View view, IPopupOptions? options = null, CancellationToken token = default)
	{
		ArgumentNullException.ThrowIfNull(page);

		return page.Navigation.ShowPopupAsync<TResult>(view, options, token);
	}
	
	/// <summary>
	/// Shows a popup with the specified options.
	/// </summary>
	/// <param name="navigation">The <see cref="INavigation"/> implementation responsible for displaying the popup. Make sure to use the one associated with the <see cref="Window"/> that you wish the popup to be displayed on.</param>
	/// <param name="view">The <see cref="View"/> that will be displayed as the content in the popup.</param>
	/// <param name="options">The <see cref="IPopupOptions"/> that enable support for customizing the display and behavior of the presented popup.</param>
	/// <param name="token">A <see cref="CancellationToken"/> providing support for canceling the wait for a result to be returned. This will <b>not</b> close the popup.</param>
	/// <returns>An <see cref="IShowPopupResult{TResult}"/> when the popup is closed or the <paramref name="token"/> is canceled. Make sure to check the <see cref="IPopupResult.WasDismissedByTappingOutsideOfPopup"/> value to determine how the popup was closed.</returns>
	public static async Task<IShowPopupResult<TResult>> TryShowPopupAsync<TResult>(this INavigation navigation, View view, IPopupOptions? options = null, CancellationToken token = default)
	{
		ArgumentNullException.ThrowIfNull(navigation);
		ArgumentNullException.ThrowIfNull(view);

		try
		{
			var popupResult = await navigation.ShowPopupAsync<TResult>(view, options, token);
			return new ShowPopupResult<TResult>(popupResult, null);
		}
		catch (Exception e)
		{
			return new ShowPopupResult<TResult>(default(TResult), false, e);
		}
	}

	/// <summary>
	/// Shows a popup with the specified options.
	/// </summary>
	/// <param name="navigation">The <see cref="INavigation"/> implementation responsible for displaying the popup. Make sure to use the one associated with the <see cref="Window"/> that you wish the popup to be displayed on.</param>
	/// <param name="view">The <see cref="View"/> that will be displayed as the content in the popup.</param>
	/// <param name="options">The <see cref="IPopupOptions"/> that enable support for customizing the display and behavior of the presented popup.</param>
	/// <param name="token">A <see cref="CancellationToken"/> providing support for canceling the wait for a result to be returned. This will <b>not</b> close the popup.</param>
	/// <returns>An <see cref="IPopupResult{TResult}"/> when the popup is closed or the <paramref name="token"/> is canceled. Make sure to check the <see cref="IPopupResult.WasDismissedByTappingOutsideOfPopup"/> value to determine how the popup was closed.</returns>
	public static async Task<IPopupResult<TResult>> ShowPopupAsync<TResult>(this INavigation navigation, View view, IPopupOptions? options = null, CancellationToken token = default)
	{
		var result = await navigation.ShowPopupAsync(view, options, token);

		return GetPopupResult<TResult>(result);
	}
	
	/// <summary>
	/// Shows a popup with the specified options.
	/// </summary>
	/// <param name="shell">Current <see cref="Shell"/></param>
	/// <param name="view">The <see cref="View"/> that will be displayed as the content in the popup.</param>
	/// <param name="options">The <see cref="IPopupOptions"/> that enable support for customizing the display and behavior of the presented popup.</param>
	/// <param name="shellParameters">Parameters that will be passed into the <paramref name="view"/> or its associated BindingContext if they implement <see cref="IQueryAttributable"/>.</param>
	/// <param name="token">A <see cref="CancellationToken"/> providing support for canceling the wait for a result to be returned. This will <b>not</b> close the popup.</param>
	/// <returns>An <see cref="IShowPopupResult{TResult}"/> when the popup is closed or the <paramref name="token"/> is canceled. Make sure to check the <see cref="IPopupResult.WasDismissedByTappingOutsideOfPopup"/> value to determine how the popup was closed.</returns>
	public static async Task<IShowPopupResult<TResult>> TryShowPopupAsync<TResult>(this Shell shell, View view, IPopupOptions? options = null, IDictionary<string, object>? shellParameters = null, CancellationToken token = default)
	{
		ArgumentNullException.ThrowIfNull(shell);
		ArgumentNullException.ThrowIfNull(view);

		try
		{
			var popupResult = await shell.ShowPopupAsync<TResult>(view, options, shellParameters, token);
			return new ShowPopupResult<TResult>(popupResult, null);
		}
		catch (Exception e)
		{
			return new ShowPopupResult<TResult>(default(TResult), false, e);
		}
	}

	/// <summary>
	/// Shows a popup with the specified options.
	/// </summary>
	/// <param name="shell">Current <see cref="Shell"/></param>
	/// <param name="view">The <see cref="View"/> that will be displayed as the content in the popup.</param>
	/// <param name="options">The <see cref="IPopupOptions"/> that enable support for customizing the display and behavior of the presented popup.</param>
	/// <param name="shellParameters">Parameters that will be passed into the <paramref name="view"/> or its associated BindingContext if they implement <see cref="IQueryAttributable"/>.</param>
	/// <param name="token">A <see cref="CancellationToken"/> providing support for canceling the wait for a result to be returned. This will <b>not</b> close the popup.</param>
	/// <returns>An <see cref="IPopupResult{TResult}"/> when the popup is closed or the <paramref name="token"/> is canceled. Make sure to check the <see cref="IPopupResult.WasDismissedByTappingOutsideOfPopup"/> value to determine how the popup was closed.</returns>
	public static async Task<IPopupResult<TResult>> ShowPopupAsync<TResult>(this Shell shell, View view, IPopupOptions? options = null, IDictionary<string, object>? shellParameters = null, CancellationToken token = default)
	{
		var result = await shell.ShowPopupAsync(view, options, shellParameters, token);

		return GetPopupResult<TResult>(result);
	}

	/// <summary>
	/// Shows a popup with the specified options.
	/// </summary>
	/// <param name="page">The current page that provides access to the <see cref="INavigation"/> implementation.</param>
	/// <param name="view">The <see cref="View"/> that will be displayed as the content in the popup.</param>
	/// <param name="options">The <see cref="IPopupOptions"/> that enable support for customizing the display and behavior of the presented popup.</param>
	/// <param name="token">A <see cref="CancellationToken"/> providing support for canceling the wait for a result to be returned. This will <b>not</b> close the popup.</param>
	/// <returns>An <see cref="IShowPopupResult"/> when the popup is closed or the <paramref name="token"/> is canceled. Make sure to check the <see cref="IPopupResult.WasDismissedByTappingOutsideOfPopup"/> value to determine how the popup was closed.</returns>
	public static Task<IShowPopupResult> TryShowPopupAsync(this Page page, View view, IPopupOptions? options = null, CancellationToken token = default)
	{
		ArgumentNullException.ThrowIfNull(page);
		
		return page.Navigation.TryShowPopupAsync(view, options, token);
	}

	/// <summary>
	/// Shows a popup with the specified options.
	/// </summary>
	/// <param name="page">The current page that provides access to the <see cref="INavigation"/> implementation.</param>
	/// <param name="view">The <see cref="View"/> that will be displayed as the content in the popup.</param>
	/// <param name="options">The <see cref="IPopupOptions"/> that enable support for customizing the display and behavior of the presented popup.</param>
	/// <param name="token">A <see cref="CancellationToken"/> providing support for canceling the wait for a result to be returned. This will <b>not</b> close the popup.</param>
	/// <returns>An <see cref="IPopupResult"/> when the popup is closed or the <paramref name="token"/> is canceled. Make sure to check the <see cref="IPopupResult.WasDismissedByTappingOutsideOfPopup"/> value to determine how the popup was closed.</returns>
	public static Task<IPopupResult> ShowPopupAsync(this Page page, View view, IPopupOptions? options = null, CancellationToken token = default)
	{
		ArgumentNullException.ThrowIfNull(page);

		return page.Navigation.ShowPopupAsync(view, options, token);
	}

	/// <summary>
	/// Shows a popup with the specified options.
	/// </summary>
	/// <param name="navigation">The <see cref="INavigation"/> implementation responsible for displaying the popup. Make sure to use the one associated with the <see cref="Window"/> that you wish the popup to be displayed on.</param>
	/// <param name="view">The <see cref="View"/> that will be displayed as the content in the popup.</param>
	/// <param name="options">The <see cref="IPopupOptions"/> that enable support for customizing the display and behavior of the presented popup.</param>
	/// <param name="token">A <see cref="CancellationToken"/> providing support for canceling the wait for a result to be returned. This will <b>not</b> close the popup.</param>
	/// <returns>An <see cref="IPopupResult"/> when the popup is closed or the <paramref name="token"/> is canceled. Make sure to check the <see cref="IPopupResult.WasDismissedByTappingOutsideOfPopup"/> value to determine how the popup was closed.</returns>
	public static async Task<IShowPopupResult> TryShowPopupAsync(this INavigation navigation, View view, IPopupOptions? options, CancellationToken token = default)
	{
		ArgumentNullException.ThrowIfNull(navigation);
		ArgumentNullException.ThrowIfNull(view);

		try
		{
			var popupResult = await navigation.ShowPopupAsync(view, options, token);
			return new ShowPopupResult(popupResult.WasDismissedByTappingOutsideOfPopup, null);
		}
		catch (Exception e)
		{
			return new ShowPopupResult(false, e);
		}
	}

	/// <summary>
	/// Shows a popup with the specified options.
	/// </summary>
	/// <param name="navigation">The <see cref="INavigation"/> implementation responsible for displaying the popup. Make sure to use the one associated with the <see cref="Window"/> that you wish the popup to be displayed on.</param>
	/// <param name="view">The <see cref="View"/> that will be displayed as the content in the popup.</param>
	/// <param name="options">The <see cref="IPopupOptions"/> that enable support for customizing the display and behavior of the presented popup.</param>
	/// <param name="token">A <see cref="CancellationToken"/> providing support for canceling the wait for a result to be returned. This will <b>not</b> close the popup.</param>
	/// <returns>An <see cref="IPopupResult"/> when the popup is closed or the <paramref name="token"/> is canceled. Make sure to check the <see cref="IPopupResult.WasDismissedByTappingOutsideOfPopup"/> value to determine how the popup was closed.</returns>
	public static async Task<IPopupResult> ShowPopupAsync(this INavigation navigation, View view, IPopupOptions? options, CancellationToken token = default)
	{
		ArgumentNullException.ThrowIfNull(navigation);
		ArgumentNullException.ThrowIfNull(view);

		token.ThrowIfCancellationRequested();

		TaskCompletionSource<IPopupResult> taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

		var popupPage = new PopupPage(view, options);
		popupPage.PopupClosed += HandlePopupClosed;

		await popupPage.ShowAsync(navigation, token);

		return await taskCompletionSource.Task.WaitAsync(token);

		void HandlePopupClosed(object? sender, IPopupResult e)
		{
			popupPage.PopupClosed -= HandlePopupClosed;
			taskCompletionSource.SetResult(e);
		}
	}

	/// <summary>
	/// Shows a popup with the specified options.
	/// </summary>
	/// <param name="shell">Current <see cref="Shell"/></param>
	/// <param name="view">The <see cref="View"/> that will be displayed as the content in the popup.</param>
	/// <param name="options">The <see cref="IPopupOptions"/> that enable support for customizing the display and behavior of the presented popup.</param>
	/// <param name="shellParameters">Parameters that will be passed into the <paramref name="view"/> or its associated BindingContext if they implement <see cref="IQueryAttributable"/>.</param>
	/// <param name="token">A <see cref="CancellationToken"/> providing support for canceling the wait for a result to be returned. This will <b>not</b> close the popup.</param>
	/// <returns>An <see cref="IShowPopupResult"/> when the popup is closed or the <paramref name="token"/> is cancelled. Make sure to check the <see cref="IPopupResult.WasDismissedByTappingOutsideOfPopup"/> value to determine how the popup was closed.</returns>
	public static async Task<IShowPopupResult> TryShowPopupAsync(this Shell shell, View view, IPopupOptions? options, IDictionary<string, object>? shellParameters = null, CancellationToken token = default)
	{
		ArgumentNullException.ThrowIfNull(shell);
		ArgumentNullException.ThrowIfNull(view);

		try
		{
			var popupResult = await shell.ShowPopupAsync(view, options, shellParameters, token);
			return new ShowPopupResult(popupResult.WasDismissedByTappingOutsideOfPopup, null);
		}
		catch (Exception e)
		{
			return new ShowPopupResult(false, e);
		}
	}

	/// <summary>
	/// Shows a popup with the specified options.
	/// </summary>
	/// <param name="shell">Current <see cref="Shell"/></param>
	/// <param name="view">The <see cref="View"/> that will be displayed as the content in the popup.</param>
	/// <param name="options">The <see cref="IPopupOptions"/> that enable support for customizing the display and behavior of the presented popup.</param>
	/// <param name="shellParameters">Parameters that will be passed into the <paramref name="view"/> or its associated BindingContext if they implement <see cref="IQueryAttributable"/>.</param>
	/// <param name="token">A <see cref="CancellationToken"/> providing support for canceling the wait for a result to be returned. This will <b>not</b> close the popup.</param>
	/// <returns>An <see cref="IPopupResult"/> when the popup is closed or the <paramref name="token"/> is cancelled. Make sure to check the <see cref="IPopupResult.WasDismissedByTappingOutsideOfPopup"/> value to determine how the popup was closed.</returns>
	public static async Task<IPopupResult> ShowPopupAsync(this Shell shell, View view, IPopupOptions? options, IDictionary<string, object>? shellParameters = null, CancellationToken token = default)
	{
		ArgumentNullException.ThrowIfNull(shell);
		ArgumentNullException.ThrowIfNull(view);

		token.ThrowIfCancellationRequested();

		var popupPageRoute = $"{nameof(PopupPage)}" + Guid.NewGuid(); // Add a GUID to the PopupPage Route to avoid duplicate Routes being added to Shell Routing

		TaskCompletionSource<IPopupResult> taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

		var popupPage = new PopupPage(view, options);
		popupPage.PopupClosed += HandlePopupClosed;

		Routing.RegisterRoute(popupPageRoute, new PopupPageRouteFactory(popupPage));

		try
		{
			await popupPage.ShowAsync(shell, popupPageRoute, shellParameters, token);
			return await taskCompletionSource.Task.WaitAsync(token);
		}
		finally
		{
			Routing.UnRegisterRoute(popupPageRoute);
		}

		void HandlePopupClosed(object? sender, IPopupResult e)
		{
			popupPage.PopupClosed -= HandlePopupClosed;
			taskCompletionSource.SetResult(e);
		}
	}

	/// <summary>
	/// Closes the most recent popup and returns an <see cref="IPopupResult"/> that provides details about the closure.
	/// </summary>
	public static Task<IClosePopupResult> TryClosePopupAsync(this Page page, CancellationToken token = default)
	{
		ArgumentNullException.ThrowIfNull(page);

		return page.Navigation.TryClosePopupAsync(token);
	}

	/// <summary>
	/// Closes the most recent popup and returns an <see cref="IPopupResult"/> that provides details about the closure.
	/// </summary>
	public static Task<IPopupResult> ClosePopupAsync(this Page page, CancellationToken token = default)
	{
		ArgumentNullException.ThrowIfNull(page);

		return page.Navigation.ClosePopupAsync(token);
	}
	
	/// <summary>
	/// Closes the most recent popup and returns an <see cref="IClosePopupResult"/> that provides details about the closure.
	/// </summary>
	public static async Task<IClosePopupResult> TryClosePopupAsync(this INavigation navigation, CancellationToken token = default)
	{
		ArgumentNullException.ThrowIfNull(navigation);

		try
		{
			var popupResult = await navigation.ClosePopupAsync(token);
			return new ClosePopupResult(popupResult.WasDismissedByTappingOutsideOfPopup, null);
		}
		catch (Exception e)
		{
			return new ClosePopupResult(false, e);
		}
	}

	/// <summary>
	/// Closes the most recent popup and returns an <see cref="IPopupResult"/> that provides details about the closure.
	/// </summary>
	public static async Task<IPopupResult> ClosePopupAsync(this INavigation navigation, CancellationToken token = default)
	{
		ArgumentNullException.ThrowIfNull(navigation);
		token.ThrowIfCancellationRequested();

		var popupClosedTCS = new TaskCompletionSource<IPopupResult>();

		var popupPage = GetMostRecentPopupPage(navigation);

		popupPage.PopupClosed += HandlePopupPageClosed;
		await popupPage.CloseAsync(new PopupResult(false), token);

		var popupResult = await popupClosedTCS.Task;
		return popupResult;

		void HandlePopupPageClosed(object? sender, IPopupResult e)
		{
			popupPage.PopupClosed -= HandlePopupPageClosed;
			popupClosedTCS.SetResult(e);
		}
	}
	
	/// <summary>
	/// Closes the most recent popup and returns an <see cref="IClosePopupResult{TResult}"/> that provides details about the closure.
	/// </summary>
	public static Task<IClosePopupResult<TResult>> TryClosePopupAsync<TResult>(this Page page, TResult result, CancellationToken token = default)
	{
		ArgumentNullException.ThrowIfNull(page);

		return page.Navigation.TryClosePopupAsync(result, token);
	}

	/// <summary>
	/// Closes the most recent popup and returns an <see cref="IPopupResult{TResult}"/> that provides details about the closure.
	/// </summary>
	public static Task<IPopupResult<TResult>> ClosePopupAsync<TResult>(this Page page, TResult result, CancellationToken token = default)
	{
		ArgumentNullException.ThrowIfNull(page);

		return page.Navigation.ClosePopupAsync(result, token);
	}

	/// <summary>
	/// Closes the most recent popup and returns an <see cref="IClosePopupResult{TResult}"/> that provides details about the closure.
	/// </summary>
	public static async Task<IClosePopupResult<TResult>> TryClosePopupAsync<TResult>(this INavigation navigation, TResult result, CancellationToken token = default)
	{
		ArgumentNullException.ThrowIfNull(navigation);

		try
		{
			var popupResult = await navigation.ClosePopupAsync(result, token);
			return new ClosePopupResult<TResult>(popupResult, null);
		}
		catch (Exception e)
		{
			return new ClosePopupResult<TResult>(result, false, e);
		}
	}

	/// <summary>
	/// Closes the most recent popup and returns an <see cref="IPopupResult{TResult}"/> that provides details about the closure.
	/// </summary>
	public static async Task<IPopupResult<TResult>> ClosePopupAsync<TResult>(this INavigation navigation, TResult result, CancellationToken token = default)
	{
		ArgumentNullException.ThrowIfNull(navigation);
		token.ThrowIfCancellationRequested();

		var popupClosedTCS = new TaskCompletionSource<IPopupResult>();

		var popupPage = GetMostRecentPopupPage(navigation);

		popupPage.PopupClosed += HandlePopupPageClosed;

		await popupPage.CloseAsync(new PopupResult<TResult>(result, false), token);

		var popupResult = await popupClosedTCS.Task;
		return GetPopupResult<TResult>(popupResult);

		void HandlePopupPageClosed(object? sender, IPopupResult e)
		{
			popupPage.PopupClosed -= HandlePopupPageClosed;
			popupClosedTCS.SetResult(e);
		}
	}

	static PopupPage GetMostRecentPopupPage(in INavigation navigation)
	{
		var currentVisibleModalPage = Shell.Current is null
			? navigation.ModalStack.LastOrDefault()
			: Shell.Current.Navigation.ModalStack.LastOrDefault();

		if (currentVisibleModalPage is null)
		{
			throw new PopupNotFoundException();
		}

		if (currentVisibleModalPage is not PopupPage popupPage)
		{
			throw new PopupBlockedException(currentVisibleModalPage);
		}

		return popupPage;
	}

	static PopupResult<T> GetPopupResult<T>(in IPopupResult result)
	{
		return result switch
		{
			PopupResult<T> popupResult => popupResult,
			IPopupResult => new PopupResult<T>(null, result.WasDismissedByTappingOutsideOfPopup),
			_ => throw new NotSupportedException($"PopupResult type {typeof(T)} is not supported")
		};
	}

	sealed class PopupPageRouteFactory(in PopupPage popupPage) : RouteFactory
	{
		readonly PopupPage popupPage = popupPage;

		public override Element GetOrCreate() => popupPage;

		public override Element GetOrCreate(IServiceProvider services) => popupPage;
	}
}