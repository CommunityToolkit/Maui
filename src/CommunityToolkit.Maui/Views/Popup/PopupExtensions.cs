using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Views;

/// <summary>
/// 
/// </summary>
public static class PopupExtensions
{
	/// <summary>
	/// 
	/// </summary>
	/// <param name="navigation"></param>
	/// <param name="view"></param>
	/// <param name="options"></param>
	/// <returns></returns>
	public static async Task<PopupResult<TResult>> ShowPopup<TResult>(this INavigation navigation, View view, PopupOptions options)
	{
		TaskCompletionSource<PopupResult<TResult>> taskCompletionSource = new();

		var popupContent = BuildPopupContent(view, options);
		var popupContainer = BuildPopupContainer(view, taskCompletionSource);
		ConfigurePopupContainer(popupContainer, popupContent, options);

		var popupLifecycleController = IPlatformApplication.Current?.Services.GetRequiredService<PopupLifecycleController>();
		popupLifecycleController?.RegisterPopup(popupContainer);

		await navigation.PushModalAsync(popupContainer);
		return await taskCompletionSource.Task;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="navigation"></param>
	/// <param name="view"></param>
	/// <param name="options"></param>
	/// <returns></returns>
	public static async Task<PopupResult> ShowPopup(this INavigation navigation, View view, PopupOptions options)
	{
		TaskCompletionSource<PopupResult> taskCompletionSource = new();

		var popupContent = BuildPopupContent(view, options);
		var popupContainer = BuildPopupContainer(view, taskCompletionSource);
		ConfigurePopupContainer(popupContainer, popupContent, options);

		var popupLifecycleController = IPlatformApplication.Current?.Services.GetRequiredService<PopupLifecycleController>();
		popupLifecycleController?.RegisterPopup(popupContainer);

		await navigation.PushModalAsync(popupContainer);
		return await taskCompletionSource.Task;
	}

	static PopupContainer<TResult> BuildPopupContainer<TResult>(View view, TaskCompletionSource<PopupResult<TResult>> taskCompletionSource)
	{
		return new PopupContainer<TResult>(view as Popup<TResult> ?? new Popup<TResult>() { Content = view }, taskCompletionSource);
	}

	static PopupContainer BuildPopupContainer(View view, TaskCompletionSource<PopupResult> taskCompletionSource)
	{
		return new PopupContainer(view as Popup ?? new Popup() { Content = view }, taskCompletionSource);
	}

	static void ConfigurePopupContainer(PopupContainer popupContainer, View popupContent, PopupOptions options)
	{
		popupContainer.BackgroundColor = options.BackgroundColor ?? Color.FromRgba(0, 0, 0, 0.4); // https://rgbacolorpicker.com/rgba-to-hex,
		popupContainer.CanBeDismissedByTappingOutsideOfPopup = options.CanBeDismissedByTappingOutsideOfPopup;
		popupContainer.Content = popupContent;
		popupContainer.BindingContext = popupContent.BindingContext;

		if (options.CanBeDismissedByTappingOutsideOfPopup)
		{
			popupContent.GestureRecognizers.Add(new TapGestureRecognizer()
			{
				Command = new Command(async () =>
				{
					options.OnTappingOutsideOfPopup?.Invoke();
					await popupContainer.Close(new PopupResult(true));
				})
			});
		}
	}

	static View BuildPopupContent(View popup, PopupOptions options)
	{
		var view = new Grid()
		{
			BackgroundColor = null
		};
		view.Children.Add(new Border()
		{
			Content = popup,
			Background = popup.Background,
			BackgroundColor = popup.BackgroundColor,
			WidthRequest = popup.WidthRequest,
			HeightRequest = popup.HeightRequest,
			VerticalOptions = options.VerticalOptions,
			HorizontalOptions = options.HorizontalOptions,
			StrokeShape = options.Shape,
			Margin = 30,
			Padding = 15
		});
		view.BindingContext = popup.BindingContext;

		return view;
	}
}