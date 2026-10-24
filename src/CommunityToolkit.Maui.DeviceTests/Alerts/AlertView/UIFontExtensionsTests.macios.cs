using CommunityToolkit.Maui.Extensions;
using Microsoft.Extensions.DependencyInjection;
using UIKit;
using Font = Microsoft.Maui.Font;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.AlertView)]
[Collection(TestCollections.DisableParallelization)]
public class UIFontExtensionsTests : AlertsTestBase
{
	[Fact]
	public async Task ToUIFontFallsBackToTheSystemFontWhenThereIsNoApplication()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var previousApplication = Application.Current;
			Application.Current = null;
			try
			{
				var font = Font.OfSize("Courier", 17).ToUIFont();

				Assert.Equal(17, (double)font.PointSize);
				Assert.Equal(UIFont.SystemFontOfSize(17)!.Name, font.Name);
			}
			finally
			{
				Application.Current = previousApplication;
			}
		});
	}

	[Fact]
	public async Task ToUIFontFallsBackToTheSystemFontWhenTheApplicationHasNoHandler()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var application = Application.Current!;
			var previousHandler = application.Handler;
			application.Handler = null;
			try
			{
				var font = Font.OfSize("Courier", 17).ToUIFont();

				Assert.Equal(17, (double)font.PointSize);
				Assert.Equal(UIFont.SystemFontOfSize(17)!.Name, font.Name);
			}
			finally
			{
				application.Handler = previousHandler;
			}
		});
	}

	[Fact]
	public async Task ToUIFontFallsBackToTheSystemFontWhenTheHandlerHasNoMauiContext()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var application = Application.Current!;
			var previousHandler = application.Handler;
			application.Handler = new StubApplicationHandler(null);
			try
			{
				var font = Font.OfSize("Courier", 17).ToUIFont();

				Assert.Equal(UIFont.SystemFontOfSize(17)!.Name, font.Name);
			}
			finally
			{
				application.Handler = previousHandler;
			}
		});
	}

	[Fact]
	public async Task ToUIFontFallsBackToTheSystemFontWhenTheServicesHaveNoFontManager()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var application = Application.Current!;
			var previousHandler = application.Handler;
			using var services = new ServiceCollection().BuildServiceProvider();
			application.Handler = new StubApplicationHandler(new DeviceTestMauiContext(services));
			try
			{
				var font = Font.OfSize("Courier", 17).ToUIFont();

				Assert.Equal(UIFont.SystemFontOfSize(17)!.Name, font.Name);
			}
			finally
			{
				application.Handler = previousHandler;
			}
		});
	}

	[Fact]
	public async Task ToUIFontFallsBackToTheSystemFontWhenTheFontManagerReturnsNoFont()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var application = Application.Current!;
			var previousHandler = application.Handler;
			using var services = new ServiceCollection().AddSingleton<IFontManager>(new NoFontManager()).BuildServiceProvider();
			application.Handler = new StubApplicationHandler(new DeviceTestMauiContext(services));
			try
			{
				var font = Font.OfSize("Courier", 17).ToUIFont();

				Assert.Equal(UIFont.SystemFontOfSize(17)!.Name, font.Name);
				Assert.Equal(17, (double)font.PointSize);
			}
			finally
			{
				application.Handler = previousHandler;
			}
		});
	}

	[Fact]
	public async Task ToUIFontUsesTheFontManagerOfTheApplicationHandlerWhenThereIsOne()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var application = Application.Current!;
			var previousHandler = application.Handler;
			application.Handler = new StubApplicationHandler(MauiContext);
			try
			{
				var font = Font.OfSize("Courier", 17).ToUIFont();

				Assert.Equal("Courier", font.FamilyName);
				Assert.Equal(17, (double)font.PointSize);
			}
			finally
			{
				application.Handler = previousHandler;
			}
		});
	}

	[Fact]
	public async Task ToUIFontKeepsTheRequestedSizeOfTheDefaultFont()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var application = Application.Current!;
			var previousHandler = application.Handler;
			application.Handler = null;
			try
			{
				Assert.Equal(0, (double)Font.SystemFontOfSize(0).ToUIFont().PointSize);
				Assert.Equal(31, (double)Font.SystemFontOfSize(31).ToUIFont().PointSize);
			}
			finally
			{
				application.Handler = previousHandler;
			}
		});
	}

	sealed class NoFontManager : IFontManager
	{
		public double DefaultFontSize => 14;

		public UIFont DefaultFont => UIFont.SystemFontOfSize(14)!;

		public UIFont GetFont(Font font, double defaultFontSize = 0) => null!;
	}
}