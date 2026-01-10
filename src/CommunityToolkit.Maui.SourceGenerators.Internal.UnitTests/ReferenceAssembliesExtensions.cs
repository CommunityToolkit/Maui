using Microsoft.CodeAnalysis.Testing;

namespace CommunityToolkit.Maui.SourceGenerators.Internal.UnitTests;

static class ReferenceAssembliesExtensions
{
	static readonly Lazy<ReferenceAssemblies> lazyNet100 = new(() =>
		new(targetFramework: "net10.0",
			referenceAssemblyPackage: new PackageIdentity("Microsoft.NETCore.App.Ref", "10.0.0-rc.2.25502.107"),
			referenceAssemblyPath: Path.Combine("ref", "net10.0")));

	extension(ReferenceAssemblies.Net)
	{
		public static ReferenceAssemblies Net100 => lazyNet100.Value;
	}
}