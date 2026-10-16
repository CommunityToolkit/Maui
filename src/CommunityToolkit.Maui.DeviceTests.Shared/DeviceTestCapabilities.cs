using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Xunit;
#if WINDOWS
using System.IO;
#endif
#if ANDROID || WINDOWS
using CommunityToolkit.Maui.DeviceTests.Runners.HeadlessRunner;
#endif

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// Lets each CI job say what its runner cannot run while every other run, such as a local script run or the visual runner, executes
/// everything. A capability declared missing (DEVICE_TEST_SKIP_CAPABILITIES) ends its tests as Skipped without probing the machine. A
/// capability declared required (DEVICE_TEST_REQUIRE_CAPABILITIES) fails its tests, instead of skipping them, when the machine turns out
/// not to offer it.
/// </summary>
/// <remarks>
/// A test guards a capability that a CI runner may lack with <see cref="Require(string, bool, string)"/> (or the overload that takes a
/// probe's reason) instead of <see cref="Skip.IfNot"/>, and calls <see cref="SkipIfDeclaredMissing"/> before a probe that is slow or may hang.
/// </remarks>
public static class DeviceTestCapabilities
{
	/// <summary>Windows: Media Foundation creates a MediaPlayer and decodes the H.264 test clip (Windows Server needs an optional feature for it).</summary>
	public const string MediaPlayback = nameof(MediaPlayback);

	/// <summary>Windows: a MediaPlayer renders the H.264 test clip, so it reports the natural video size and plays.</summary>
	public const string VideoRendering = nameof(VideoRendering);

	public const string Camera = nameof(Camera);

	/// <summary>A run that declares <see cref="Camera"/> missing has no second camera either.</summary>
	public const string SecondCamera = nameof(SecondCamera);

	/// <summary>Android: an on-device speech recognition service that answers and has a language installed.</summary>
	public const string OnDeviceSpeech = nameof(OnDeviceSpeech);

	/// <summary>Windows: the WebView2 Runtime, which hosts the map of CommunityToolkit.Maui.Maps.</summary>
	public const string WebView2 = nameof(WebView2);

	const string workflow = ".github/workflows/device-tests.yml";
	const string skipKey = "SkipCapabilities";
	const string requireKey = "RequireCapabilities";
	const string declarationsFileName = "devicetestcapabilities.txt";

	static readonly Lazy<Declarations> declarations = new(ReadDeclarations);

	public static IReadOnlyList<string> DeclaredMissing => declarations.Value.Missing;

	public static IReadOnlyList<string> DeclaredRequired => declarations.Value.Required;

	public static void SkipIfDeclaredMissing(string capability)
	{
		var declared = DeclaredMissing.FirstOrDefault(missing => missing == capability || (capability is SecondCamera && missing is Camera));
		if (declared is not null)
		{
			throw new SkipException($"Not run: this run declares {declared} missing (DEVICE_TEST_SKIP_CAPABILITIES, which the CI sets per job in {workflow}). Runs without the declaration, such as local runs, execute this test.");
		}
	}

	public static void Require(string capability, [DoesNotReturnIf(false)] bool isAvailable, string unavailableReason)
	{
		SkipIfDeclaredMissing(capability);
		if (!isAvailable)
		{
			ReportUnavailable(capability, unavailableReason);
		}
	}

	public static void Require(string capability, string? unavailableReason)
	{
		SkipIfDeclaredMissing(capability);
		if (unavailableReason is not null)
		{
			ReportUnavailable(capability, unavailableReason);
		}
	}

	/// <summary>
	/// A misspelled capability name would silently skip or require nothing, so the DeviceTestsInfrastructure tests of every app
	/// fail on any problem.
	/// </summary>
	public static IReadOnlyList<string> GetDeclarationProblems()
	{
		var known = typeof(DeviceTestCapabilities).GetFields(BindingFlags.Public | BindingFlags.Static)
			.Where(static field => field.IsLiteral && field.FieldType == typeof(string))
			.Select(static field => (string)field.GetRawConstantValue()!)
			.ToHashSet(StringComparer.Ordinal);

		var problems = new List<string>();
		foreach (var (list, names) in new[] { ("DEVICE_TEST_SKIP_CAPABILITIES", DeclaredMissing), ("DEVICE_TEST_REQUIRE_CAPABILITIES", DeclaredRequired) })
		{
			problems.AddRange(names.Where(name => !known.Contains(name)).Select(name => $"{list} names '{name}', which is not a capability. Known: {string.Join(", ", known.Order(StringComparer.Ordinal))}."));
		}

		problems.AddRange(DeclaredMissing.Intersect(DeclaredRequired, StringComparer.Ordinal).Select(static name => $"'{name}' is declared both missing and required."));

		return problems;
	}

	[DoesNotReturn]
	static void ReportUnavailable(string capability, string reason)
	{
		if (DeclaredRequired.Contains(capability, StringComparer.Ordinal))
		{
			Assert.Fail($"This run requires {capability} (DEVICE_TEST_REQUIRE_CAPABILITIES, which the CI sets per job in {workflow}), but this machine does not offer it: {reason}");
		}

		throw new SkipException(reason);
	}

	static Declarations ReadDeclarations()
	{
		string? missing = null, required = null;

#if ANDROID
		var arguments = MauiTestInstrumentation.Current?.Arguments;
		missing = arguments?.GetString(skipKey);
		required = arguments?.GetString(requireKey);
#elif IOS || MACCATALYST
		missing = Environment.GetEnvironmentVariable(skipKey);
		required = Environment.GetEnvironmentVariable(requireKey);
#elif WINDOWS
		// A packaged app does not inherit the environment of the script that starts it, so windows.ps1 writes the lists to a file next to
		// the results file. A headless run gets the results file as its first argument; the visual runner gets none.
		var resultsFile = HeadlessTestRunner.TestResultsFile;
		var declarationsFile = string.IsNullOrEmpty(resultsFile) ? null : Path.Combine(Path.GetDirectoryName(resultsFile) ?? string.Empty, declarationsFileName);
		if (declarationsFile is not null && File.Exists(declarationsFile))
		{
			foreach (var line in File.ReadAllLines(declarationsFile))
			{
				var separator = line.IndexOf('=', StringComparison.Ordinal);
				switch (separator < 0 ? string.Empty : line[..separator].Trim())
				{
					case skipKey:
						missing = line[(separator + 1)..];
						break;
					case requireKey:
						required = line[(separator + 1)..];
						break;
				}
			}
		}
#endif

		var result = new Declarations(Split(missing), Split(required));
		Console.WriteLine($"Device tests declared capabilities: missing [{string.Join(", ", result.Missing)}], required [{string.Join(", ", result.Required)}]");

		return result;
	}

	static string[] Split(string? list) =>
		list is null ? [] : [.. list.Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct(StringComparer.Ordinal)];

	sealed record Declarations(IReadOnlyList<string> Missing, IReadOnlyList<string> Required);
}