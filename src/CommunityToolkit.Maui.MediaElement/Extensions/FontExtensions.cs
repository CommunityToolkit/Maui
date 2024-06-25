using System.Reflection;
using System.Text.RegularExpressions;

namespace CommunityToolkit.Maui.Core;

static class FontExtensions
{
	public record struct FontFamily(string input)
	{
		static readonly string pattern = @"(.+\.ttf)#(.+)";

		readonly Match match = Regex.Match(input, pattern);
		public readonly string Android
		{
				get 
				{
					if (match.Success)
					{
						return match.Groups[1].Value;
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
					if (match.Success)
					{
						return $"ms-appx:///{match.Groups[1].Value}#{match.Groups[2].Value}";
					}
					else
					{
						System.Diagnostics.Trace.TraceError("The input string is not in the expected format.");
						return string.Empty;
					}
				}
		}
		public readonly string MacIOS
		{
				get 
				{
					if (match.Success)
					{
						return match.Groups[2].Value;
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
