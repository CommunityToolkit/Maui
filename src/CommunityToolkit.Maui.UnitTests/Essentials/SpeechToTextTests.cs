using System.Globalization;
using CommunityToolkit.Maui.Media;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace CommunityToolkit.Maui.UnitTests.Essentials;

public class SpeechToTextTests(ITestOutputHelper testOutputHelper) : BaseTest
{
	[Fact]
	public void SpeechToTextTestsSetDefaultUpdatesInstance()
	{
		var speechToTextImplementationMock = new SpeechToTextImplementationMock(string.Empty, string.Empty);
		SpeechToText.SetDefault(speechToTextImplementationMock);
		var speechToText = SpeechToText.Default;
		speechToText.Should().BeSameAs(speechToTextImplementationMock);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ListenAsyncFailsOnNet()
	{
		SpeechToText.SetDefault(new SpeechToTextImplementation());
		var result = await SpeechToText.ListenAsync(CultureInfo.CurrentCulture, null, CancellationToken.None);
		result.Text.Should().BeNull();
		result.Exception.Should().BeOfType<NotImplementedInReferenceAssemblyException>();
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task StartListenAsyncFailsOnNet()
	{
		SpeechToText.SetDefault(new SpeechToTextImplementation());
		await Assert.ThrowsAsync<NotImplementedInReferenceAssemblyException>(() => SpeechToText.StartListenAsync(CultureInfo.CurrentCulture, CancellationToken.None));
	}

	[Fact(Timeout = (int)TestDuration.Long)]
	public async Task StartStopListenAsyncShouldChangeState()
	{
		SpeechToText.SetDefault(new SpeechToTextImplementationMock(string.Empty, string.Empty));
		SpeechToText.Default.StateChanged += OnStateChanged;
		SpeechToText.Default.CurrentState.Should().Be(SpeechToTextState.Stopped);
		await SpeechToText.StartListenAsync(CultureInfo.CurrentCulture, CancellationToken.None);
		SpeechToText.Default.CurrentState.Should().Be(SpeechToTextState.Listening);
		await SpeechToText.StopListenAsync(CancellationToken.None);
		SpeechToText.Default.CurrentState.Should().Be(SpeechToTextState.Stopped);
		SpeechToText.Default.StateChanged -= OnStateChanged;
		void OnStateChanged(object? sender, SpeechToTextStateChangedEventArgs args)
		{
			testOutputHelper.WriteLine(args.State.ToString());
		};
	}

	[Fact(Timeout = (int)TestDuration.Long)]
	public async Task StartStopListenAsyncShouldChangeRecognitionText()
	{
		var expectedPartialText = ".NET MAUI";
		var expectedFinalText = ".NET MAUI";
		var currentPartialText = string.Empty;
		var currentFinalText = string.Empty;
		SpeechToText.SetDefault(new SpeechToTextImplementationMock(expectedPartialText, expectedFinalText));
		SpeechToText.Default.RecognitionResultUpdated += OnRecognitionTextUpdated;
		SpeechToText.Default.RecognitionResultCompleted += OnRecognitionTextCompleted;
		await SpeechToText.StartListenAsync(CultureInfo.CurrentCulture, CancellationToken.None);
		await Task.Delay(500);
		await SpeechToText.StopListenAsync(CancellationToken.None);
		SpeechToText.Default.RecognitionResultUpdated -= OnRecognitionTextUpdated;
		SpeechToText.Default.RecognitionResultCompleted -= OnRecognitionTextCompleted;
		currentPartialText.Should().Be(expectedPartialText);
		currentFinalText.Should().Be(expectedFinalText);

		void OnRecognitionTextUpdated(object? sender, SpeechToTextRecognitionResultUpdatedEventArgs args)
		{
			currentPartialText = args.RecognitionResult;
		};
		void OnRecognitionTextCompleted(object? sender, SpeechToTextRecognitionResultCompletedEventArgs args)
		{
			currentFinalText = args.RecognitionResult;
		};
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task StopListenAsyncFailsOnNet()
	{
		SpeechToText.SetDefault(new SpeechToTextImplementation());
		await Assert.ThrowsAsync<NotSupportedException>(() => SpeechToText.StopListenAsync(CancellationToken.None));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task RequestPermissionsFailsOnNet()
	{
		SpeechToText.SetDefault(new SpeechToTextImplementation());
		await Assert.ThrowsAsync<NotImplementedInReferenceAssemblyException>(() => SpeechToText.RequestPermissions(CancellationToken.None));
	}

	[Fact]
	public void DefaultStateShouldBeStopped()
	{
		SpeechToText.SetDefault(new SpeechToTextImplementation());
		SpeechToText.Default.CurrentState.Should().Be(SpeechToTextState.Stopped);
	}
}