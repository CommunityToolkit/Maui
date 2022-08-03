using System.Collections;

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// TBD
/// </summary>
public interface ISemanticOrderView : IView
{
	/// <summary>
	/// TBD
	/// </summary>
	public IEnumerable ViewOrder { get; set; }
}
