using Microsoft.Maui.Controls.Internals;
namespace CommunityToolkit.Maui.UnitTests.Mocks;

// Inspired by https://github.com/dotnet/maui/blob/79695fbb7ba6517a334c795ecf0a1d6358ef309a/src/Controls/Foldable/test/MockPlatformServices.cs#L145-L176

#pragma warning disable CS0612 // Type or member is obsolete
class MockResourcesProvider : ISystemResourcesProvider
#pragma warning restore CS0612 // Type or member is obsolete
{
	readonly ResourceDictionary dictionary = [];

	public IResourceDictionary GetSystemResources() => dictionary;
}