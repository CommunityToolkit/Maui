using System;
using Microsoft.Maui.Platform;
using SkiaSharp;
using Tizen.UIExtensions.Common;
using Tizen.UIExtensions.NUI;
using DeviceInfo = Tizen.UIExtensions.Common.DeviceInfo;
using Rect = Microsoft.Maui.Graphics.Rect;
using Size = Microsoft.Maui.Graphics.Size;
using TSize = Tizen.UIExtensions.Common.Size;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// An Android ViewGroup implementation responible for hosting, measuring, and arranging cross-platform .NET Maui content.
/// </summary>
public class ContentViewGroup : ViewGroup, IMeasurable
{
	IView? virtualView;
	Size measureCache;
	bool needMeasureUpdate;

	/// <summary>
	/// Initializes a new instance.
	/// </summary>
	public ContentViewGroup(IView? view)
	{
		virtualView = view;
		LayoutUpdated += OnLayoutUpdated;
	}

	/// <summary>
	/// Gets or sets the delegate function that invoke the cross-platform measurement logic.
	/// </summary>
	public Func<double, double, Size>? CrossPlatformMeasure { get; set; }
	/// <summary>
	/// /// Gets or sets the delegate function that invoke the cross-platform layout arrange logic.
	/// </summary>
	public Func<Rect, Size>? CrossPlatformArrange { get; set; }

	/// <summary>
	/// Marks this view as 'dirty', indicating that it needs to be remeasured.
	/// </summary>
	public void SetNeedMeasureUpdate()
	{
		needMeasureUpdate = true;
		MarkChanged();
	}

	/// <summary>
	/// Reset the 'dirty' flayg, indicating that the view's measurement is currently up-to-date.
	/// </summary>
	public void ClearNeedMeasureUpdate()
	{
		needMeasureUpdate = false;
	}

	/// <summary>
	/// Measures the view and its content using the provided available width and height constraints.
	/// </summary>
	public TSize Measure(double availableWidth, double availableHeight)
	{
		var measuredSize = InvokeCrossPlatformMeasure(availableWidth / DeviceInfo.ScalingFactor, availableHeight / DeviceInfo.ScalingFactor) * DeviceInfo.ScalingFactor;
		return new TSize(measuredSize.Width, measuredSize.Height);
	}

	/// <summary>
	/// Executes the cached CrossPlatformMeasure delegate to calculate the desired size of the cross-platform content.
	/// </summary>
	public Size InvokeCrossPlatformMeasure(double availableWidth, double availableHeight)
	{
		if (CrossPlatformMeasure == null)
		{
			return Microsoft.Maui.Graphics.Size.Zero;
		}

		var measured = CrossPlatformMeasure(availableWidth, availableHeight);
		if (measured != measureCache && virtualView?.Parent is IView parentView)
		{
			parentView?.InvalidateMeasure();
		}
		measureCache = measured;
		ClearNeedMeasureUpdate();
		return measured;
	}

	void OnLayoutUpdated(object? sender, LayoutEventArgs e)
	{
		if (CrossPlatformArrange is null || CrossPlatformMeasure is null)
		{
			return;
		}

		var bounds = this.GetBounds();
		var platformGeometry = new Rect(bounds.X / DeviceInfo.ScalingFactor, bounds.Y / DeviceInfo.ScalingFactor, bounds.Width / DeviceInfo.ScalingFactor, bounds.Height / DeviceInfo.ScalingFactor);
		if (needMeasureUpdate || measureCache != platformGeometry.Size)
		{
			InvokeCrossPlatformMeasure(platformGeometry.Width, platformGeometry.Height);
		}

		if (platformGeometry.Width > 0 && platformGeometry.Height > 0)
		{
			platformGeometry.X = 0;
			platformGeometry.Y = 0;
			CrossPlatformArrange(platformGeometry);
		}
	}
}