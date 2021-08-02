using Android.App;
using Android.Runtime;
using Microsoft.Maui;
using System;

namespace MCT.Sample
{
    [Application]
    public class MainApplication : MauiApplication<Startup>
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }
    }
}