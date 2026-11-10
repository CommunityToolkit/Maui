using CommunityToolkit.Maui.Core;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class MediaFailedEventArgsTests
{
	[Fact]
	public void Constructor_NullErrorMessage_ThrowsArgumentNullException()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => new MediaFailedEventArgs(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		exception.ParamName.Should().Be("errorMessage");
	}

	[Fact]
	public void Constructor_ErrorMessage_IsReturned()
	{
		new MediaFailedEventArgs("failed").ErrorMessage.Should().Be("failed");
	}
}