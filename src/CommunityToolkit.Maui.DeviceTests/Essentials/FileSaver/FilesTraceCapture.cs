using System.Diagnostics;

namespace CommunityToolkit.Maui.DeviceTests;

public sealed class FilesTraceCapture : TraceListener
{
	readonly Lock gate = new();
	readonly List<string> lines = [];

	public FilesTraceCapture() => Trace.Listeners.Add(this);

	public IReadOnlyList<string> Lines
	{
		get
		{
			lock (gate)
			{
				return [.. lines];
			}
		}
	}

	public bool Contains(string text) => Lines.Any(line => line.Contains(text, StringComparison.Ordinal));

	public override void Write(string? message)
	{
		lock (gate)
		{
			lines.Add(message ?? string.Empty);
		}
	}

	public override void WriteLine(string? message) => Write(message);

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			Trace.Listeners.Remove(this);
		}

		base.Dispose(disposing);
	}
}

// Records synchronously, because Progress<T> would post the values to a context.
public sealed class FilesRecordingProgress : IProgress<double>
{
	readonly Lock gate = new();
	readonly List<double> values = [];

	public IReadOnlyList<double> Values
	{
		get
		{
			lock (gate)
			{
				return [.. values];
			}
		}
	}

	// A test cancels a token from here: the toolkit reports a block after writing it and before it reads the next one.
	public Action<double>? OnReport { get; init; }

	public void Report(double value)
	{
		lock (gate)
		{
			values.Add(value);
		}

		OnReport?.Invoke(value);
	}
}