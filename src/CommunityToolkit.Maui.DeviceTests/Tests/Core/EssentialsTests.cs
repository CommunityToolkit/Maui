using System.Globalization;
using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Media;
using CommunityToolkit.Maui.Storage;
using Xunit;

namespace CommunityToolkit.Maui.DeviceTests.Tests.Core;

public class FolderPickerResultTests
{
	[Fact]
	public void FolderPickerResult_Successful()
	{
		var folder = new Folder("/test", "Test");
		var result = new FolderPickerResult(folder, null);

		Assert.True(result.IsSuccessful);
		Assert.False(result.IsCancelled);
		Assert.NotNull(result.Folder);
		Assert.Null(result.Exception);
	}

	[Fact]
	public void FolderPickerResult_Cancelled()
	{
		var result = new FolderPickerResult(null, new OperationCanceledException());

		Assert.False(result.IsSuccessful);
		Assert.True(result.IsCancelled);
		Assert.Null(result.Folder);
		Assert.NotNull(result.Exception);
	}

	[Fact]
	public void FolderPickerResult_Failed()
	{
		var exception = new InvalidOperationException("Test error");
		var result = new FolderPickerResult(null, exception);

		Assert.False(result.IsSuccessful);
		Assert.False(result.IsCancelled);
		Assert.Null(result.Folder);
		Assert.Equal(exception, result.Exception);
	}
}

public class FolderPickerExceptionTests
{
	[Fact]
	public void FolderPickerException_HasMessage()
	{
		var exception = new FolderPickerException("Custom message");

		Assert.Equal("Custom message", exception.Message);
	}
}

public class FileSaverResultTests
{
	[Fact]
	public void FileSaverResult_Successful()
	{
		var result = new FileSaverResult("/saved/file.txt", null);

		Assert.True(result.IsSuccessful);
		Assert.False(result.IsCancelled);
		Assert.Equal("/saved/file.txt", result.FilePath);
		Assert.Null(result.Exception);
	}

	[Fact]
	public void FileSaverResult_Cancelled()
	{
		var result = new FileSaverResult(null, new OperationCanceledException());

		Assert.False(result.IsSuccessful);
		Assert.True(result.IsCancelled);
		Assert.Null(result.FilePath);
	}

	[Fact]
	public void FileSaverResult_Failed()
	{
		var exception = new IOException("Disk full");
		var result = new FileSaverResult(null, exception);

		Assert.False(result.IsSuccessful);
		Assert.False(result.IsCancelled);
		Assert.Equal(exception, result.Exception);
	}
}

public class FileSaveExceptionTests
{
	[Fact]
	public void FileSaveException_HasMessage()
	{
		var exception = new FileSaveException("Save failed");

		Assert.Equal("Save failed", exception.Message);
	}
}

public class SpeechToTextResultTests
{
	[Fact]
	public void SpeechToTextResult_WithText()
	{
		var result = new SpeechToTextResult("Hello world", null);

		Assert.True(result.IsSuccessful);
		Assert.Equal("Hello world", result.Text);
		Assert.Null(result.Exception);
	}

	[Fact]
	public void SpeechToTextResult_WithException()
	{
		var exception = new InvalidOperationException("Recognition failed");
		var result = new SpeechToTextResult(null, exception);

		Assert.False(result.IsSuccessful);
		Assert.Null(result.Text);
		Assert.Equal(exception, result.Exception);
	}

	[Fact]
	public void SpeechToTextResult_BothNull_Throws()
	{
		var thrown = false;
		try
		{
			new SpeechToTextResult(null, null);
		}
		catch (ArgumentNullException)
		{
			thrown = true;
		}

		Assert.True(thrown);
	}
}

public class SpeechToTextStateEnumTests
{
	[Theory]
	[InlineData(SpeechToTextState.Stopped, 0)]
	[InlineData(SpeechToTextState.Listening, 1)]
	[InlineData(SpeechToTextState.Silence, 2)]
	public void SpeechToTextState_HasExpectedValues(SpeechToTextState state, int expected)
	{
		Assert.Equal(expected, (int)state);
	}
}

public class SpeechToTextOptionsTests
{
	[Fact]
	public void SpeechToTextOptions_DefaultValues()
	{
		var options = new SpeechToTextOptions
		{
			Culture = CultureInfo.InvariantCulture,
		};

		Assert.Equal(CultureInfo.InvariantCulture, options.Culture);
		Assert.True(options.ShouldReportPartialResults);
		Assert.Equal(TimeSpan.MaxValue, options.AutoStopSilenceTimeout);
	}

	[Fact]
	public void SpeechToTextOptions_CanSetCulture()
	{
		var options = new SpeechToTextOptions
		{
			Culture = CultureInfo.GetCultureInfo("fr-FR"),
			ShouldReportPartialResults = false,
		};

		Assert.Equal("fr-FR", options.Culture.Name);
		Assert.False(options.ShouldReportPartialResults);
	}

	[Fact]
	public void SpeechToTextOptions_CanSetAutoStopSilenceTimeout()
	{
		var options = new SpeechToTextOptions
		{
			Culture = CultureInfo.InvariantCulture,
			AutoStopSilenceTimeout = TimeSpan.FromSeconds(5),
		};

		Assert.Equal(TimeSpan.FromSeconds(5), options.AutoStopSilenceTimeout);
	}

	[Fact]
	public void SpeechToTextOptions_NegativeAutoStopSilenceTimeout_Throws()
	{
		var thrown = false;
		try
		{
			new SpeechToTextOptions
			{
				Culture = CultureInfo.InvariantCulture,
				AutoStopSilenceTimeout = TimeSpan.FromSeconds(-1),
			};
		}
		catch (ArgumentOutOfRangeException)
		{
			thrown = true;
		}

		Assert.True(thrown);
	}
}

public class SpeechToTextEventArgsTests
{
	[Fact]
	public void SpeechToTextStateChangedEventArgs_CarriesState()
	{
		var args = new SpeechToTextStateChangedEventArgs(SpeechToTextState.Listening);

		Assert.Equal(SpeechToTextState.Listening, args.State);
	}

	[Fact]
	public void SpeechToTextRecognitionResultUpdatedEventArgs_CarriesText()
	{
		var args = new SpeechToTextRecognitionResultUpdatedEventArgs("partial text");

		Assert.Equal("partial text", args.RecognitionResult);
	}

	[Fact]
	public void SpeechToTextRecognitionResultCompletedEventArgs_CarriesResult()
	{
		var speechResult = new SpeechToTextResult("final text", null);
		var args = new SpeechToTextRecognitionResultCompletedEventArgs(speechResult);

		Assert.Equal(speechResult, args.RecognitionResult);
		Assert.Equal("final text", args.RecognitionResult.Text);
	}
}
