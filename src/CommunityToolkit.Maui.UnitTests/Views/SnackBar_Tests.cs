using System;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Extensions;
using Microsoft.Maui.Controls;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class SnackBar_Tests
{
	[Fact]
	public void PageExtension_DisplaySnackBarAsync_PlatformNotSupportedException()
	{
		var page = new ContentPage();
		Assert.ThrowsAsync<PlatformNotSupportedException>(() => page.DisplaySnackBarAsync(string.Empty, string.Empty, () => { }));
	}
}
