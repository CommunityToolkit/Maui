using System.Diagnostics;

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// Collects what the toolkit writes with <c>Trace.WriteLine</c> (it reports problems that it survives, such as a launcher without
/// badge support) for as long as the object lives. Dispose it to stop listening.
/// </summary>
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

/// <summary>Records every value reported to it, in order, synchronously (Progress&lt;T&gt; would post to a context).</summary>
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

	/// <summary>
	/// Runs after each value was recorded, on the thread that reported it. A test cancels a token from here: the toolkit reports a block
	/// after writing it and before it reads the next one.
	/// </summary>
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