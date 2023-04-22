using System.Diagnostics.CodeAnalysis;

namespace CommunityToolkit.Maui.Media;

/// <summary>
/// Result of the <see cref="ISpeechToText"/>
/// </summary>
/// <param name="Text">Recognition result</param>
/// <param name="Exception">Exception if operation failed</param>
public record SpeechToTextResult(string? Text, Exception? Exception)
{
	/// <summary>
	/// Check if operation was successful.
	/// </summary>
	[MemberNotNullWhen(true, nameof(Text))]
	[MemberNotNullWhen(false, nameof(Exception))]
	public bool IsSuccessful => Exception is null;

	/// <summary>
	/// Check if operation was successful.
	/// </summary>
	[MemberNotNull(nameof(Text))]
	public void EnsureSuccess()
	{
		if (!IsSuccessful)
		{
			throw Exception;
		}
	}
}