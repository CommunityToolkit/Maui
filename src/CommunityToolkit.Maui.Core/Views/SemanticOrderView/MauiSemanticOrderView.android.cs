
using System;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Google.Android.Material.Chip;
using Microsoft.Maui.Graphics.Platform;
using Microsoft.Maui.Platform;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// TBD
/// </summary>
public class MauiSemanticOrderView : ViewGroup
{
	internal ISemanticOrderView VirtualView { get; init; } = null!;
	Context context;
	//public MauiSemanticOrderView(Context context, ISemanticOrderView virtualView)
	//	: base(context) 
	//{
	//	this.virtualView = virtualView;
	//	this.context = context;
	//	this.ChildViewAdded += (_, __) =>
	//	{
	//		_ = true;
	//	};
	//}

	public MauiSemanticOrderView(Context context) : base(context)
	{
		this.context = context;
		this.ChildViewAdded += (_, __) =>
		{
			_ = true;
		};
	}

	public MauiSemanticOrderView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
	{
		var context = Context;
		ArgumentNullException.ThrowIfNull(context);
		this.context = context;
	}

	public MauiSemanticOrderView(Context context, IAttributeSet attrs) : base(context, attrs)
	{
		this.context = context;
	}

	public MauiSemanticOrderView(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
	{
		this.context = context;
	}

	public MauiSemanticOrderView(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes)
	{
		this.context = context;
	}


	protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
	{
		var h = this.MeasuredHeight;
		var w = this.MeasuredWidth;

		var height = Height;
		var width = Width;
		SetAccessibilityElements();

		if (CrossPlatformArrange is null)
		{
			return;
		}

		var destination = context.ToCrossPlatformRectInReferenceFrame(left, top, right, bottom);

		CrossPlatformArrange(destination);

		//base.OnLayout(changed, left, top, right, bottom);
	}
	internal Func<double, double, Microsoft.Maui.Graphics.Size>? CrossPlatformMeasure { get; set; }
	internal Func<Microsoft.Maui.Graphics.Rect, Microsoft.Maui.Graphics.Size>? CrossPlatformArrange { get; set; }

	protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
	{
		if (CrossPlatformMeasure == null)
		{
			base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
			return;
		}

		var deviceIndependentWidth = widthMeasureSpec.ToDouble(context);
		var deviceIndependentHeight = heightMeasureSpec.ToDouble(context);

		var widthMode = MeasureSpec.GetMode(widthMeasureSpec);
		var heightMode = MeasureSpec.GetMode(heightMeasureSpec);

		var measure = CrossPlatformMeasure(deviceIndependentWidth, deviceIndependentHeight);

		// If the measure spec was exact, we should return the explicit size value, even if the content
		// measure came out to a different size
		var width = widthMode == MeasureSpecMode.Exactly ? deviceIndependentWidth : measure.Width;
		var height = heightMode == MeasureSpecMode.Exactly ? deviceIndependentHeight : measure.Height;

		var platformWidth = context.ToPixels(width);
		var platformHeight = context.ToPixels(height);

		// Minimum values win over everything
		platformWidth = Math.Max(MinimumWidth, platformWidth);
		platformHeight = Math.Max(MinimumHeight, platformHeight);

		SetMeasuredDimension((int)platformWidth, (int)platformHeight);
		if (VirtualView.Content is IView view && firstTime)
		{
			AddView(view.ToPlatform(VirtualView.Handler!.MauiContext!));
			firstTime = false;
		}
	}
	bool firstTime = true;

	void SetAccessibilityElements()
	{
		var viewOrder = VirtualView.ViewOrder.OfType<IView>().ToList();

		for (var i = 1; i < viewOrder.Count; i++)
		{
			var view1 = viewOrder[i - 1].GetViewForAccessibility();
			var view2 = viewOrder[i].GetViewForAccessibility();

			if (view1 == null || view2 == null)
			{
				return;
			}

			view2.AccessibilityTraversalAfter = view1.Id;
			view1.AccessibilityTraversalBefore = view2.Id;
		}
	}

	protected override void DispatchDraw(Canvas? canvas)
	{
		if (Clip != null)
		{
			ClipChild(canvas);
		}

		base.DispatchDraw(canvas);
	}

	IBorderStroke? clip;
	internal IBorderStroke? Clip
	{
		get => clip;
		set
		{
			clip = value;
			PostInvalidate();
		}
	}

	void ClipChild(Canvas? canvas)
	{
		if (Clip == null || canvas == null)
		{
			return;
		}

		float density = context.GetDisplayDensity();

		float strokeThickness = (float)(Clip.StrokeThickness * density);
		float offset = strokeThickness / 2;
		float w = (canvas.Width / density) - strokeThickness;
		float h = (canvas.Height / density) - strokeThickness;

		var bounds = new Microsoft.Maui.Graphics.RectF(offset, offset, w, h);
		var path = Clip.Shape?.PathForBounds(bounds);
		var currentPath = path?.AsAndroidPath(scaleX: density, scaleY: density);

		if (currentPath != null)
		{
			canvas.ClipPath(currentPath);
		}
	}
}
