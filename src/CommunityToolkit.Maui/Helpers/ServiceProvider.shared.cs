namespace CommunityToolkit.Maui.Helpers;


  static class ServiceProvider
{
	 public static TService GetService<TService>()
	 {
		 if (Current == null)
		 {
			 throw new NullReferenceException(string.Format("An instance of {0} not found.", nameof(IServiceProvider)));
		 }

		 var service = Current.GetService<TService>();
		 if (service == null)
		 {
			 throw new NullReferenceException(string.Format("An instance of {0} not found on the ServiceProvider.", nameof(TService)));
		 }
		 return service;
	}
	 static IServiceProvider? Current
		=>
#if WINDOWS
			MauiWinUIApplication.Current.Services;
#elif ANDROID
			MauiApplication.Current.Services;
#elif IOS || MACCATALYST
			MauiUIApplicationDelegate.Current.Services;
#else
			null;
#endif
}