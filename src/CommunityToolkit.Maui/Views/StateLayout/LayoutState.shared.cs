namespace CommunityToolkit.Maui.UI.Views;

/// <summary>
/// State used by the StateToBooleanConverter 
/// </summary>
public enum LayoutState
{
	/// <summary>
	/// Default; will show the initial view
	/// </summary>
	None,

	/// <summary>
	/// Layout is Loading
	/// </summary>
	Loading,

	/// <summary>
	/// Layout is Saving
	/// </summary>
	Saving,

	/// <summary>
	/// Layout Success
	/// </summary>
	Success,

	/// <summary>
	/// Layout Error
	/// </summary>
	Error,

	/// <summary>
	/// Layout Empty
	/// </summary>
	Empty,

	/// <summary>
	/// Layout Custom
	/// </summary>
	Custom
}