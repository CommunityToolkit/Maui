using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace CommunityToolkit.Maui.DeviceTests;

// xUnit's default order is a hash of each collection's UniqueID (DefaultTestCollectionOrderer), so the PermanentAndroidGrants
// collection could run before the SpeechToText tests that need RECORD_AUDIO denied. This keeps the default order of every other
// collection and moves that one to the end. xUnit creates it by name from [assembly: TestCollectionOrderer].
public sealed class PermanentGrantsLastOrderer : ITestCollectionOrderer
{
	public const string TypeName = "CommunityToolkit.Maui.DeviceTests.PermanentGrantsLastOrderer";
	public const string AssemblyName = "CommunityToolkit.Maui.DeviceTests.Shared";

	readonly DefaultTestCollectionOrderer defaultOrderer = new();

	public IEnumerable<ITestCollection> OrderTestCollections(IEnumerable<ITestCollection> testCollections)
	{
		var ordered = defaultOrderer.OrderTestCollections(testCollections).ToList();
		return [.. ordered.Where(static collection => collection.DisplayName != TestCollections.PermanentAndroidGrants),
			.. ordered.Where(static collection => collection.DisplayName == TestCollections.PermanentAndroidGrants)];
	}
}
