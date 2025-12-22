using System.Globalization;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.UnitTests.Mocks;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests;

[Collection("CommunityToolkit.UnitTests")]
public abstract class BaseTest : IDisposable, IAsyncDisposable
{
	readonly CultureInfo defaultCulture, defaultUiCulture;
	protected readonly MockAppInfo mockAppInfo;
	protected const AppTheme initialAppTheme = AppTheme.Light;

	bool isDisposed;

	protected enum TestDuration
	{
#if DEBUG
		Short = 20_000,
		Medium = 50_000,
		Long = 100_000
#else
		Short = 2_000,
		Medium = 5_000,
		Long = 10_000
#endif
	}

	protected BaseTest()
	{
		defaultCulture = Thread.CurrentThread.CurrentCulture;
		defaultUiCulture = Thread.CurrentThread.CurrentUICulture;

		DeviceDisplay.SetCurrent(null);
		DeviceInfo.SetCurrent(null);
		AppInfo.SetCurrent(mockAppInfo = new()
		{
			RequestedTheme = initialAppTheme
		});

		DispatcherProvider.SetCurrent(new MockDispatcherProvider());
		DeviceDisplay.SetCurrent(null);
	}

	~BaseTest() => Dispose(false);

	public async ValueTask DisposeAsync()
	{
		await DisposeAsyncCore().ConfigureAwait(false);

		Dispose(false);
		GC.SuppressFinalize(this);
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual ValueTask DisposeAsyncCore()
	{
		return ValueTask.CompletedTask;
	}

	protected virtual void Dispose(bool isDisposing)
	{
		if (isDisposed)
		{
			return;
		}

		Thread.CurrentThread.CurrentCulture = defaultCulture;
		Thread.CurrentThread.CurrentUICulture = defaultUiCulture;

		DeviceDisplay.SetCurrent(null);
		DispatcherProvider.SetCurrent(null);

		// Restore default options
		var options = new Options();
		options.SetShouldUseStatusBarBehaviorOnAndroidModalPage(true);
		options.SetShouldEnableSnackbarOnWindows(false);
		options.SetShouldSuppressExceptionsInAnimations(false);
		options.SetShouldSuppressExceptionsInBehaviors(false);
		options.SetShouldSuppressExceptionsInConverters(false);
		options.SetPopupDefaults(new DefaultPopupSettings());
		options.SetPopupOptionsDefaults(new DefaultPopupOptionsSettings());

		// Restore default MediaElementOptions
		var mediaElementOptions = new MediaElementOptions();
		mediaElementOptions.SetDefaultAndroidViewType(AndroidViewType.SurfaceView);
		mediaElementOptions.SetDefaultAndroidForegroundServiceEnabled(true);
		isDisposed = true;
	}

	protected static Task<Stream> GetStreamFromImageSource(StreamImageSource imageSource, CancellationToken token)
		=> imageSource.Stream(token);

	protected static bool StreamEquals(Stream a, Stream b)
	{
		if (a == b)
		{
			return true;
		}

		if (a.Length != b.Length)
		{
			return false;
		}

		for (var i = 0; i < a.Length; i++)
		{
			if (a.ReadByte() != b.ReadByte())
			{
				return false;
			}
		}

		return true;
	}
}