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

	/// <summary>
	/// Android: the classes whose tests grant runtime permissions with <c>pm grant</c> (CameraAndroidTestBase). The app keeps a grant
	/// until XHarness uninstalls it, because revoking one kills the process. PermanentGrantsLastOrderer runs this collection after every
	/// other one, so the tests that need RECORD_AUDIO denied (SpeechToText) run first.
	/// </summary>
	public const string PermanentAndroidGrants = nameof(PermanentAndroidGrants);

	public static bool DeclaresDisableParallelizationCollection(Assembly assembly) => DeclaresCollection(assembly, DisableParallelization);

	public static void EnsureInDisableParallelizationCollection(Type testClass, string reason) =>
		EnsureInCollection(testClass, DisableParallelization, reason);

	public static void EnsureInPermanentAndroidGrantsCollection(Type testClass) =>
		EnsureInCollection(testClass, PermanentAndroidGrants, "grants runtime permissions that the app keeps until it is uninstalled");

	static bool DeclaresCollection(Assembly assembly, string name) =>
		assembly.GetTypes().Any(type => type.GetCustomAttributesData().Any(attribute =>
			attribute.AttributeType == typeof(CollectionDefinitionAttribute)
			&& attribute.ConstructorArguments.FirstOrDefault().Value as string == name
			&& attribute.NamedArguments.Any(static argument => argument.MemberName == nameof(CollectionDefinitionAttribute.DisableParallelization) && argument.TypedValue.Value is true)));

	static void EnsureInCollection(Type testClass, string collection, string reason)
	{
		var collectionName = testClass.GetCustomAttributesData()
			.FirstOrDefault(static attribute => attribute.AttributeType == typeof(CollectionAttribute))?
			.ConstructorArguments.FirstOrDefault().Value as string;

		if (collectionName != collection)
		{
			throw new InvalidOperationException($"{testClass.FullName} {reason}, so it must be declared with [Collection(TestCollections.{collection})].");
		}

		if (!DeclaresCollection(testClass.Assembly, collection))
		{
			throw new InvalidOperationException($"{testClass.Assembly.GetName().Name} must declare [CollectionDefinition(TestCollections.{collection}, DisableParallelization = true)].");
		}
	}
}