using System.Globalization;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.UnitTests.Mocks;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests;

[Collection("CommunityToolkit.UnitTests")]
public abstract class BaseTest : IDisposable, IAsyncDisposable
{
	protected const AppTheme initialAppTheme = AppTheme.Light;

	readonly CultureInfo defaultCulture, defaultUiCulture;

	bool isDisposed;

	protected BaseTest()
	{
		defaultCulture = Thread.CurrentThread.CurrentCulture;
		defaultUiCulture = Thread.CurrentThread.CurrentUICulture;

		DispatcherProvider.SetCurrent(new MockDispatcherProvider());
	}

	~BaseTest() => Dispose(false);

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
		mediaElementOptions.SetIsAndroidForegroundServiceEnabled(false);
		isDisposed = true;
	}
}