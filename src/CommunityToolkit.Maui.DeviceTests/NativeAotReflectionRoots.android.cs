using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace CommunityToolkit.Maui.DeviceTests;

// The tests reach private nested types of the toolkit through GetNestedType, which ILLink keeps for what the product uses but NativeAOT's ILC
// emits reflection metadata for only when it sees them named or annotated. Types reached through a literal typeof(X).GetField("name") need no
// root, because ILC sees that call.
static class NativeAotReflectionRoots
{
	const DynamicallyAccessedMemberTypes constructorsMethodsFieldsAndProperties =
		DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors
		| DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods
		| DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields
		| DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties;

	[ModuleInitializer]
	[DynamicDependency(constructorsMethodsFieldsAndProperties, "CommunityToolkit.Maui.Core.CameraManager.ImageCallBack", "CommunityToolkit.Maui.Camera")]
	[DynamicDependency(constructorsMethodsFieldsAndProperties, "CommunityToolkit.Maui.Core.CameraManager.ResolutionFilter", "CommunityToolkit.Maui.Camera")]
	[DynamicDependency(constructorsMethodsFieldsAndProperties, "CommunityToolkit.Maui.Behaviors.TouchBehavior.AccessibilityListener", "CommunityToolkit.Maui")]
	[DynamicDependency(constructorsMethodsFieldsAndProperties, "CommunityToolkit.Maui.Core.StreamDataSourceFactory.StreamDataSource", "CommunityToolkit.Maui.MediaElement")]
	[DynamicDependency(constructorsMethodsFieldsAndProperties, "CommunityToolkit.Maui.Media.SpeechToTextImplementation.SpeechRecognitionListener", "CommunityToolkit.Maui.Core")]
	[DynamicDependency(constructorsMethodsFieldsAndProperties, "CommunityToolkit.Maui.Media.OfflineSpeechToTextImplementation.SpeechRecognitionListener", "CommunityToolkit.Maui.Core")]
	internal static void KeepTheReflectedTypes()
	{
	}
}