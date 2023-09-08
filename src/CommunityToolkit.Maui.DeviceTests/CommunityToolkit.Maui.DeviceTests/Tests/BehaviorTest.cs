using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Behaviors;
using Xunit;

namespace CommunityToolkit.Maui.DeviceTests.Tests;
public class BehaviorTest
{
	[Fact]
	public void SuccessfulTest()
	{
		Assert.True(true);
	}

	[Fact]
	public void SomeBehavior()
	{
		var behavior = new IconTintColorBehavior();
		var expectedColor = Colors.Fuchsia;
		behavior.TintColor = expectedColor;

		var appliedColor = behavior.GetValue(IconTintColorBehavior.TintColorProperty) as Color;

		Assert.Equal(expectedColor, appliedColor);
	}

	[Fact]
	public async Task ChangeStatusBar()
	{
		var expectedColor = Colors.Fuchsia;
		var page = new ContentPage();
		var behavior = new IconTintColorBehavior();

		var tcs = new TaskCompletionSource();
		
		page.Loaded += (s,e) => tcs.SetResult();

		var cts = new CancellationTokenSource();
		using var x = cts.Token.Register(() => tcs.SetCanceled());
		cts.CancelAfter(10_000);

		await tcs.Task;

		page.Behaviors.Add(behavior);

		behavior.TintColor = expectedColor;

		var appliedColor = behavior.TintColor;

		Assert.Equal(expectedColor, appliedColor);

		
	}
}
