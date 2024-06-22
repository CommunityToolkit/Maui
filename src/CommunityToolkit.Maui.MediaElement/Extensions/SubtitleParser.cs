namespace CommunityToolkit.Maui.Core;

/// <summary>
/// A class that Represents a parser.
/// </summary>
public class SubtitleParser
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
	public virtual List<SubtitleCue> ParseContent(string content)
	{
		return IParser.ParseContent(content);
	}

	internal static async Task<string> Content(string subtitleUrl)
	{
		try
		{
			return await httpClient.GetStringAsync(subtitleUrl).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			System.Diagnostics.Trace.TraceError(ex.Message);
			return string.Empty;
		}
	}
}
