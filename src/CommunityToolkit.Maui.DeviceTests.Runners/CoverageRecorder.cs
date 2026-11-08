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
/// coverlet adds a tracker class to every assembly it instruments, and the tracker writes the module's hits file from a
/// ProcessExit or DomainUnload handler. Those never run in the Android, iOS and Mac Catalyst test apps, which end without a
/// managed process exit, so the headless runners call <see cref="Flush"/> once the tests have run.
/// </summary>
public static class CoverageRecorder
{
	public const string DirectoryVariable = "CTM_COVERAGE_DIRECTORY";

	const string trackerNamespace = "Coverlet.Core.Instrumentation.Tracker";

	/// <summary>
	/// Call it once, after the tests have run. It never throws: a failure is described in the returned line, and the script that
	/// reads the hits files reports the missing coverage.
	/// </summary>
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
					// coverlet's ModuleTrackerTemplate: the static constructor sets HitsFilePath, a path on the machine that instrumented the
					// assembly, so it is redirected to the given directory; UnloadModule(object, EventArgs) then writes the hits there only
					// once, adding them to a hits file that already exists.
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