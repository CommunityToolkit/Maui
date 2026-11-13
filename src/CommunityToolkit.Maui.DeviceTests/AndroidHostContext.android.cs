using System.Diagnostics.CodeAnalysis;
using Android.Content;

namespace CommunityToolkit.Maui.DeviceTests;

static class AndroidHostContext
{
	// The setter is private (vendored dotnet/maui code), hence the reflection.
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties, typeof(MauiProgramDefaults))]
	public static void Restore(Context hostContext)
	{
		var property = typeof(MauiProgramDefaults).GetProperty(nameof(MauiProgramDefaults.DefaultContext))
			?? throw new InvalidOperationException("MauiProgramDefaults.DefaultContext was not found.");
		property.SetValue(null, hostContext);
	}
}
