using System.Text.RegularExpressions;

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// A class that Represents a parser.
/// </summary>
public partial class SubtitleParser
{
	static readonly HttpClient httpClient = new();

	/// <summary>
	/// A property that represents the <see cref="IParser"/>
	/// </summary>
	public IParser IParser { get; set; }

	/// <summary>
	/// A property that represents the separator.
	/// </summary>
	public static readonly string[] Separator = ["\r\n", "\n"];
	
	/// <summary>
	/// A constructor that initializes the <see cref="IParser"/>
	/// </summary>
	/// <param name="parser"></param>
	public SubtitleParser(IParser parser)
	{
		this.IParser = parser;
	}

	/// <summary>
	/// A method that parses the content.
	/// </summary>
	/// <param name="content"></param>
	/// <returns></returns>
	public virtual SubtitleDocument ParseContent(string content)
	{
		return IParser.ParseContent(content);
	}

	internal static async Task<string> Content(string? subtitleUrl)
	{
		ArgumentNullException.ThrowIfNull(subtitleUrl);
		if (string.IsNullOrWhiteSpace(subtitleUrl))
		{
			throw new ArgumentException("Url is empty.");
		}
		if (!ValidateUrlWithRegex(subtitleUrl))
		{
			throw new UriFormatException("Invalid URL");
		}
		
		
		try
		{
			return await httpClient.GetStringAsync(subtitleUrl).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			System.Diagnostics.Trace.TraceError(ex.Message);
			throw new FormatException("Invalid URL");
		}
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="url"></param>
	/// <returns></returns>
    internal static bool ValidateUrlWithRegex(string url)
    {
        var urlRegex = ValidateUrl();
        urlRegex.Matches(url);
        if(!urlRegex.IsMatch(url))
		{
			return false;
		}
        return true;
    }

	[GeneratedRegex(@"^(https?|ftps?):\/\/(?:[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,}(?::(?:0|[1-9]\d{0,3}|[1-5]\d{4}|6[0-4]\d{3}|65[0-4]\d{2}|655[0-2]\d|6553[0-5]))?(?:\/(?:[-a-zA-Z0-9@%_\+.~#?&=]+\/?)*)?$", RegexOptions.IgnoreCase, "en-CA")]
	private static partial Regex ValidateUrl();
}

