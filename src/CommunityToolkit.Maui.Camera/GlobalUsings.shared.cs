#if IOS || MACCATALYST
global using NativePlatformCameraPreviewView = global::UIKit.UIView;
global using NativePlatformView = global::UIKit.UIView;
#elif ANDROID
global using NativePlatformView = global::Android.Views.View;
global using NativePlatformCameraPreviewView = global::AndroidX.Camera.View.PreviewView;
#elif WINDOWS
global using NativePlatformCameraPreviewView = global::Microsoft.UI.Xaml.FrameworkElement;
global using NativePlatformView = global::Microsoft.UI.Xaml.FrameworkElement;
#elif TIZEN
global using NativePlatformCameraPreviewView = global::object;
global using NativePlatformView = global::object;
#endif