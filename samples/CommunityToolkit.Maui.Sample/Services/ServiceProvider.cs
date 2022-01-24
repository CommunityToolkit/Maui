namespace CommunityToolkit.Maui.Sample;

public static class ServiceProvider
{
	static IServiceProvider Current =>
#if WINDOWS10_0_17763_0_OR_GREATER
			MauiWinUIApplication.Current.Services;
#elif ANDROID
            MauiApplication.Current.Services;
#elif IOS || MACCATALYST
			MauiUIApplicationDelegate.Current.Services;
#else
			throw new PlatformNotSupportedException();
#endif

	public static TService GetRequiredService<TService>() where TService : notnull
		=> Current.GetRequiredService<TService>();

	public static TService GetRequiredService<TService>(Type type)
		=> (TService)Current.GetRequiredService(type);
}