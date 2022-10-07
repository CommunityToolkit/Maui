using System.Collections;

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// TBD
/// </summary>
public interface ISemanticOrderView : IContentView
{
	/// <summary>
	/// TBD
	/// </summary>
	public IEnumerable ViewOrder { get; set; }
}
