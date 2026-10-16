using System.Diagnostics;

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// Collects what the toolkit writes with <see cref="Trace"/> while it is registered: StatusBar.ios.cs and NavigationBar.android.cs report
/// "not possible here" situations as trace lines instead of exceptions.
/// </summary>
sealed class CapturingTraceListener : TraceListener
{
	readonly Lock gate = new();
	readonly List<string> messages = [];

	public IReadOnlyList<string> Messages
	{
		get
		{
			lock (gate)
			{
				return [.. messages];
			}
		}
	}

	public override void Write(string? message) => Add(message);

	public override void WriteLine(string? message) => Add(message);

	/// <summary>Registers the listener, runs <paramref name="action"/> and unregisters it again.</summary>
	public static CapturingTraceListener Capture(Action action)
	{
		var listener = new CapturingTraceListener();
		Trace.Listeners.Add(listener);
		try
		{
			action();
		}
		finally
		{
			Trace.Listeners.Remove(listener);
		}

		return listener;
	}

	void Add(string? message)
	{
		lock (gate)
		{
			messages.Add(message ?? string.Empty);
		}
	}
}