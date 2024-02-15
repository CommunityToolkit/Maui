using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.Core.Handlers;

/// <summary>
/// Handler Expander control
/// </summary>
public partial class ExpanderHandler
{
	/// <summary>
	/// PropertyMapper for Expander Control
	/// </summary>
	public static IPropertyMapper<IExpander, ExpanderHandler> ExpanderMapper = new PropertyMapper<IExpander, ExpanderHandler>(ViewMapper)
	{
		[nameof(IExpander.Direction)] = MapDirection,
		[nameof(IExpander.Header)] = MapHeader,
		[nameof(IExpander.Content)] = MapContent,
		[nameof(IExpander.IsExpanded)] = MapIsExpanded,
	};
	
	/// <summary>
	/// Constructor for <see cref="ExpanderHandler"/>.
	/// </summary>
	/// <param name="mapper">Custom instance of <see cref="PropertyMapper"/>, if it's null the <see cref="ExpanderMapper"/> will be used</param>
	/// <param name="commandMapper">Custom instance of <see cref="CommandMapper"/>, if it's null the <see cref="ViewHandler.ViewCommandMapper"/> will be used</param>
	public ExpanderHandler(IPropertyMapper? mapper, CommandMapper? commandMapper)
		: base(mapper ?? ExpanderMapper, commandMapper ?? ViewCommandMapper)
	{
	}

	/// <summary>
	/// Default Constructor for <see cref="ExpanderHandler"/>.
	/// </summary>
	public ExpanderHandler()
		: base(ExpanderMapper, ViewCommandMapper)
	{
	}
}