using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Handlers;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

public class MockExpanderHandler : ViewHandler<IExpander, object>
{
	IDrawingLineAdapter adapter = new DrawingLineAdapter();

	public static readonly PropertyMapper<IExpander, MockExpanderHandler> ExpanderPropertyMapper = new(ViewMapper)
	{
		[nameof(IExpander.Header)] = MapHeader,
		[nameof(IExpander.Content)] = MapContent,
		[nameof(IExpander.IsExpanded)] = MapIsExpanded,
		[nameof(IExpander.Direction)] = MapDirection
	};

	public MockExpanderHandler() : this(ExpanderPropertyMapper)
	{
	}

	public MockExpanderHandler(IPropertyMapper mapper) : base(mapper)
	{

	}

	public int MapHeaderCount { get; private set; }
	public int MapContentCount { get; private set; }
	public int MapIsExpandedCount { get; private set; }
	public int MapDirectionCount { get; private set; }

	static void MapHeader(MockExpanderHandler arg1, IExpander arg2)
	{
		arg1.MapHeaderCount++;
	}

	static void MapContent(MockExpanderHandler arg1, IExpander arg2)
	{
		arg1.MapContentCount++;
	}

	static void MapIsExpanded(MockExpanderHandler arg1, IExpander arg2)
	{
		arg1.MapIsExpandedCount++;
	}

	static void MapDirection(MockExpanderHandler arg1, IExpander arg2)
	{
		arg1.MapDirectionCount++;
	}

	protected override object CreatePlatformView()
	{
		return new object();
	}
}