using System.Runtime.InteropServices;
using Foundation;
using MediaPlayer;
using ObjCRuntime;

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// The remote commands (lock screen, headphones, Control Center) of the shared <see cref="MPRemoteCommandCenter"/> that the toolkit's
/// <c>Metadata</c> class registers targets on. The command center is process-wide state: every <c>Metadata</c> that any test (or any
/// MediaManager) ever created keeps its targets there, so tests start from no targets and leave none behind.
/// </summary>
static class AppleRemoteCommands
{
	static readonly Selector hasTargetsSelector = new("hasTargets");
	static readonly Selector newCommandEventSelector = new("newCommandEvent");
	static readonly Selector invokeCommandSelector = new("invokeCommandWithEvent:completion:");

	static int completions;

	// MediaPlayer requires a completion block (a nil block aborts the app). NSBlockOperation turns a managed Action into a real
	// Objective-C block without unsafe code; the command center calls it with the handler status as an argument, which it ignores.
	static readonly NSBlockOperation completionOperation = NSBlockOperation.Create(static () => Interlocked.Increment(ref completions));

	/// <summary>The remote command names the tests use, as they are called on <see cref="MPRemoteCommandCenter"/>.</summary>
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

	/// <summary>Removes every target of the commands the toolkit uses (a null target removes all targets of a command).</summary>
	public static void RemoveAllTargets()
	{
		foreach (var name in Names)
		{
			Get(name).RemoveTarget(null);
		}
	}

	/// <summary>
	/// MediaPlayer has no public way to ask a command for its targets; the <c>hasTargets</c> getter of <see cref="MPRemoteCommand"/> is
	/// what <see cref="MPRemoteCommandCenter"/> itself uses to decide which commands to advertise.
	/// </summary>
	public static bool CanQueryTargets => MPRemoteCommandCenter.Shared.PlayCommand.RespondsToSelector(hasTargetsSelector);

	public static bool HasTargets(string name) => SendBoolMessage(Get(name).Handle, hasTargetsSelector.Handle) != 0;

	/// <summary>
	/// The system delivers a remote command (lock screen, headphones, Control Center) by creating an event of the command and invoking
	/// the command's targets with it: <c>-[MPRemoteCommand newCommandEvent]</c> and <c>-[MPRemoteCommand invokeCommandWithEvent:completion:]</c>.
	/// Both are MediaPlayer's own (non-public) entry points, so tests can check which handler each command reaches with a real event.
	/// </summary>
	public static bool CanDispatch =>
		MPRemoteCommandCenter.Shared.PlayCommand.RespondsToSelector(newCommandEventSelector)
		&& MPRemoteCommandCenter.Shared.PlayCommand.RespondsToSelector(invokeCommandSelector);

	/// <summary>How often the command center reported that a dispatched event was handled.</summary>
	public static int Completions => Volatile.Read(ref completions);

	/// <summary>Sends a new event of the command to its targets and returns <see cref="Completions"/> from before the call. Main thread only.</summary>
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