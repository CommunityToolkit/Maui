namespace CommunityToolkit.Maui.DeviceTests;

// xUnit v2 discovers collection definitions only in the test assembly, so each device-test app declares this one.
[CollectionDefinition(TestCollections.DisableParallelization, DisableParallelization = true)]
public sealed class DisableParallelizationCollection;