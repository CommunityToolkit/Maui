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

		var popupContent = BuildPopupContent(view, options);
		var popupContainer = BuildPopupContainer(view, taskCompletionSource);
		ConfigurePopupContainer(popupContainer, popupContent, options);

		var popupLifecycleController = IPlatformApplication.Current?.Services.GetRequiredService<PopupLifecycleController>();
		popupLifecycleController?.RegisterPopup(popupContainer);

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

		var popupContent = BuildPopupContent(view, options);
		var popupContainer = BuildPopupContainer(view, taskCompletionSource);
		ConfigurePopupContainer(popupContainer, popupContent, options);

		var popupLifecycleController = IPlatformApplication.Current?.Services.GetRequiredService<PopupLifecycleController>();
		popupLifecycleController?.RegisterPopup(popupContainer);

		await navigation.PushModalAsync(popupContainer, false).WaitAsync(token);
		return await taskCompletionSource.Task.WaitAsync(token);
	}

	static PopupContainer<TResult> BuildPopupContainer<TResult>(View view, TaskCompletionSource<PopupResult<TResult>> taskCompletionSource)
	{
		return new PopupContainer<TResult>(view as Popup<TResult> ?? new Popup<TResult> { Content = view }, taskCompletionSource);
	}

	static PopupContainer BuildPopupContainer(View view, TaskCompletionSource<PopupResult> taskCompletionSource)
	{
		return new PopupContainer(view as Popup ?? new Popup { Content = view }, taskCompletionSource);
	}

	static void ConfigurePopupContainer(PopupContainer popupContainer, View popupContent, PopupOptions options)
	{
		popupContainer.BackgroundColor = options.BackgroundColor;
		popupContainer.CanBeDismissedByTappingOutsideOfPopup = options.CanBeDismissedByTappingOutsideOfPopup;
		popupContainer.Content = popupContent;
		popupContainer.BindingContext = popupContent.BindingContext;

		if (options.CanBeDismissedByTappingOutsideOfPopup)
		{
			popupContent.GestureRecognizers.Add(new TapGestureRecognizer
			{
				Command = new Command(async () =>
				{
					options.OnTappingOutsideOfPopup?.Invoke();
					await popupContainer.Close(new PopupResult(true));
				})
			});
		}
	}

	static Grid BuildPopupContent(View popup, PopupOptions options)
	{
		var view = new Grid
		{
			BackgroundColor = null
		};
		
		view.Children.Add(new Border
		{
			Content = popup,
			Background = popup.Background,
			BackgroundColor = popup.BackgroundColor,
			VerticalOptions = options.VerticalOptions,
			HorizontalOptions = options.HorizontalOptions,
			StrokeShape = options.Shape,
			Margin = options.Margin,
			Padding = options.Padding
		});
		
		view.BindingContext = popup.BindingContext;

		return view;
	}
}