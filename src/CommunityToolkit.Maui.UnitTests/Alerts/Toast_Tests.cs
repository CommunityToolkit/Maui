using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Alerts;

public class Toast_Tests : BaseTest
{
	[Fact]
	public void ToastMake_NewToastCreatedWithValidProperties()
	{
		var expectedToast = new Toast
		{
			Duration = ToastDuration.Long,
			Text = "Test"
		};

		var currentToast = Toast.Make(
			"Test",
			ToastDuration.Long);

		currentToast.Should().BeEquivalentTo(expectedToast);
	}
}