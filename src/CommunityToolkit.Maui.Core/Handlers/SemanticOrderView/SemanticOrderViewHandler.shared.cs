using CommunityToolkit.Maui.Core.Views;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.Core.Handlers;

/// <summary>
/// Handler SemanticOrderView control
/// </summary>
public partial class SemanticOrderViewHandler : ContentViewHandler
{
	/// <summary>
	/// PropertyMapper for SemanticOrderView Control
	/// </summary>
	public static IPropertyMapper<ISemanticOrderView, SemanticOrderViewHandler> SemanticOrderViewMapper = new PropertyMapper<ISemanticOrderView, SemanticOrderViewHandler>(ContentViewHandler.Mapper)
	{
		[nameof(ISemanticOrderView.ViewOrder)] = MapViewOrder,
	};
	/// <summary>
	/// <see cref ="CommandMapper"/> for SemanticOrderView Control.
	/// </summary>
	public static CommandMapper<ISemanticOrderView, SemanticOrderViewHandler> SemanticOrderViewCommandMapper = new(CommandMapper)
	{
	};

	/// <summary>
	/// Constructor for <see cref="SemanticOrderViewHandler"/>.
	/// </summary>
	/// <param name="mapper">Custom instance of <see cref="PropertyMapper"/>, if it's null the <see cref="SemanticOrderViewMapper"/> will be used</param>
	/// <param name="commandMapper">Custom instance of <see cref="CommandMapper"/>, if it's null the <see cref="SemanticOrderViewCommandMapper"/> will be used</param>
	public SemanticOrderViewHandler(IPropertyMapper? mapper, CommandMapper? commandMapper)
		: base(mapper ?? SemanticOrderViewMapper, commandMapper ?? SemanticOrderViewCommandMapper)
	{
	}

	/// <summary>
	/// Default Constructor for <see cref="SemanticOrderViewHandler"/>.
	/// </summary>
	public SemanticOrderViewHandler() : base(SemanticOrderViewMapper, SemanticOrderViewCommandMapper)
	{

	}

	/// <inheritdoc/>
	public override void SetVirtualView(IView view)
	{
		base.SetVirtualView(view);
#if WINDOWS || IOS || MACCATALYST || ANDROID || TIZEN
		if (PlatformView is MauiSemanticOrderView semanticOrderView)
		{
			semanticOrderView.VirtualView = (ISemanticOrderView)VirtualView;
		}
#endif
	}

	static void MapViewOrder(SemanticOrderViewHandler handler, ISemanticOrderView view)
	{
#if WINDOWS || IOS || MACCATALYST || ANDROID || TIZEN
		if (handler.PlatformView is MauiSemanticOrderView semanticOrderView)
		{
			semanticOrderView.UpdateViewOrder();
		}
#endif
	}
}