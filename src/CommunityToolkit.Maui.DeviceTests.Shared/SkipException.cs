using System;
using System.Diagnostics.CodeAnalysis;

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// Thrown by a test to end it as Skipped (not Passed) at run time, e.g. when the device's OS version has no implementation
/// of the branch under test. Tests declared with this assembly's <see cref="FactAttribute"/>/<see cref="TheoryAttribute"/>
/// report it as Skipped with <see cref="Exception.Message"/> as the reason; with xUnit's own attributes it is a failure.
/// </summary>
public sealed class SkipException(string reason) : Exception(reason);

/// <summary>
/// Run-time skips that the results report as Skipped. Use them only when the test cannot run on this device; when the
/// platform code takes a different branch on this device, assert that branch's behavior instead.
/// </summary>
public static class Skip
{
	/// <summary>
	/// Ends the test as Skipped when <paramref name="condition"/> is true.
	/// </summary>
	public static void If([DoesNotReturnIf(true)] bool condition, string reason)
	{
		if (condition)
		{
			throw new SkipException(reason);
		}
	}

	/// <summary>
	/// Ends the test as Skipped when <paramref name="condition"/> is false.
	/// </summary>
	public static void IfNot([DoesNotReturnIf(false)] bool condition, string reason) => If(!condition, reason);
}