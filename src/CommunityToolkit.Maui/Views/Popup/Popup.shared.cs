using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Views;

/// <inheritdoc/>
public abstract class Popup : BasePopup
{
	TaskCompletionSource<object?> taskCompletionSource = new();

	/// <summary>
	/// Gets the final result of the dismissed popup.
	/// </summary>
	public Task<object?> Result => taskCompletionSource.Task;

	/// <summary>
	/// Resets the Popup.
	/// </summary>
	public void Reset() => taskCompletionSource = new();

	/// <summary>
	/// Dismiss the current popup.
	/// </summary>
	/// <param name="result">
	/// The result to return.
	/// </param>
	public void Dismiss(object? result)
	{
		taskCompletionSource.TrySetResult(result);
		OnDismissed(result);
	}

	/// <inheritdoc/>
	protected internal override void LightDismiss()
	{
		taskCompletionSource.TrySetResult(GetLightDismissResult());
		base.LightDismiss();
	}

	/// <summary>
	/// Gets the light dismiss default result.
	/// </summary>
	/// <returns>
	/// The light dismiss value.
	/// </returns>
	/// <remarks>
	/// When a user dismisses the Popup via the light dismiss, this
	/// method will return a default value.
	/// </remarks>
	protected virtual object? GetLightDismissResult() => default;
}
