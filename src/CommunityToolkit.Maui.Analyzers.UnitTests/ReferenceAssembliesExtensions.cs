using Microsoft.CodeAnalysis.Testing;

namespace CommunityToolkit.Maui.Analyzers.UnitTests;

static class ReferenceAssembliesExtensions
{
	extension(ReferenceAssemblies.Net)
	{
		public static ReferenceAssemblies Net100 =>
			new(targetFramework: "net10.0",
				referenceAssemblyPackage: new PackageIdentity("Microsoft.NETCore.App.Ref", "10.0.0-rc.2.25502.107"),
				referenceAssemblyPath: Path.Combine("ref", "net10.0"));
	}
}