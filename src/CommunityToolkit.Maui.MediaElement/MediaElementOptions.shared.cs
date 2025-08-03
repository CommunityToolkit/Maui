﻿namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Construction options for MediaElement, for example, to create an Android SurfaceView or TextureView
/// </summary>
public class MediaElementOptions
{
	readonly MauiAppBuilder? builder;

	internal MediaElementOptions()
	{

	}

	internal MediaElementOptions(in MauiAppBuilder builder) : this()
	{
		this.builder = builder;
	}

	/// <summary>
	/// Set Android View type for MediaElement as SurfaceView or TextureView on construction
	/// </summary>
	internal static AndroidViewType DefaultAndroidViewType { get; private set; } = AndroidViewType.SurfaceView;

	/// <summary>
	/// Set Android Foreground Service for MediaElement on construction
	/// </summary>
	internal static bool IsAndroidForegroundServiceEnabled { get; private set; } = true;

	/// <summary>
	/// Set Android View type for MediaElement as SurfaceView or TextureView on construction
	/// </summary>
	public void SetDefaultAndroidViewType(AndroidViewType androidViewType) => DefaultAndroidViewType = androidViewType;

	/// <summary>
	/// Set Android Foreground Service for MediaElement on construction
	/// </summary>
	/// <param name="androidForegroundServiceEnabled">Specifies whether the Android Foreground Service should be enabled for the MediaElement. Set to <c>true</c> to enable, or <c>false</c> to disable.</param>
	public void SetDefaultAndroidForegroundService(bool androidForegroundServiceEnabled) => IsAndroidForegroundServiceEnabled = androidForegroundServiceEnabled;
}