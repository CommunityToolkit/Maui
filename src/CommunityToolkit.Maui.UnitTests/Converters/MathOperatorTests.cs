using CommunityToolkit.Maui.Core;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class MathOperatorTests : BaseTest
{

	[Fact]
	public void MathOperator_NullName_ThrowsArgumentNullException()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => new MathOperator(null, 1, static _ => null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		Assert.Equal("name", exception.ParamName);
	}

	[Fact]
	public void MathOperator_NullCalculateFunc_ThrowsArgumentNullException()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => new MathOperator("name", 1, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		Assert.Equal("calculateFunc", exception.ParamName);
	}

	[Fact]
	public void MathOperator_KeepsItsArguments()
	{
		Func<object?[], object?> calculate = static _ => null;

		var mathOperator = new MathOperator("name", 2, calculate);

		Assert.Equal("name", mathOperator.Name);
		Assert.Equal(2, mathOperator.NumericCount);
		Assert.Same(calculate, mathOperator.CalculateFunc);
	}
}