namespace AddAndroidTextureViewTest;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Platform;
using System.Diagnostics;
public partial class App : Application {
	public App() {
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState) {
		return new Window(new MyTestPage());
	}

}
public partial class MyTestPage : ContentPage {
	public MyTestPage() {

		MediaElement mediaElement = new();
		mediaElement.HandlerChanged += delegate {
#if ANDROID
			/*var platformView = mediaElement.ToPlatform(mediaElement.Handler!.MauiContext!);
			Debug.WriteLine("PLATFORM VIEW TYPE " + platformView.GetType());*/
#endif
		};
		AbsoluteLayout absDummy = new();

		this.Content = absDummy;

		AbsoluteLayout absRoot = new();
		absDummy.Add(absRoot);

		absRoot.Add(mediaElement);

	}
}