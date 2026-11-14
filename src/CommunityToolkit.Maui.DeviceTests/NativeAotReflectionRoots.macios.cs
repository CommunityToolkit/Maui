using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace CommunityToolkit.Maui.DeviceTests;

// The tests reach private types and members by reflection. ILLink keeps a member that the product code uses; NativeAOT's ILC emits reflection
// metadata only for what it sees named or annotated, so each private type the tests inspect through GetType() or a type name is listed here.
static class NativeAotReflectionRoots
{
#if MACCATALYST
	const string appleAssembly = "Microsoft.MacCatalyst";
#else
	const string appleAssembly = "Microsoft.iOS";
#endif

	[ModuleInitializer]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, "CommunityToolkit.Maui.Core.CameraManager.PreviewView", "CommunityToolkit.Maui.Camera")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, "CommunityToolkit.Maui.Behaviors.TouchBehavior.TouchUITapGestureRecognizer", "CommunityToolkit.Maui")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicFields, "ObjCRuntime.Runtime", appleAssembly)]
	internal static void KeepTheReflectedTypes()
	{
	}
}