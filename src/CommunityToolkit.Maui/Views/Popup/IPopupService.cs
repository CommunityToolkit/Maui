namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Provides a mechanism for displaying Popups based on the underlying view model.
/// </summary>
public interface IPopupService
{
	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="TBindingContext"></typeparam>
	/// <returns></returns>
	Task<PopupResult> ShowPopupAsync<TBindingContext>(PopupOptions<TBindingContext> options, CancellationToken cancellationToken)
		where TBindingContext : notnull;

	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="TBindingContext"></typeparam>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	Task<PopupResult<T>> ShowPopupAsync<TBindingContext, T>(PopupOptions<TBindingContext> options, CancellationToken cancellationToken)
		where TBindingContext : notnull;

	/// <summary>
	/// 
	/// </summary>
	Task ClosePopup();
	/// <summary>
	/// 
	/// </summary>
	Task ClosePopup<T>(T result);
}