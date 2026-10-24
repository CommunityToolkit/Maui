using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace CommunityToolkit.Maui.DeviceTests;

public static class TestCollections
{
	/// <summary>
	/// Use for classes that host views in the window, change app-wide state (toolkit options, status bar, Application.Current,
	/// Preferences, permissions, files at fixed paths) or depend on focus. The XHarness headless runner already runs every test
	/// sequentially; this protects the visual runner, which runs collections in parallel.
	/// </summary>
	/// <remarks>
	/// xUnit v2 discovers collection definitions only in the test assembly, so every device-test app declares
	/// <c>[CollectionDefinition(TestCollections.DisableParallelization, DisableParallelization = true)]</c>.
	/// </remarks>
	public const string DisableParallelization = nameof(DisableParallelization);

	public static bool DeclaresDisableParallelizationCollection(Assembly assembly) =>
		assembly.GetTypes().Any(static type => type.GetCustomAttributesData().Any(static attribute =>
			attribute.AttributeType == typeof(CollectionDefinitionAttribute)
			&& attribute.ConstructorArguments.FirstOrDefault().Value is DisableParallelization
			&& attribute.NamedArguments.Any(static argument => argument.MemberName == nameof(CollectionDefinitionAttribute.DisableParallelization) && argument.TypedValue.Value is true)));

	public static void EnsureInDisableParallelizationCollection(Type testClass, string reason)
	{
		var collectionName = testClass.GetCustomAttributesData()
			.FirstOrDefault(static attribute => attribute.AttributeType == typeof(CollectionAttribute))?
			.ConstructorArguments.FirstOrDefault().Value as string;

		if (collectionName is not DisableParallelization)
		{
			throw new InvalidOperationException($"{testClass.FullName} {reason}, so it must be declared with [Collection(TestCollections.{nameof(DisableParallelization)})].");
		}

		if (!DeclaresDisableParallelizationCollection(testClass.Assembly))
		{
			throw new InvalidOperationException($"{testClass.Assembly.GetName().Name} must declare [CollectionDefinition(TestCollections.{nameof(DisableParallelization)}, DisableParallelization = true)].");
		}
	}
}