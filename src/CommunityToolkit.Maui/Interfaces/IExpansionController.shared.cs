namespace CommunityToolkit.Maui;

/// <summary>
/// Defines a pluggable controller responsible for handling expansion
/// and collapse transitions for an <see cref="Views.Expander"/>.
/// </summary>
public interface IExpansionController
{
	/// <summary>
	/// Executes asynchronous logic when the expander transitions
	/// from a collapsed to an expanded state.
	/// </summary>
	/// <param name="expander">The <see cref="Views.Expander"/> instance initiating the expansion.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	Task OnExpandingAsync(Views.Expander expander);

	/// <summary>
	/// Executes asynchronous logic when the expander transitions
	/// from an expanded to a collapsed state.
	/// </summary>
	/// <param name="expander">The <see cref="Views.Expander"/> instance initiating the collapse.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	Task OnCollapsingAsync(Views.Expander expander);
}
