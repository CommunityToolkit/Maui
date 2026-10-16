// Not part of dotnet/maui: dotnet/maui collects no code coverage from its device tests. The coverage runs of eng/devices
// (-Coverage, see eng/devices/README.md) instrument the toolkit assemblies with coverlet and read what this class writes.
#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace CommunityToolkit.Maui.DeviceTests.Runners;

/// <summary>
/// Writes the code coverage that coverlet-instrumented assemblies recorded during a headless test run.
/// </summary>
/// <remarks>
/// coverlet adds a tracker class to every assembly it instruments, and the tracker writes the module's hits file from a
/// ProcessExit or DomainUnload handler. Those never run in the Android, iOS and Mac Catalyst test apps, which end without a
/// managed process exit, so the headless runners call <see cref="Flush"/> once the tests have run. In a run without
/// instrumented assemblies it writes nothing.
/// </remarks>
public static class CoverageRecorder
{
	/// <summary>
	/// The environment variable (iOS and Mac Catalyst) or instrumentation argument (Android) that names the directory receiving the
	/// hits files. The scripts set it only for coverage runs.
	/// </summary>
	public const string DirectoryVariable = "CTM_COVERAGE_DIRECTORY";

	const string trackerNamespace = "Coverlet.Core.Instrumentation.Tracker";

	/// <summary>
	/// Writes the hits file of every coverlet-instrumented assembly loaded in this process to <paramref name="directory"/>, under the
	/// file name that coverlet expects. Call it once, after the tests have run.
	/// </summary>
	/// <param name="directory">The directory to write to, created if needed; <see langword="null"/> or empty when this is no coverage run.</param>
	/// <returns>A line for the test log, or <see langword="null"/> when <paramref name="directory"/> is <see langword="null"/> or empty.</returns>
	/// <remarks>It never throws: a failure is described in the returned line, and the script that reads the files reports the missing coverage.</remarks>
	public static string? Flush(string? directory)
	{
		if (string.IsNullOrEmpty(directory))
		{
			return null;
		}

		try
		{
			// The directory exists even when no instrumented assembly is loaded: on Android, xharness fails a run whose device output
			// folder is missing.
			Directory.CreateDirectory(directory);

			var flushed = 0;
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(static assembly => !assembly.IsDynamic))
			{
				foreach (var tracker in GetTypes(assembly).Where(static type => type.Namespace == trackerNamespace))
				{
					// coverlet's ModuleTrackerTemplate: the static constructor sets HitsFilePath (a path on the machine that instrumented the
					// assembly) and HitsArray, and UnloadModule(object, EventArgs) writes HitsArray to HitsFilePath once (next to a .lock file),
					// adding it to a hits file that already exists.
					var hitsFilePath = tracker.GetField("HitsFilePath", BindingFlags.Public | BindingFlags.Static);
					var unloadModule = tracker.GetMethod("UnloadModule", BindingFlags.Public | BindingFlags.Static);
					if (hitsFilePath is null || unloadModule is null)
					{
						continue;
					}

					RuntimeHelpers.RunClassConstructor(tracker.TypeHandle);
					if (hitsFilePath.GetValue(null) is not string path)
					{
						continue;
					}

					hitsFilePath.SetValue(null, Path.Combine(directory, Path.GetFileName(path)));
					unloadModule.Invoke(null, [null, EventArgs.Empty]);
					flushed++;
				}
			}

			return $"Coverage: wrote {flushed} hits file(s) to '{directory}'.";
		}
		catch (Exception e)
		{
			return $"Coverage: writing the hits files to '{directory}' failed: {e}";
		}
	}

	static Type[] GetTypes(Assembly assembly)
	{
		try
		{
			return assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException e)
		{
			return e.Types.OfType<Type>().ToArray();
		}
	}
}