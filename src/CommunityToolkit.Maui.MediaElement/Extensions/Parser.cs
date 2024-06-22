namespace CommunityToolkit.Maui.Core;

/// <summary>
/// 
/// </summary>
public partial class Parser
{
	/// <summary>
	/// 
	/// </summary>
	public IParser IParser { get; set; }
	static readonly HttpClient httpClient = new();

	/// <summary>
	/// 
	/// </summary>
	public static readonly string[] Separator = ["\r\n", "\n"];
	
	/// <summary>
	/// 
	/// </summary>
	/// <param name="parser"></param>
	public Parser(IParser parser)
	{
		this.IParser = parser;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="content"></param>
	/// <returns></returns>
	public virtual List<SubtitleCue> ParseContent(string content)
	{
		return IParser.ParseContent(content);
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="subtitleUrl"></param>
	/// <returns></returns>
	public static async Task<string> Content(string subtitleUrl)
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
