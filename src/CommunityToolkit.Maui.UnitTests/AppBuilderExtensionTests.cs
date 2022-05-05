using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.Views;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests;

public class AppBuilderExtensionTests : BaseTest
{
	[Fact]
	public void EnsureMauiAppBuilderExtensionsAddsHandlers()
	{
		var builderWithoutInitialization = MauiApp.CreateBuilder();
		var serviceProviderWithoutInitialization = builderWithoutInitialization.Services.BuildServiceProvider();

		Assert.Throws<InvalidOperationException>(() => serviceProviderWithoutInitialization.GetRequiredService<HandlerMauiAppBuilderExtensions.HandlerRegistration>());

		var builderWithInitialization = MauiApp.CreateBuilder().UseMauiCommunityToolkit();
		var serviceProviderInitialization = builderWithInitialization.Services.BuildServiceProvider();
		var handlerRegistration = serviceProviderInitialization.GetRequiredService<HandlerMauiAppBuilderExtensions.HandlerRegistration>();
	}
}
