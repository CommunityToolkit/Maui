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
	/// Constructor for <see cref="SemanticOrderViewHandler"/>.
	/// </summary>
	/// <param name="mapper">Custom instance of <see cref="PropertyMapper"/>, if it's null the <see cref="SemanticOrderViewMapper"/> will be used</param>
	public SemanticOrderViewHandler(IPropertyMapper? mapper)
		: base(mapper ?? SemanticOrderViewMapper)
	{
	}

	/// <summary>
	/// Default Constructor for <see cref="SemanticOrderViewHandler"/>.
	/// </summary>
	public SemanticOrderViewHandler()
		: base(SemanticOrderViewMapper)
	{
	}
}

#if !(IOS || ANDROID || MACCATALYST || WINDOWS)
public partial class SemanticOrderViewHandler : Microsoft.Maui.Handlers.ViewHandler<ISemanticOrderView, object>
{
	/// <inheritdoc/>
	protected override object CreatePlatformView() => throw new NotSupportedException();

	/// <summary>
	/// TBD
	/// </summary>
	/// <exception cref="NotSupportedException"></exception>
	public static void MapViewOrder(SemanticOrderViewHandler handler, ISemanticOrderView view) => throw new NotSupportedException();
}
#endif