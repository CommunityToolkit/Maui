using System.Reflection;
using System.Text.RegularExpressions;

namespace CommunityToolkit.Maui.Core;

static class DeviceFontSpecs
{
	/// <summary>
	/// Extracts and returns the font file and font name for Android, Windows, and iOS from the given input string.
	/// </summary>
	/// <param name="input">The input string in the format "fontfile.ttf#fontname".</param>
	/// <returns>A tuple containing the font specifications for Android, Windows, and iOS.</returns>
	public static (string androidFont, string windowsFont, string iOSFont) OutputDeviceSpecifications(string input)
	{
		string pattern = @"(.+\.ttf)#(.+)";

		var match = Regex.Match(input, pattern);

		if (match.Success)
		{
			// Extract the font file and font name
			var fontFile = match.Groups[1].Value;
			var fontName = match.Groups[2].Value;
			string windowsFont = $"ms-appx:///{fontFile}#{fontName}";
			return (fontFile, windowsFont, fontName);
		}
		else
		{
			System.Diagnostics.Trace.TraceError("The input string is not in the expected format.");
		}
		return (string.Empty, string.Empty, string.Empty);
	}
}

static class FontHelper
{
	/// <summary>
	/// Returns the list of exported fonts from the assembly.
	/// </summary>
	/// <returns></returns>
	public static IEnumerable<(string FontFileName, string Alias)> GetExportedFonts()
	{
		var assembly = typeof(FontHelper).Assembly;
		var exportedFonts = new List<(string FontFileName, string Alias)>();

		var customAttributes = assembly.GetCustomAttributes<ExportFontAttribute>();

		foreach (var attribute in customAttributes)
		{
			exportedFonts.Add((attribute.FontFileName, attribute.Alias));
		}

		return exportedFonts;
	}
}
