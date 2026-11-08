using System;
using System.Diagnostics.CodeAnalysis;

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// Reported as Skipped, with <see cref="Exception.Message"/> as the reason, only by tests declared with this assembly's
/// <see cref="FactAttribute"/>/<see cref="TheoryAttribute"/>; with xUnit's own attributes it is a failure.
/// </summary>
public sealed class SkipException(string reason) : Exception(reason);

/// <summary>
/// Use only when the test cannot run on this device. When the platform code takes a different branch on this device, assert that
/// branch's behavior instead.
/// </summary>
public static class Skip
{
	public static void If([DoesNotReturnIf(true)] bool condition, string reason)
	{
		if (condition)
		{
			throw new SkipException(reason);
		}
	}

	public static void IfNot([DoesNotReturnIf(false)] bool condition, string reason) => If(!condition, reason);
}