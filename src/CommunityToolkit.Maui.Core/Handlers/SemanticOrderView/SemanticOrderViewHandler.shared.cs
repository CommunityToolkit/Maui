using CommunityToolkit.Maui.Core.Views;

namespace CommunityToolkit.Maui.Core.Handlers;

/// <summary>
/// TBD
/// </summary>
public partial class SemanticOrderViewHandler
{
	/// <summary>
	/// PropertyMapper for SemanticOrderViewHandler Control
	/// </summary>
	public static IPropertyMapper<ISemanticOrderView, SemanticOrderViewHandler> SemanticOrderViewMapper = new PropertyMapper<ISemanticOrderView, SemanticOrderViewHandler>(ElementMapper)
	{
		[nameof(ISemanticOrderView.ViewOrder)] = MapViewOrder,
	};

	/// <summary>
	/// TBD
	/// </summary>
	/// <param name="mapper"></param>
	/// <param name="commandMapper"></param>
	public SemanticOrderViewHandler(IPropertyMapper mapper, CommandMapper? commandMapper = null)
		: base(mapper, commandMapper)
	{
	}
}

#if !(IOS || ANDROID || MACCATALYST || WINDOWS)
public partial class SemanticOrderViewHandler : Microsoft.Maui.Handlers.ElementHandler<ISemanticOrderView, object>
{
	/// <inheritdoc/>
	protected override object CreatePlatformElement() => throw new NotSupportedException();

	/// <summary>
	/// TBD
	/// </summary>
	/// <exception cref="NotSupportedException"></exception>
	public static void MapViewOrder(SemanticOrderViewHandler handler, ISemanticOrderView view) => throw new NotSupportedException();
}
#endif