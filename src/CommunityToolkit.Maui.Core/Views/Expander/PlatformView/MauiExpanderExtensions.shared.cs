using CommunityToolkit.Maui.Core.Views;
using Microsoft.Maui.Platform;
#if WINDOWS
using MauiExpander = Microsoft.UI.Xaml.Controls.Expander;
#endif
namespace CommunityToolkit.Maui.Core.Extensions;

/// <summary>
/// Extension methods to support <see cref="IExpander"/>
/// </summary>
public static partial class MauiExpanderExtensions
{
#if ANDROID || IOS || MACCATALYST || WINDOWS


	/// <summary>
	/// Set Header
	/// </summary>
	public static void SetHeader(this MauiExpander mauiExpander, IView? header, IMauiContext context)
	{
		mauiExpander.Header = header?.ToPlatform(context);
	}

	/// <summary>
	/// Set Content
	/// </summary>
	public static void SetContent(this MauiExpander mauiExpander, IView? content, IMauiContext context)
	{
		mauiExpander.Content = content?.ToPlatform(context);
	}

	/// <summary>
	/// Set IsExpanded
	/// </summary>
	public static void SetIsExpanded(this MauiExpander mauiExpander, bool isExpanded)
	{
		if (mauiExpander.IsExpanded != isExpanded)
		{
			mauiExpander.IsExpanded = isExpanded;
		}
	}

	/// <summary>
	/// Set Direction
	/// </summary>
	public static void SetDirection(this MauiExpander mauiExpander, ExpandDirection direction)
	{
		mauiExpander.ExpandDirection = direction.ToPlatform();
	}
#endif

#if WINDOWS
	/// <summary>
	/// Converts platform expand direction to virtual expand direction
	/// </summary>
	public static Microsoft.UI.Xaml.Controls.ExpandDirection ToPlatform(this ExpandDirection direction)
	{
		return Enum.Parse<Microsoft.UI.Xaml.Controls.ExpandDirection>(direction.ToString());
	}
#else
	/// <summary>
	/// Converts platform expand direction to virtual expand direction
	/// </summary>
	public static ExpandDirection ToPlatform(this ExpandDirection direction)
	{
		return direction;
	}
#endif
}