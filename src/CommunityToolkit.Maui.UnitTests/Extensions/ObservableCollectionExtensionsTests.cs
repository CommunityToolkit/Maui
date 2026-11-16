using CommunityToolkit.Maui.Core.Extensions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Extensions;

public class ObservableCollectionExtensionsTests : BaseTest
{

	[Fact]
	public void ToObservableCollection_NullCollection_ThrowsArgumentNullException()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => ObservableCollectionExtensions.ToObservableCollection<int>(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		Assert.Equal("collection", exception.ParamName);
	}

	[Fact]
	public void ToObservableCollection_CopiesTheItems()
	{
		int[] items = [1, 2, 3];

		var result = items.ToObservableCollection();

		Assert.Equal(items, result);
	}
}