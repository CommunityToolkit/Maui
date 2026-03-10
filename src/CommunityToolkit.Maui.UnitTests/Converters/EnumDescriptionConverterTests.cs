using Xunit;

using CommunityToolkit.Maui.Converters;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class EnumDescriptionConverterTests
{
   enum TestEnum
	{
		[Display(Name = "Display Name")]
		WithDisplay,
		[Description("Description Text")]
		WithDescription,
		NoAttribute
	}

	enum MultiAttributeEnum
	{
		[Display(Name = "Display Name")]
		[Description("Description Text")]
		Both,
		[Display(Name = "")]
		[Description("Description Text")]
		EmptyDisplay,
		[Display(Name = " ")]
		[Description("Description Text")]
		WhitespaceDisplay,
		[Description("")]
		EmptyDescription,
	}

  [Fact]
	public void ConvertFrom_ThrowsArgumentNullException_WhenNull()
	{
		var converter = new EnumDescriptionConverter();
		Assert.Throws<ArgumentNullException>(() => converter.ConvertFrom(null!));
	}

	[Fact]
	public void ConvertFrom_FallbackToValueToString_WhenInvalidEnumValue()
	{
		var converter = new EnumDescriptionConverter();
		// Cast an int not defined in MultiAttributeEnum
		var invalid = (MultiAttributeEnum)999;
		var result = converter.ConvertFrom(invalid);
		Assert.Equal("999", result);
	}

	[Fact]
	public void ConvertFrom_ReturnsDisplayName()
	{
		var converter = new EnumDescriptionConverter();
		var result = converter.ConvertFrom(TestEnum.WithDisplay);
		Assert.Equal("Display Name", result);
	}

	[Fact]
	public void ConvertFrom_ReturnsDescriptionText()
	{
		var converter = new EnumDescriptionConverter();
		var result = converter.ConvertFrom(TestEnum.WithDescription);
		Assert.Equal("Description Text", result);
	}

	[Fact]
	public void ConvertFrom_ReturnsEnumName_WhenNoAttribute()
	{
		var converter = new EnumDescriptionConverter();
		var result = converter.ConvertFrom(TestEnum.NoAttribute);
		Assert.Equal("NoAttribute", result);
	}


  [Fact]
	public void ConvertFrom_DisplayTakesPrecedence_WhenBothAttributes()
	{
		var converter = new EnumDescriptionConverter();
		var result = converter.ConvertFrom(MultiAttributeEnum.Both);
		Assert.Equal("Display Name", result);
	}

	[Fact]
	public void ConvertFrom_FallbackToDescription_WhenDisplayEmpty()
	{
		var converter = new EnumDescriptionConverter();
		var result = converter.ConvertFrom(MultiAttributeEnum.EmptyDisplay);
		Assert.Equal("Description Text", result);
	}

	[Fact]
	public void ConvertFrom_FallbackToDescription_WhenDisplayWhitespace()
	{
		var converter = new EnumDescriptionConverter();
		var result = converter.ConvertFrom(MultiAttributeEnum.WhitespaceDisplay);
		Assert.Equal("Description Text", result);
	}

	[Fact]
	public void ConvertFrom_FallbackToEnumName_WhenDescriptionEmpty()
	{
		var converter = new EnumDescriptionConverter();
		var result = converter.ConvertFrom(MultiAttributeEnum.EmptyDescription);
		Assert.Equal("EmptyDescription", result);
	}
}