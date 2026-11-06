using CommunityToolkit.Maui.Maps;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Extensions;

public class MapsAppBuilderExtensionsTests : BaseTest
{
	[Fact]
	public void UseMauiCommunityToolkitMaps_NullBuilder_ThrowsArgumentNullException()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => Maps.AppBuilderExtensions.UseMauiCommunityToolkitMaps(null, "key"));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		exception.ParamName.Should().Be("builder");
	}
}
