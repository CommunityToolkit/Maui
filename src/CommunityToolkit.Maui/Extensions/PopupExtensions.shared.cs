using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Extensions;

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
	/// <param name="options"><see cref="IPopupOptions"/></param>
	/// <remarks>This is an <see langword="async"/> <see langword="void"/> method. Use <see cref="ShowPopupAsync(Page,View,CommunityToolkit.Maui.IPopupOptions?,CancellationToken)"/> to <see langword="await"/> this method and return <see cref="PopupResult{T}"/> </remarks>
	public static void ShowPopup(this Page page, View view, IPopupOptions? options = null)
	{
		ArgumentNullException.ThrowIfNull(page);

		ShowPopup(page.Navigation, view, options);
	}

	/// <summary>
	/// Shows a popup with the specified options.
	/// </summary>
	/// <param name="navigation">Popup parent</param>
	/// <param name="view">Popup content</param>
	/// <param name="options"><see cref="IPopupOptions"/></param>
	/// <remarks>This is an <see langword="async"/> <see langword="void"/> method. Use <see cref="ShowPopupAsync(Page,View,CommunityToolkit.Maui.IPopupOptions?,CancellationToken)"/> to <see langword="await"/> this method</remarks>
	public static async void ShowPopup(this INavigation navigation, View view, IPopupOptions? options = null)
	{
		ArgumentNullException.ThrowIfNull(navigation);
		ArgumentNullException.ThrowIfNull(view);

		var popupPage = new PopupPage(view, options ?? PopupOptions.Empty);

		await navigation.PushModalAsync(popupPage, false);
	}
	
	/// <summary>
	/// Shows a popup with the specified options.
	/// </summary>
	/// <param name="shell">Current <see cref="Shell"/></param>
	/// <param name="view">Popup content</param>
	/// <param name="options"><see cref="IPopupOptions"/></param>
	/// <param name="shellParameters">Parameters used by <see cref="IQueryAttributable"/></param>
	/// <remarks>This is an <see langword="async"/> <see langword="void"/> method. Use <see cref="ShowPopupAsync(Page,View,CommunityToolkit.Maui.IPopupOptions?,CancellationToken)"/> to <see langword="await"/> this method</remarks>
	public static async void ShowPopup(this Shell shell, View view, IPopupOptions? options = null, IDictionary<string, object>? shellParameters = null)
	{
		ArgumentNullException.ThrowIfNull(shell);
		ArgumentNullException.ThrowIfNull(view);

		await shell.ShowPopupAsync(view, options, shellParameters);
	}

	/// <summary>
	/// Opens a popup with the specified options.
	/// </summary>
	/// <param name="page">Current page</param>
	/// <param name="view">Popup content</param>
	/// <param name="options"><see cref="IPopupOptions"/></param>
	/// <param name="token"><see cref="CancellationToken"/></param>
	/// <returns><see cref="IPopupResult"/></returns>
	public static Task<IPopupResult<TResult>> ShowPopupAsync<TResult>(this Page page, View view, IPopupOptions? options = null, CancellationToken token = default)
	{
		ArgumentNullException.ThrowIfNull(page);

		return page.Navigation.ShowPopupAsync<TResult>(view, options, token);
	}

	/// <summary>
	/// Opens a popup with the specified options.
	/// </summary>
	/// <param name="navigation">Popup parent</param>
	/// <param name="view">Popup content</param>
	/// <param name="options"><see cref="IPopupOptions"/></param>
	/// <param name="token"><see cref="CancellationToken"/></param>
	/// <returns><see cref="IPopupResult{T}"/></returns>
	public static async Task<IPopupResult<TResult>> ShowPopupAsync<TResult>(this INavigation navigation, View view, IPopupOptions? options = null, CancellationToken token = default)
	{
		var result = await ShowPopupAsync(navigation, view, options, token);

		return GetPopupResult<TResult>(result);
	}
	
	/// <summary>
	/// Opens a popup with the specified options.
	/// </summary>
	/// <param name="shell">Current <see cref="Shell"/></param>
	/// <param name="view">Popup content</param>
	/// <param name="options"><see cref="IPopupOptions"/></param>
	/// <param name="shellParameters">Parameters used by <see cref="IQueryAttributable"/></param>
	/// <param name="token"><see cref="CancellationToken"/></param>
	/// <returns><see cref="IPopupResult{T}"/></returns>
	public static async Task<IPopupResult<TResult>> ShowPopupAsync<TResult>(this Shell shell, View view, IPopupOptions? options = null, IDictionary<string, object>? shellParameters = null, CancellationToken token = default)
	{
		var result = await ShowPopupAsync(shell, view, options, shellParameters, token);
		
		return GetPopupResult<TResult>(result);
	}

	/// <summary>
	/// Shows a popup with the specified options.
	/// </summary>
	/// <param name="page">Current page</param>
	/// <param name="view">Popup content</param>
	/// <param name="options"><see cref="IPopupOptions"/></param>
	/// <param name="token"><see cref="CancellationToken"/></param>
	/// <returns><see cref="IPopupResult"/></returns>
	public static Task<IPopupResult> ShowPopupAsync(this Page page, View view, IPopupOptions? options = null, CancellationToken token = default)
	{
		ArgumentNullException.ThrowIfNull(page);

		return ShowPopupAsync(page.Navigation, view, options, token);
	}

	/// <summary>
	/// Shows a popup with the specified options.
	/// </summary>
	/// <param name="navigation">Popup parent</param>
	/// <param name="view">Popup content</param>
	/// <param name="options"><see cref="IPopupOptions"/></param>
	/// <param name="token"><see cref="CancellationToken"/></param>
	/// <returns><see cref="IPopupResult"/></returns>
	public static async Task<IPopupResult> ShowPopupAsync(this INavigation navigation, View view, IPopupOptions? options, CancellationToken token = default)
	{
		ArgumentNullException.ThrowIfNull(navigation);
		ArgumentNullException.ThrowIfNull(view);

		token.ThrowIfCancellationRequested();

		TaskCompletionSource<IPopupResult> taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

		var popupPage = new PopupPage(view, options ?? PopupOptions.Empty);
		popupPage.PopupClosed += HandlePopupClosed;

		await navigation.PushModalAsync(popupPage, false).WaitAsync(token);
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
	/// <param name="view">Popup content</param>
	/// <param name="options"><see cref="IPopupOptions"/></param>
	/// <param name="shellParameters">Parameters used by <see cref="IQueryAttributable"/></param>
	/// <param name="token"><see cref="CancellationToken"/></param>
	/// <returns><see cref="IPopupResult"/></returns>
	public static async Task<IPopupResult> ShowPopupAsync(this Shell shell, View view, IPopupOptions? options, IDictionary<string, object>? shellParameters = null, CancellationToken token = default)
	{
		ArgumentNullException.ThrowIfNull(shell);
		ArgumentNullException.ThrowIfNull(view);

		token.ThrowIfCancellationRequested();

		var popupPageRoute = $"{nameof(PopupPage)}" + Guid.NewGuid(); // Add a GUID to the PopupPage Route to avoid duplicate Routes being added to Shell Routing

		TaskCompletionSource<IPopupResult> taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

		var popupPage = new PopupPage(view, options ?? PopupOptions.Empty);
		popupPage.PopupClosed += HandlePopupClosed;

		Routing.RegisterRoute(popupPageRoute, new PopupPageRouteFactory(popupPage));
		
		try
		{
			await shell.GoToAsync(popupPageRoute, shellParameters);
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