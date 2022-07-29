using System.Collections;

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// TBD
/// </summary>
public interface ISemanticOrderView : IElement
{
	/// <summary>
	/// TBD
	/// </summary>
	public IEnumerable ViewOrder { get; set; }
}
