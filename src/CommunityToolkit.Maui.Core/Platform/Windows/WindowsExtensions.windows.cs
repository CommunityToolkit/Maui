using XamlStyle = Microsoft.UI.Xaml.Style;

namespace CommunityToolkit.Maui.Core.Platform;
static class WindowsExtensions
{
	public static XamlStyle UpdateStyle<TElement, TControl>(this TControl control, TElement element, XamlStyle currentStyle, Func<TControl, TElement, XamlStyle, XamlStyle> updateStyleFunc)
		where TElement : IElement
	{
		return updateStyleFunc(control, element, currentStyle);
	}
}
