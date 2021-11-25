using System;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.UnitTests.Mocks;
using CommunityToolkit.Maui.Views.Popup.SnackBar;
using Microsoft.Maui.Controls;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class SnackBar_Tests : BaseTest
{
	ISnackbar snackbar;
	public SnackBar_Tests()
	{
		snackbar = new MockSnackbar();
	}

	[Fact]
	public async Task VisualElementExtension_PageDisplaySnackBar_PlatformNotSupportedException()
	{
		var page = new ContentPage();
		await Assert.ThrowsAsync<PlatformNotSupportedException>(() => page.DisplaySnackBar(string.Empty, string.Empty, () => { }));
	}

	[Fact]
	public async Task VisualElementExtension_AnchorDisplaySnackBar_PlatformNotSupportedException()
	{
		var button = new Button();
		await Assert.ThrowsAsync<PlatformNotSupportedException>(() => button.DisplaySnackBar(string.Empty, string.Empty, () => { }));
	}

	[Fact]
	public async Task SnackbarShow_AnchorDisplaySnackBar_PlatformNotSupportedException()
	{
		await snackbar.Show();
		Assert.True(snackbar.IsShown);
	}
}
