namespace CommunityToolkit.Maui.Core.Handlers;

public partial class ExpanderHandler : Microsoft.Maui.Handlers.ViewHandler<IExpander, object>
{
	/// <inheritdoc/>
	protected override object CreatePlatformView() => throw new NotSupportedException();

	/// <summary>
	/// Action that's triggered when the Expander is Opened.
	/// </summary>
	/// <param name="handler">An instance of <see cref="ExpanderHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IExpander"/>.</param>
	/// <param name="result">We don't need to provide the result parameter here.</param>
	public static void MapExpandedChanged(ExpanderHandler handler, IExpander view, object? result)
	{
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="handler"></param>
	/// <param name="view"></param>
	/// <exception cref="NotImplementedException"></exception>
	public static void MapIsExpanded(ExpanderHandler handler, IExpander view)
	{
		throw new NotImplementedException();
	}

	/// <summary>
	/// Action that's triggered when the Expander <see cref="IExpander.Direction"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="ExpanderHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IExpander"/>.</param>
	public static void MapDirection(ExpanderHandler handler, IExpander view)
	{
	}

	/// <summary>
	/// Action that's triggered when the Expander <see cref="IExpander.Header"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="ExpanderHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IExpander"/>.</param>
	public static void MapHeader(ExpanderHandler handler, IExpander view)
	{
	}

	/// <summary>
	/// Action that's triggered when the Expander <see cref="IExpander.Content"/> property changes.
	/// </summary>
	/// <param name="handler">An instance of <see cref="ExpanderHandler"/>.</param>
	/// <param name="view">An instance of <see cref="IExpander"/>.</param>
	public static void MapContent(ExpanderHandler handler, IExpander view)
	{
	}
}