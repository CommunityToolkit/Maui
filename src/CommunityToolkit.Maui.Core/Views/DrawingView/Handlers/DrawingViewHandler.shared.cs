using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// DrawingView handler
/// </summary>
public partial class DrawingViewHandler
{
	/// <summary>
	/// Initialize new instance of <see cref="DrawingViewHandler"/>.
	/// </summary>
	public DrawingViewHandler() : base(ViewMapper)
	{
	}

	/// <summary>
	/// Initialize new instance of <see cref="DrawingViewHandler"/>.
	/// </summary>
	/// <param name="mapper">Custom instance of <see cref="IPropertyMapper"/></param>
	/// <param name="commandMapper">Custom instance of <see cref="CommandMapper"/></param>
	public DrawingViewHandler(IPropertyMapper mapper, CommandMapper? commandMapper) : base(mapper, commandMapper)
	{
	}
}

#if !(ANDROID || IOS || MACCATALYST || WINDOWS)
public partial class DrawingViewHandler : ViewHandler<IDrawingView, object>
{
	/// <inheritdoc />
	protected override object CreateNativeView() => throw new NotImplementedException();
}
#endif