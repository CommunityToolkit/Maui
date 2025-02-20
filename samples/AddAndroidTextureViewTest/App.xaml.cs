namespace AddAndroidTextureViewTest;

using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Platform;
using System.Diagnostics;

public partial class App : Application {
	public App() {
	}

	protected override Window CreateWindow(IActivationState? activationState) {
		return new Window(new MyTestPage());
	}

}
public partial class MyTestPage : ContentPage {
	public MyTestPage() {

		//======================================
		//SET THE ANDROID VIEW TYPE TO CREATE
		//======================================
		MediaElementOptions options = new() { AndroidViewType = AndroidViewType.TextureView };
		//MediaElementOptions options = new() { AndroidViewType = AndroidViewType.SurfaceView };

		MediaElement mediaElement = new(options);
		//MediaElement mediaElement = new(); // default is SurfaceView

		mediaElement.HandlerChanged += delegate {

#if ANDROID
			//============================================
			//TEST IF CREATED CORRECT ANDROID VIEW TYPE
			//============================================
			var platformView = ((CommunityToolkit.Maui.Core.Views.MauiMediaElement)mediaElement.ToPlatform(mediaElement.Handler!.MauiContext!));
			var viewType = platformView.PlayerView.VideoSurfaceView?.GetType();
			Debug.WriteLine("ANDROID VIEW TYPE: " + viewType?.ToString() ?? "");

			//=== outputs either:
			// ANDROID VIEW TYPE: Android.Views.TextureView
			// ANDROID VIEW TYPE: Android.Views.SurfaceView
#endif
		};
		
		AbsoluteLayout absDummy = new();
		this.Content = absDummy;

		AbsoluteLayout absRoot = new();
		absDummy.Add(absRoot);

		absRoot.Add(mediaElement);

	}
}