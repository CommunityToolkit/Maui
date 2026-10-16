using System.Runtime.InteropServices;
using Foundation;
using MediaPlayer;
using ObjCRuntime;

namespace CommunityToolkit.Maui.DeviceTests;

// MPRemoteCommandCenter is process-wide state: every Metadata that any test (or any MediaManager) ever created keeps its targets there,
// so tests start from no targets and leave none behind.
static class AppleRemoteCommands
{
	static readonly Selector hasTargetsSelector = new("hasTargets");
	static readonly Selector newCommandEventSelector = new("newCommandEvent");
	static readonly Selector invokeCommandSelector = new("invokeCommandWithEvent:completion:");

	static int completions;

	// MediaPlayer requires a completion block (a nil block aborts the app). NSBlockOperation turns a managed Action into a real
	// Objective-C block without unsafe code; the command center passes it the handler status, which the Action ignores.
	static readonly NSBlockOperation completionOperation = NSBlockOperation.Create(static () => Interlocked.Increment(ref completions));

	public static readonly string[] Names = ["togglePlayPause", "play", "pause", "changePlaybackPosition", "seekBackward", "seekForward"];

	public static MPRemoteCommand Get(string name)
	{
		var center = MPRemoteCommandCenter.Shared;
		return name switch
		{
			"togglePlayPause" => center.TogglePlayPauseCommand,
			"play" => center.PlayCommand,
			"pause" => center.PauseCommand,
			"changePlaybackPosition" => center.ChangePlaybackPositionCommand,
			"seekBackward" => center.SeekBackwardCommand,
			"seekForward" => center.SeekForwardCommand,
			"nextTrack" => center.NextTrackCommand,
			_ => throw new ArgumentOutOfRangeException(nameof(name), name, null)
		};
	}

	public static void RemoveAllTargets()
	{
		foreach (var name in Names)
		{
			// A null target removes every target of the command.
			Get(name).RemoveTarget(null);
		}
	}

	// MediaPlayer has no public way to ask a command for its targets; the hasTargets getter of MPRemoteCommand is what
	// MPRemoteCommandCenter itself uses to decide which commands to advertise.
	public static bool CanQueryTargets => MPRemoteCommandCenter.Shared.PlayCommand.RespondsToSelector(hasTargetsSelector);

	public static bool HasTargets(string name) => SendBoolMessage(Get(name).Handle, hasTargetsSelector.Handle) != 0;

	// MediaPlayer's own (non-public) entry points: the system delivers a remote command with -[MPRemoteCommand newCommandEvent] and
	// -[MPRemoteCommand invokeCommandWithEvent:completion:], so tests can check which handler each command reaches with a real event.
	public static bool CanDispatch =>
		MPRemoteCommandCenter.Shared.PlayCommand.RespondsToSelector(newCommandEventSelector)
		&& MPRemoteCommandCenter.Shared.PlayCommand.RespondsToSelector(invokeCommandSelector);

	public static int Completions => Volatile.Read(ref completions);

	// Main thread only.
	public static int Dispatch(MPRemoteCommand command)
	{
		var before = Completions;
		var completion = completionOperation.ExecutionBlocks[0];
		using var commandEvent = Runtime.GetNSObject<MPRemoteCommandEvent>(SendIntPtrMessage(command.Handle, newCommandEventSelector.Handle), owns: true)
			?? throw new InvalidOperationException($"{command} created no command event.");

		SendVoidMessage(command.Handle, invokeCommandSelector.Handle, commandEvent.Handle, completion.Handle);

		GC.KeepAlive(completion);
		return before;
	}

	[DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
	static extern byte SendBoolMessage(IntPtr receiver, IntPtr selector);

	[DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
	static extern IntPtr SendIntPtrMessage(IntPtr receiver, IntPtr selector);

	[DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
	static extern void SendVoidMessage(IntPtr receiver, IntPtr selector, IntPtr argument1, IntPtr argument2);
}