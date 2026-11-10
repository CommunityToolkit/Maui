using System.Reflection;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests;

/// <summary>
/// Guards #3331's "runtime async on the Windows TFM only" scoping.
///
/// .NET 11 "runtime async" (C# 15 / .NET 11) sets bit 0x2000 on the MethodImplAttributes of every async
/// method (a.k.a. MethodImplOptions.Async). It is a CoreCLR-only preview feature: Mono (Android/iOS with
/// UseMonoRuntime=true, and Blazor WebAssembly) cannot execute runtime-async IL - it throws
/// PlatformNotSupportedException, returns a task that never completes, or throws InvalidProgramException.
///
/// #3331 enables runtime async ONLY on the net11.0-windows TFM (CoreCLR), via the Windows-TFM-scoped
/// PropertyGroup in Directory.Build.targets. The bare net11.0 asset - which a Blazor WebAssembly app or a
/// UseMonoRuntime=true MAUI app consumes - must therefore contain no runtime-async methods. These tests scan
/// the shipped library assemblies the UnitTests link against (bare net11.0) and assert none carry the flag,
/// so a future widening of the TFM condition is caught. The positive proof (the net11.0-windows library
/// assembly HAS &gt;= 1 runtime-async method) is done on a Windows build (fork CI / the PR Windows device-test
/// job), not here, because the bare net11.0 test assembly is intentionally built without runtime async.
/// </summary>
public class Tests_RuntimeAsync_ShippedAssemblies
{
	const BindingFlags allDeclared =
		BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

	[Theory]
	[InlineData("CommunityToolkit.Maui")]
	[InlineData("CommunityToolkit.Maui.Core")]
	[InlineData("CommunityToolkit.Maui.Camera")]
	[InlineData("CommunityToolkit.Maui.MediaElement")]
	public void ShippedAssembly_HasNoRuntimeAsyncMethods(string assemblyName)
	{
		var assembly = Assembly.Load(assemblyName);
		var runtimeAsyncMethods = FindRuntimeAsyncMethods(assembly);

		runtimeAsyncMethods.Should().BeEmpty(
			because: $"'{assemblyName}' is the Mono-capable bare net11.0 asset and must not contain runtime-async IL. " +
					 "Runtime async is enabled only on the net11.0-windows TFM (CoreCLR-only).");
	}

	static IEnumerable<string> FindRuntimeAsyncMethods(Assembly assembly)
	{
		Type[] types;
		try
		{
			types = assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException ex)
		{
			// Some types reference assemblies that are not loadable in the test context;
			// scan the ones that did load rather than failing the whole scan.
			types = [.. ex.Types.OfType<Type>()];
		}

		foreach (var type in types)
		{
			var members = type.GetMethods(allDeclared).Cast<MethodBase>()
				.Concat(type.GetConstructors(allDeclared).Cast<MethodBase>());

			foreach (var method in members)
			{
				if ((method.GetMethodImplementationFlags() & MethodImplAttributes.Async) != 0)
				{
					yield return $"{type.FullName}.{method.Name}";
				}
			}
		}
	}
}
