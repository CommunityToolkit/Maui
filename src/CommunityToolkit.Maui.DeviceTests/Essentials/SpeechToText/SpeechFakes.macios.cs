using AVFoundation;
using Foundation;
using Microsoft.Maui.Dispatching;
using ObjCRuntime;
using Speech;

namespace CommunityToolkit.Maui.DeviceTests;

// The Speech framework produces recognition results only from real audio, with the consent of a person, and a headless run has
// neither a usable microphone nor anybody to answer the consent prompts. The toolkit only reads State, Final and
// BestTranscription.FormattedString, and calls Finish / InstallTapOnBus / Prepare / StartAndReturnError / Stop / RemoveTapOnBus, so
// the fakes simulate nothing else. The types are public because the protected helpers of the public test base expose them.

// A real Objective-C object (a registered subclass), so that it reaches the toolkit as the return value of the stubbed
// recognitionTaskWithRequest:resultHandler:.
public sealed class SpeechFakeTask : SFSpeechRecognitionTask
{
	SFSpeechRecognitionTaskState state;

	public SpeechFakeTask(SFSpeechRecognitionTaskState state) => this.state = state;

	public override SFSpeechRecognitionTaskState State => state;

	public int FinishCount { get; private set; }

	public bool IsDisposed { get; private set; }

	public void MoveTo(SFSpeechRecognitionTaskState newState) => state = newState;

	public override void Finish() => FinishCount++;

	protected override void Dispose(bool disposing)
	{
		IsDisposed = true;
		base.Dispose(disposing);
	}
}

public sealed class SpeechFakeTranscription : SFTranscription
{
	readonly string text;

	public SpeechFakeTranscription(string text) : base(NSObjectFlag.Empty) => this.text = text;

	public override string FormattedString => text;
}

public sealed class SpeechFakeRecognitionResult : SFSpeechRecognitionResult
{
	readonly bool isFinal;
	readonly SpeechFakeTranscription transcription;

	public SpeechFakeRecognitionResult(bool isFinal, string text) : base(NSObjectFlag.Empty)
	{
		this.isFinal = isFinal;
		transcription = new SpeechFakeTranscription(text);
	}

	public override bool Final => isFinal;

	public override SFTranscription BestTranscription => transcription;
}

// isRunning stands for the timer that CreateSilenceTimer leaves running for a start with an AutoStopSilenceTimeout; it is not counted
// as a Start call.
public sealed class SpeechFakeDispatcherTimer(bool isRunning = false) : IDispatcherTimer
{
	EventHandler? tick;

	public TimeSpan Interval { get; set; }

	public bool IsRepeating { get; set; } = true;

	public bool IsRunning { get; private set; } = isRunning;

	public int StartCount { get; private set; }

	public int StopCount { get; private set; }

	public int TickSubscriberCount => tick?.GetInvocationList().Length ?? 0;

	public event EventHandler? Tick
	{
		add => tick += value;
		remove => tick -= value;
	}

	public void Start()
	{
		StartCount++;
		IsRunning = true;
	}

	public void Stop()
	{
		StopCount++;
		IsRunning = false;
	}
}

public sealed class SpeechFakeInputNode : AVAudioInputNode
{
	readonly AVAudioFormat format;
	readonly List<string> calls;
	AVAudioNodeTapBlock? tapBlock;

	public SpeechFakeInputNode(AVAudioFormat format, List<string> calls) : base(NSObjectFlag.Empty)
	{
		this.format = format;
		this.calls = calls;
	}

	public Exception? InstallTapException { get; set; }

	public int InstallTapCount { get; private set; }

	public nuint InstalledTapBus { get; private set; }

	public uint InstalledTapBufferSize { get; private set; }

	public AVAudioFormat? InstalledTapFormat { get; private set; }

	public bool HasTap => tapBlock is not null;

	public override AVAudioFormat GetBusOutputFormat(nuint bus) => format;

	public override void InstallTapOnBus(nuint bus, uint bufferSize, AVAudioFormat? tapFormat, AVAudioNodeTapBlock tapBlock)
	{
		calls.Add("installTap");
		InstallTapCount++;
		if (InstallTapException is not null)
		{
			throw InstallTapException;
		}

		InstalledTapBus = bus;
		InstalledTapBufferSize = bufferSize;
		InstalledTapFormat = tapFormat;
		this.tapBlock = tapBlock;
	}

	public override void RemoveTapOnBus(nuint bus)
	{
		calls.Add(bus is 0 ? "removeTap" : $"removeTap:{bus}");
		tapBlock = null;
	}

	public AVAudioPcmBuffer DeliverBuffer()
	{
		var block = tapBlock ?? throw new InvalidOperationException("The toolkit has not installed a tap (or it was removed).");
		var buffer = new AVAudioPcmBuffer(format, 160) { FrameLength = 160 };
		block(buffer, null!);

		return buffer;
	}
}

public sealed class SpeechFakeAudioEngine : AVAudioEngine
{
	bool isRunning;

	public SpeechFakeAudioEngine() => FakeInputNode = new SpeechFakeInputNode(new AVAudioFormat(16000, 1), Calls);

	public List<string> Calls { get; } = [];

	public SpeechFakeInputNode FakeInputNode { get; }

	public NSError? StartError { get; set; }

	public Action? OnStart { get; set; }

	public override AVAudioInputNode InputNode => IsDisposedEngine ? null! : FakeInputNode;

	public override bool Running => isRunning;

	// Once disposed it behaves like a disposed real engine: messages go to a nil handle, so InputNode is null and Prepare and Stop do
	// nothing.
	bool IsDisposedEngine => Handle == NativeHandle.Zero;

	public override void Prepare()
	{
		if (!IsDisposedEngine)
		{
			Calls.Add("prepare");
		}
	}

	public override bool StartAndReturnError(out NSError outError)
	{
		Calls.Add("start");
		OnStart?.Invoke();
		outError = StartError!;
		isRunning = StartError is null;

		return isRunning;
	}

	public override void Stop()
	{
		if (!IsDisposedEngine)
		{
			Calls.Add("stop");
			isRunning = false;
		}
	}
}