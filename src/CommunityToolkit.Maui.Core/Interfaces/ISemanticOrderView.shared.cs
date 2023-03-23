namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Allows users to specify the semantic order of child views.
/// </summary>
public interface ISemanticOrderView : IContentView
{
	/// <summary>
	/// The semantic order to traverse child views
	/// </summary>
	IEnumerable<IView> ViewOrder { get; }
}