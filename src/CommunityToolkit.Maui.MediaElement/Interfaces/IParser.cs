namespace CommunityToolkit.Maui.Core;

/// <summary>
/// A parser interface
/// </summary>
public interface IParser
{
	/// <summary>
	/// A method that parses the content.
	/// </summary>
	/// <param name="content"></param>
	/// <returns></returns>
	public SubtitleDocument ParseContent(string content);
}
