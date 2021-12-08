using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Microsoft.Maui;

namespace CommunityToolkit.Maui.Controls.Platform;

static class ActivityExtensions
{
	public static Activity? ResolveActivity(this MauiApplication mauiApplication)
	{
		return mauiApplication.GetActivity();
	}
}
