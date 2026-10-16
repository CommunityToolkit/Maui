using AVFoundation;
using Foundation;
using Microsoft.Maui.Dispatching;
using ObjCRuntime;
using Speech;

namespace CommunityToolkit.Maui.DeviceTests;

// The Speech framework produces recognition results only from real audio, with the consent of a person, and a headless run has
// neither a usable microphone nor anybody to answer the consent prompts. These subclasses of the real framework types let a test
// hand the toolkit exactly what the framework or the audio hardware would. The toolkit only reads State, Final and
// BestTranscription.FormattedString, and calls Finish / InstallTapOnBus / Prepare / StartAndReturnError / Stop / RemoveTapOnBus,
// so nothing else is simulated. (The types are public because the protected helpers of the public test base expose them.)

/// <summary>
/// A recognition task handed to the toolkit by the stubbed <c>recognitionTaskWithRequest:resultHandler:</c>. It is a real
/// Objective-C object (a registered subclass), so it travels through the framework's return value to the toolkit.
/// </summary>
public sealed class SpeechFakeTask : SFSpeechRecognitionTask
{
	SFSpeechRecognitionTaskState state;

	public SpeechFakeTask(SFSpeechRecognitionTaskState state) => this.state = state;

	public override SFSpeechRecognitionTaskState State => state;

	public int FinishCount { get; private set; }

	public bool IsDisposed { get; private set; }

	/// <summary>What the framework does to a task: Starting until audio is processed, then Running, then Finishing/Completed.</summary>
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

/// <summary>A recognition result as the framework hands it to the result handler (partial or final).</summary>
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

/// <summary>
/// A dispatcher timer that counts what the toolkit does to it. <paramref name="isRunning"/> creates it running (as
/// CreateSilenceTimer leaves the timer of a start with an AutoStopSilenceTimeout) without counting a start.
/// </summary>
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

/// <summary>
/// The microphone input node of <see cref="SpeechFakeAudioEngine"/>: reports a valid format, remembers the tap that the toolkit
/// installs (and can deliver audio buffers to it), and can fail the way installing a tap on a node without a usable input format
/// does. It logs every call into the engine's <see cref="SpeechFakeAudioEngine.Calls"/>.
/// </summary>
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

	/// <summary>When set, InstallTapOnBus throws it (after logging the call).</summary>
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

	/// <summary>Delivers a buffer of silence to the installed tap, as the audio thread would, and returns it.</summary>
	public AVAudioPcmBuffer DeliverBuffer()
	{
		var block = tapBlock ?? throw new InvalidOperationException("The toolkit has not installed a tap (or it was removed).");
		var buffer = new AVAudioPcmBuffer(format, 160) { FrameLength = 160 };
		block(buffer, null!);

		return buffer;
	}
}

/// <summary>
/// An <see cref="AVAudioEngine"/> without audio hardware. It logs <c>prepare</c>, <c>start</c> and <c>stop</c> (and, through its
/// input node, <c>installTap</c> and <c>removeTap</c>) in <see cref="Calls"/> in the order the toolkit makes them, and can fail to
/// start the way a busy or missing audio device does. Once disposed it behaves like a disposed real engine: messages go to a nil
/// handle, so <see cref="InputNode"/> is null and the other calls do nothing.
/// </summary>
public sealed class SpeechFakeAudioEngine : AVAudioEngine
{
	bool isRunning;

	public SpeechFakeAudioEngine() => FakeInputNode = new SpeechFakeInputNode(new AVAudioFormat(16000, 1), Calls);

	public List<string> Calls { get; } = [];

	public SpeechFakeInputNode FakeInputNode { get; }

	/// <summary>When set, StartAndReturnError fails with it.</summary>
	public NSError? StartError { get; set; }

	/// <summary>Runs inside StartAndReturnError (for example to cancel a token at that exact point).</summary>
	public Action? OnStart { get; set; }

	public override AVAudioInputNode InputNode => IsDisposedEngine ? null! : FakeInputNode;

	public override bool Running => isRunning;

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