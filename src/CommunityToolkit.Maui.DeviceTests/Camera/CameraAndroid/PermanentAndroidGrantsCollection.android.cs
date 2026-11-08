using CommunityToolkit.Maui.DeviceTests;

[assembly: TestCollectionOrderer(PermanentGrantsLastOrderer.TypeName, PermanentGrantsLastOrderer.AssemblyName)]

namespace CommunityToolkit.Maui.DeviceTests;

// xUnit v2 discovers collection definitions only in the test assembly. Android only: no other platform's tests grant for good.
[CollectionDefinition(TestCollections.PermanentAndroidGrants, DisableParallelization = true)]
public sealed class PermanentAndroidGrantsCollection;
