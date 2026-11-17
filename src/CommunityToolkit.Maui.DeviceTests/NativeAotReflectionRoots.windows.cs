using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace CommunityToolkit.Maui.DeviceTests;

// Under TrimMode=full, ILLink removes the getters of MAUI's ItemTemplateContext and GroupTemplateContext, which WinUI reaches only by
// reflection through {Binding} in ItemsViewDefaultTemplate, so the legacy CollectionView never realizes a templated item (dotnet/maui#31227).
// These tests use the legacy handler (UseWindowsCollectionView2Handler=false in the project file).
static class NativeAotReflectionRoots
{
	[ModuleInitializer]
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(Microsoft.Maui.Controls.Platform.ItemTemplateContext))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, "Microsoft.Maui.Controls.Platform.GroupTemplateContext", "Microsoft.Maui.Controls")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicFields, "CommunityToolkit.Maui.Media.SpeechToTextImplementation.SpeechRecognizerLifetime", "CommunityToolkit.Maui.Core")]
	internal static void KeepTheReflectedTypes()
	{
	}
}