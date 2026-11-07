using System.Globalization;
using CommunityToolkit.Maui.Media;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Xunit;

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
	public async Task StartListenAsyncFailsOnNet()
	{
		SpeechToText.SetDefault(new SpeechToTextImplementation());
		await Assert.ThrowsAsync<NotSupportedException>(() => SpeechToText.StartListenAsync(new SpeechToTextOptions { Culture = CultureInfo.CurrentCulture }, TestContext.Current.CancellationToken));
	}

	[Fact(Timeout = (int)TestDuration.Long)]
	public async Task StartStopListenAsyncShouldChangeState()
	{
		SpeechToText.SetDefault(new SpeechToTextImplementationMock(string.Empty, string.Empty));
		SpeechToText.Default.StateChanged += OnStateChanged;
		SpeechToText.Default.CurrentState.Should().Be(SpeechToTextState.Stopped);
		await SpeechToText.StartListenAsync(new SpeechToTextOptions { Culture = CultureInfo.CurrentCulture }, TestContext.Current.CancellationToken);
		SpeechToText.Default.CurrentState.Should().Be(SpeechToTextState.Listening);
		await SpeechToText.StopListenAsync(TestContext.Current.CancellationToken);
		SpeechToText.Default.CurrentState.Should().Be(SpeechToTextState.Stopped);
		SpeechToText.Default.StateChanged -= OnStateChanged;
		void OnStateChanged(object? sender, SpeechToTextStateChangedEventArgs args)
		{
			testOutputHelper.WriteLine(args.State.ToString());
		}
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
		await SpeechToText.StartListenAsync(new SpeechToTextOptions { Culture = CultureInfo.CurrentCulture }, TestContext.Current.CancellationToken);
		await Task.Delay(500, TestContext.Current.CancellationToken);
		await SpeechToText.StopListenAsync(TestContext.Current.CancellationToken);
		SpeechToText.Default.RecognitionResultUpdated -= OnRecognitionTextUpdated;
		SpeechToText.Default.RecognitionResultCompleted -= OnRecognitionTextCompleted;
		currentPartialText.Should().Be(expectedPartialText);
		currentFinalText.Should().Be(expectedFinalText);

		void OnRecognitionTextUpdated(object? sender, SpeechToTextRecognitionResultUpdatedEventArgs args)
		{
			currentPartialText = args.RecognitionResult;
		}

		void OnRecognitionTextCompleted(object? sender, SpeechToTextRecognitionResultCompletedEventArgs args)
		{
			currentFinalText = args.RecognitionResult.Text;
		}
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task StopListenAsyncFailsOnNet()
	{
		SpeechToText.SetDefault(new SpeechToTextImplementation());
		await Assert.ThrowsAsync<NotSupportedException>(() => SpeechToText.StopListenAsync(TestContext.Current.CancellationToken));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task RequestPermissionsFailsOnNet()
	{
		SpeechToText.SetDefault(new SpeechToTextImplementation());
		await Assert.ThrowsAnyAsync<NotImplementedException>(() => SpeechToText.RequestPermissions(TestContext.Current.CancellationToken));
	}

	[Fact]
	public void DefaultStateShouldBeStopped()
	{
		SpeechToText.SetDefault(new SpeechToTextImplementation());
		SpeechToText.Default.CurrentState.Should().Be(SpeechToTextState.Stopped);
	}

	[Fact]
	public void SpeechToTextRecognitionResultCompletedEventArgs_NullRecognitionResult_ThrowsArgumentNullException()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => new SpeechToTextRecognitionResultCompletedEventArgs(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		Assert.Equal("recognitionResult", exception.ParamName);
	}

	[Fact]
	public void SpeechToTextRecognitionResultUpdatedEventArgs_NullRecognitionResult_ThrowsArgumentNullException()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => new SpeechToTextRecognitionResultUpdatedEventArgs(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		Assert.Equal("recognitionResult", exception.ParamName);
	}

	[Fact]
	public void StartListenAsync_NullOptions_ThrowsArgumentNullException()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => { _ = SpeechToText.StartListenAsync(null, TestContext.Current.CancellationToken); });
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		Assert.Equal("options", exception.ParamName);
	}

	[Fact]
	public void OfflineStartListenAsync_NullOptions_ThrowsArgumentNullException()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => { _ = OfflineSpeechToText.StartListenAsync(null, TestContext.Current.CancellationToken); });
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		Assert.Equal("options", exception.ParamName);
	}

	[Fact]
	public void ImplementationStartListenAsync_NullOptions_ThrowsArgumentNullException()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => { _ = new SpeechToTextImplementation().StartListenAsync(null, TestContext.Current.CancellationToken); });
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		Assert.Equal("options", exception.ParamName);
	}

	[Fact]
	public void OfflineImplementationStartListenAsync_NullOptions_ThrowsArgumentNullException()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => { _ = new OfflineSpeechToTextImplementation().StartListenAsync(null, TestContext.Current.CancellationToken); });
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		Assert.Equal("options", exception.ParamName);
	}

	[Fact]
	public async Task ImplementationStartListenAsync_NullOptionsAndCanceledToken_ThrowsArgumentNullExceptionFromTheCall()
	{
		using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		await cancellationTokenSource.CancelAsync();

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => { _ = new SpeechToTextImplementation().StartListenAsync(null, cancellationTokenSource.Token); });
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ImplementationStartListenAsync_CanceledToken_FaultsTheTaskWithOperationCanceledException()
	{
		using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		await cancellationTokenSource.CancelAsync();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new SpeechToTextImplementation().StartListenAsync(new SpeechToTextOptions { Culture = CultureInfo.CurrentCulture }, cancellationTokenSource.Token));
	}
}
