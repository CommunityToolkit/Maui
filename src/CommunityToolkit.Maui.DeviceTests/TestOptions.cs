using System.Reflection;

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// Configuration options for the device test runner.
/// Mirrors the pattern used by the .NET MAUI team's TestUtils.DeviceTests.Runners.
/// </summary>
public sealed class TestOptions
{
	/// <summary>
	/// The list of assemblies that contain tests.
	/// </summary>
	public List<Assembly> Assemblies { get; set; } = [];

	/// <summary>
	/// The list of categories to skip in the form: [category-name]=[skip-when-value]
	/// </summary>
	public List<string> SkipCategories { get; set; } = [];
}
