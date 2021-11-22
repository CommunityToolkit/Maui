using System;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Extensions;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Extensions;

public class ColorAnimationExtensions_Test : BaseTest
{
	[Fact]
	public async Task BackgroundColorTo_VerifyColor()
	{
		VisualElement element = new Label();
		var isSuccessful = await element.BackgroundColorTo(Colors.Aqua);

		Assert.True(isSuccessful);
		Assert.Equal(Colors.Aqua, element.BackgroundColor);
	}
}

