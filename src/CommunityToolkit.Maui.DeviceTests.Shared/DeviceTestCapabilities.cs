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
/// Machine capabilities that a headless run declares, so that each CI job says what its runner cannot run while every other run executes
/// everything. A run declares capabilities missing (DEVICE_TEST_SKIP_CAPABILITIES): their tests end as Skipped without probing the machine.
/// It declares capabilities required (DEVICE_TEST_REQUIRE_CAPABILITIES): their tests fail, instead of skipping, when the machine turns out not
/// to offer them. The device test jobs of .github/workflows/device-tests.yml set both lists, and eng/devices/*.ps1 pass them to the app the way
/// they pass TestFilter: an instrumentation argument on Android, an environment variable on iOS and Mac Catalyst, and a file next to the
/// results file on Windows. Runs without the lists, such as local script runs and the visual runner, skip nothing and require nothing.
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

	/// <summary>A camera that the platform's capture APIs find.</summary>
	public const string Camera = nameof(Camera);

	/// <summary>A second camera to switch to. A run that declares <see cref="Camera"/> missing has no second camera either.</summary>
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

	/// <summary>The capabilities this run declares missing.</summary>
	public static IReadOnlyList<string> DeclaredMissing => declarations.Value.Missing;

	/// <summary>The capabilities this run requires.</summary>
	public static IReadOnlyList<string> DeclaredRequired => declarations.Value.Required;

	/// <summary>
	/// Ends the test as Skipped when this run declares <paramref name="capability"/> missing, and does nothing otherwise.
	/// </summary>
	public static void SkipIfDeclaredMissing(string capability)
	{
		var declared = DeclaredMissing.FirstOrDefault(missing => missing == capability || (capability is SecondCamera && missing is Camera));
		if (declared is not null)
		{
			throw new SkipException($"Not run: this run declares {declared} missing (DEVICE_TEST_SKIP_CAPABILITIES, which the CI sets per job in {workflow}). Runs without the declaration, such as local runs, execute this test.");
		}
	}

	/// <summary>
	/// Lets the test go on when <paramref name="isAvailable"/> is true. Otherwise the test fails when this run requires
	/// <paramref name="capability"/>, and ends as Skipped with <paramref name="unavailableReason"/> when it does not. A run that declares
	/// the capability missing skips without looking at <paramref name="isAvailable"/>.
	/// </summary>
	public static void Require(string capability, [DoesNotReturnIf(false)] bool isAvailable, string unavailableReason)
	{
		SkipIfDeclaredMissing(capability);
		if (!isAvailable)
		{
			ReportUnavailable(capability, unavailableReason);
		}
	}

	/// <summary>
	/// <see cref="Require(string, bool, string)"/> for a probe that answers with the reason why <paramref name="capability"/> is
	/// unavailable, or with null when it is available.
	/// </summary>
	public static void Require(string capability, string? unavailableReason)
	{
		SkipIfDeclaredMissing(capability);
		if (unavailableReason is not null)
		{
			ReportUnavailable(capability, unavailableReason);
		}
	}

	/// <summary>
	/// What is wrong with this run's declarations, one line per problem: a name that is not a capability of this class, or a capability
	/// that is declared both missing and required. The DeviceTestsInfrastructure tests of every app fail on any.
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
		// A packaged app does not inherit the environment of the script that starts it, so windows.ps1 writes the lists, one "key=value" line
		// each, to a file next to the results file. A headless run gets the results file as its first argument; the visual runner gets none.
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