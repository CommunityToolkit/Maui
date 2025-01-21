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
	Task<PopupResult> ShowPopupAsync<TBindingContext>(INavigation navigation, PopupOptions options, CancellationToken cancellationToken = default)
		where TBindingContext : notnull;

	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="TBindingContext"></typeparam>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	Task<PopupResult<T>> ShowPopupAsync<TBindingContext, T>(INavigation navigation, PopupOptions options, CancellationToken cancellationToken = default)
		where TBindingContext : notnull;

	/// <summary>
	/// 
	/// </summary>
	/// <param name="cancellationToken"></param>
	Task ClosePopupAsync(CancellationToken cancellationToken = default);
	/// <summary>
	/// 
	/// </summary>
	Task ClosePopupAsync<T>(T result, CancellationToken cancellationToken = default);
}