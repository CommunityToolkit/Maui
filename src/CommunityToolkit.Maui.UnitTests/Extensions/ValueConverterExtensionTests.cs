using CommunityToolkit.Maui.Extensions;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Extensions;

public class ValueConverterExtensionTests
{
	#region ValidateNullableTargetType

	[Fact]
	public void ValidateNullableTargetType_ShouldNotThrowArgumentException_WhenValidatingNullableBoolAgainstBool()
	{
		//Arrange

		//Act
		var action = () => ValueConverterExtension.ValidateTargetType<bool>(typeof(bool?));

		//Assert
		action.Should().NotThrow<ArgumentException>();
	}
	
	[Fact]
	public void ValidateNullableTargetType_ShouldNotThrowArgumentException_WhenValidatingNullableIntAgainstInt()
	{
		//Arrange

		//Act
		var action = () => ValueConverterExtension.ValidateTargetType<int>(typeof(int?));

		//Assert
		action.Should().NotThrow<ArgumentException>();
	}
	
	[Fact]
	public void ValidateNullableTargetType_ShouldNotThrowArgumentException_WhenValidatingNullableCharAgainstChar()
	{
		//Arrange

		//Act
		var action = () => ValueConverterExtension.ValidateTargetType<char>(typeof(char?));

		//Assert
		action.Should().NotThrow<ArgumentException>();
	}

	#endregion
}