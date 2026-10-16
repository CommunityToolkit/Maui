// Vendored from dotnet/maui @ 2067dd42c6032fbc40d2e46245806679a85df5e6
// Original path: src/TestUtils/src/DeviceTests/UINSWindow.iOS.cs
// Copyright (c) .NET Foundation and Contributors. Licensed under the MIT License (see LICENSE-dotnet-maui.txt).
// Toolkit change: namespace changed from Microsoft.Maui(.DeviceTests) to CommunityToolkit.Maui.DeviceTests.
// Toolkit change: renamed to the toolkit's platform suffix (UINSWindow.macios.cs).
// Toolkit change: #nullable enable added: the upstream project enables nullable reference types, these copies are compiled as generated code.
// Toolkit change: From skips null items instead of dereferencing them: NSArray.GetItem<T> returns T? in the iOS and Mac Catalyst bindings of .NET 11 and of 26.4 or later on .NET 10 (the 26.0 bindings that net10.0-ios and net10.0-maccatalyst compile against return T), so the upstream code fails CS8602 (nullable warnings are errors).
#nullable enable
using System;
using System.Runtime.InteropServices;
using CoreGraphics;
using Foundation;
using Microsoft.Maui.Graphics;
using ObjCRuntime;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

public static class UIWindowExtensions
{
	public static void SetFrame(this UIWindow window, Rect frame, bool display = true, bool animate = true)
	{
		var nsWindow = UINSWindow.From(window);
		if (nsWindow is null)
			throw new InvalidOperationException("Unable to update frame of non-existant window.");

		nsWindow.SetFrame(frame, display, animate);
	}

	public static bool HasNSWindow(this UIWindow window)
	{
		var nsWindow = UINSWindow.From(window);
		return nsWindow is not null;
	}

	class UINSWindow
	{
		[DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
		static extern void void_objc_msgSend_CGRect_bool_bool(IntPtr receiver, IntPtr selector, CGRect arg1, bool arg2, bool arg3);

		static NativeHandle? nsApplicationHandle;
		static NativeHandle NSApplicationHandle =>
			nsApplicationHandle ??= Class.GetHandle("NSApplication");

		static Selector? sharedApplicationSelector;
		static Selector SharedApplicationSelector =>
			sharedApplicationSelector ??= new Selector("sharedApplication");

		static Selector? windowsSelector;
		static Selector WindowsSelector =>
			windowsSelector ??= new Selector("windows");

		static Selector? uiWindowsSelector;
		static Selector UIWindowsSelector =>
			uiWindowsSelector ??= new Selector("uiWindows");

		Selector? setFrameSelector;
		Selector SetFrameSelector =>
			setFrameSelector ??= new Selector("setFrame:display:animate:");

		public UINSWindow(NativeHandle handle, UIWindow uiWindow)
		{
			Handle = handle;
			UIWindow = uiWindow;
		}

		protected NativeHandle Handle { get; }

		public UIWindow UIWindow { get; }

		public void SetFrame(CGRect frame, bool display = true, bool animate = true)
		{
			var screenHeight = UIWindow.Screen.Bounds.Height;
			frame = new CGRect(
				frame.X / 1.3,
				screenHeight - (frame.Y / 1.3),
				frame.Width / 1.3,
				frame.Height / 1.3);

			void_objc_msgSend_CGRect_bool_bool(Handle.Handle, SetFrameSelector.Handle, frame, display, animate);
		}

		internal static UINSWindow? From(UIWindow uiWindow)
		{
			var nsapp = Runtime.GetNSObject(NSApplicationHandle);
			if (nsapp is null)
				return null;

			var sharedApp = nsapp.PerformSelector(SharedApplicationSelector);
			var windows = sharedApp.PerformSelector(WindowsSelector) as NSArray;

			for (nuint i = 0; i < windows!.Count; i++)
			{
				var nswin = windows.GetItem<NSObject>(i);
				if (nswin is null)
					continue;

				var uiwindows = nswin.PerformSelector(UIWindowsSelector) as NSArray;

				for (nuint j = 0; j < uiwindows!.Count; j++)
				{
					var uiwin = uiwindows.GetItem<UIWindow>(j);

					if (uiwin is not null && uiwin.Handle == uiWindow.Handle)
						return new UINSWindow(nswin.Handle, uiWindow);
				}
			}

			return null;
		}
	}
}
