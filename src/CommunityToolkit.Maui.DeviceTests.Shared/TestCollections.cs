using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// xUnit collection names shared by the device-test apps.
/// </summary>
public static class TestCollections
{
	/// <summary>
	/// <c>[Collection(TestCollections.DisableParallelization)]</c> runs the class after all other tests and never in parallel
	/// with anything else. Use it for classes that host views in the window, change app-wide state (toolkit options, status bar,
	/// Application.Current, Preferences, permissions, files at fixed paths) or depend on focus. The XHarness headless runner
	/// already runs every test sequentially; this protects the visual runner, which runs collections in parallel.
	/// </summary>
	/// <remarks>
	/// xUnit v2 discovers collection definitions only in the test assembly, so every device-test app declares
	/// <c>[CollectionDefinition(TestCollections.DisableParallelization, DisableParallelization = true)]</c>.
	/// </remarks>
	public const string DisableParallelization = nameof(DisableParallelization);

	/// <summary>
	/// True when <paramref name="assembly"/> declares the <see cref="DisableParallelization"/> collection with
	/// <c>DisableParallelization = true</c>.
	/// </summary>
	public static bool DeclaresDisableParallelizationCollection(Assembly assembly) =>
		assembly.GetTypes().Any(static type => type.GetCustomAttributesData().Any(static attribute =>
			attribute.AttributeType == typeof(CollectionDefinitionAttribute)
			&& attribute.ConstructorArguments.FirstOrDefault().Value is DisableParallelization
			&& attribute.NamedArguments.Any(static argument => argument.MemberName == nameof(CollectionDefinitionAttribute.DisableParallelization) && argument.TypedValue.Value is true)));

	/// <summary>
	/// Throws unless <paramref name="testClass"/> is in the <see cref="DisableParallelization"/> collection and its assembly
	/// declares that collection.
	/// </summary>
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