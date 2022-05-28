using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Primitives;

namespace CommunityToolkit.Maui.Views;

public partial class CameraView : View, ICameraView
{
	public bool IsAvailable { get; set; }
	public bool IsCameraViewBusy { get; set; }

	public static readonly BindableProperty CameraFlashModeProperty =
		BindableProperty.Create(nameof(CameraFlashMode), typeof(CameraFlashMode), typeof(CameraView), CameraFlashMode.Off);

	public CameraFlashMode CameraFlashMode
	{
		get => (CameraFlashMode)GetValue(CameraFlashModeProperty);
		set => SetValue(CameraFlashModeProperty, value);
	}

	public void OnAvailable()
	{
	}

	public void OnMediaCaptured()
	{
	}

	public void OnMediaCapturedFailed()
	{
	}

	public void Shutter()
	{
		Handler?.Invoke(nameof(ICameraView.Shutter));
	}
}
