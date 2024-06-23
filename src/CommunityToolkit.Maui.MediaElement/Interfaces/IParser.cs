﻿namespace CommunityToolkit.Maui.Core;

/// <summary>
/// 
/// </summary>
public interface IParser
{
	/// <summary>
	/// A method that parses the content.
	/// </summary>
	/// <param name="content"></param>
	/// <returns></returns>
	public List<SubtitleCue> ParseContent(string content);
}