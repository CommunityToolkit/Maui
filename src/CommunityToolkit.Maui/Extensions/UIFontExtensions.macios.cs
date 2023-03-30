using UIKit;
using Font = Microsoft.Maui.Font;

namespace CommunityToolkit.Maui.Extensions;

/// <summary>
/// UIFont Extensions
/// </summary>
public static class UIFontExtensions
{
	/// <summary>
	/// Convert <see cref="Font"/> to <see cref="UIFont"/>
	/// </summary>
	public static UIFont ToUIFont(this Font font)
	{
		var fontManager = Application.Current?.RequireFontManager();
		return fontManager is null ? UIFont.SystemFontOfSize((nfloat)font.Size) : fontManager.GetFont(font, UIFont.SystemFontSize);
	}
}