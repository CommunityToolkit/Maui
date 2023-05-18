using System.Diagnostics.CodeAnalysis;

namespace CommunityToolkit.Maui.Media;

/// <summary>
/// Result of the <see cref="ISpeechToText"/>
/// </summary>
public record SpeechToTextResult
{
	/// <summary>
	/// Creates the result of the <see cref="ISpeechToText"/>
	/// </summary>
	/// <param name="text">Recognition result</param>
	/// <param name="exception">Exception if operation failed</param>
	public SpeechToTextResult(string? text, Exception? exception)
	{
		if (exception is null && text is null)
		{
			throw new ArgumentNullException(nameof(text), $"{nameof(text)} cannot be null when {nameof(exception)} is null");
		}

		Text = text;
		Exception = exception;
	}

	/// <summary>
	/// Check if operation was successful.
	/// </summary>
	[MemberNotNullWhen(true, nameof(Text))]
	[MemberNotNullWhen(false, nameof(Exception))]
	public bool IsSuccessful => Exception is null;

	/// <summary>
	/// Text result from <see cref="ISpeechToText"/>
	/// </summary>
	public string? Text { get; }

	/// <summary>
	/// Exception thrown during <see cref="ISpeechToText"/>
	/// </summary>
	public Exception? Exception { get; }

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