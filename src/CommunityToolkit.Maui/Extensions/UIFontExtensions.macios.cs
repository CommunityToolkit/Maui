using Microsoft.Extensions.DependencyInjection;
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
		var defaultFont = UIFont.SystemFontOfSize((nfloat)font.Size)
			?? throw new InvalidOperationException("Unable to create the default font.");
		var fontManager = Application.Current?.Handler?.MauiContext?.Services.GetService<IFontManager>();
		return fontManager?.GetFont(font, UIFont.SystemFontSize) ?? defaultFont;
	}
}