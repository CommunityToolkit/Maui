using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Views;

public partial class CameraView : View, ICameraView
{
	public bool IsAvailable { get; set; }
	public bool IsCameraViewBusy { get; set; }

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
