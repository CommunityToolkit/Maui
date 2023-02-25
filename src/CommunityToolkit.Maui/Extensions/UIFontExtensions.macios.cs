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
		if (fontManager is null)
		{
			return UIFont.SystemFontOfSize((nfloat)font.Size);
		}
		
		return fontManager.GetFont(font, UIFont.SystemFontSize);
	}
}