using System.Reflection;
using System.Text.RegularExpressions;

namespace CommunityToolkit.Maui.Core;

sealed class FontExtensions
{
	public record struct FontFamily(string input)
	{
		static readonly string ttfPattern = @"(.+\.ttf)#(.+)";
		static readonly string otfPattern = @"(.+\.otf)#(.+)";
		readonly Match ttfMatch = Regex.Match(input, ttfPattern);
		readonly Match otfMatch = Regex.Match(input, otfPattern);
		public readonly string Android
		{
				get 
				{
					if (ttfMatch.Success)
					{
						return ttfMatch.Groups[1].Value;
					}
					if (otfMatch.Success)
					{
						return otfMatch.Groups[1].Value;
					}
					else
					{
						System.Diagnostics.Trace.TraceError("The input string is not in the expected format.");
						return string.Empty;
					}
				}
		}
		public readonly string WindowsFont
		{
				get 
				{
					if (ttfMatch.Success)
					{
						return $"ms-appx:///{ttfMatch.Groups[1].Value}#{ttfMatch.Groups[2].Value}";
					}
					if (otfMatch.Success)
					{
						return $"ms-appx:///{otfMatch.Groups[1].Value}#{otfMatch.Groups[2].Value}";
					}
					else
					{
						System.Diagnostics.Trace.TraceError("The input string is not in the expected format.");
						return string.Empty;
					}				}
		}
		public readonly string MacIOS
		{
				get 
				{
					if (ttfMatch.Success)
					{
						return ttfMatch.Groups[2].Value;
					}
					if (otfMatch.Success)
					{
						return otfMatch.Groups[2].Value;
					}
					else
					{
						System.Diagnostics.Trace.TraceError("The input string is not in the expected format.");
						return string.Empty;
					}
				}
		}
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
