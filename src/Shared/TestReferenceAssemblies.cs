using Microsoft.CodeAnalysis.Testing;

namespace CommunityToolkit.Maui;

// Linked into the analyzer and source generator test projects, so a new .NET version changes the reference assemblies in one place
static class TestReferenceAssemblies
{
#if NET11_0
	// Microsoft.CodeAnalysis.Testing 1.1.4 has no ReferenceAssemblies.Net.Net110 preset; use it once it ships
	public static ReferenceAssemblies Current { get; } = new("net11.0", new PackageIdentity("Microsoft.NETCore.App.Ref", "11.0.0"), Path.Combine("ref", "net11.0"));
#else
#error ReferenceAssemblies must be updated to current version of .NET
#endif
}