global using Microsoft.Maui;
global using Microsoft.Maui.Devices;
global using Microsoft.Maui.Graphics;
global using Microsoft.Maui.Hosting;

#if IOS || MACCATALYST
global using NativePlatformCameraPreviewView = global::UIKit.UIView;
global using NativePlatformView = global::UIKit.UIView;
#elif ANDROID
global using NativePlatformCameraPreviewView = global::AndroidX.Camera.View.PreviewView;
global using NativePlatformView = global::Android.Views.View;
#elif WINDOWS
global using NativePlatformCameraPreviewView = global::Microsoft.UI.Xaml.FrameworkElement;
global using NativePlatformView = global::Microsoft.UI.Xaml.FrameworkElement;
#endif