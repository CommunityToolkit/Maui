using Microsoft.Maui.Platform;
using NPointStateType = Tizen.NUI.PointStateType;

namespace CommunityToolkit.Maui.Core.Views;

public partial class MauiDrawingView : PlatformTouchGraphicsView
{
	/// <summary>
	/// Initialize a new instance of <see cref="MauiDrawingView" />.
	/// </summary>
	public MauiDrawingView() : base(null)
	{
		previousPoint = new();
		TouchEvent += OnTouch;
	}

	/// <inheritdoc />
	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			currentPath.Dispose();
		}

		base.Dispose(disposing);
	}

	/// <summary>
	/// Initialize resources
	/// </summary>
	public void Initialize()
	{
		Drawable = new DrawingViewDrawable(this);
		Lines.CollectionChanged += OnLinesCollectionChanged;
	}

	bool OnTouch(object source, TouchEventArgs e)
	{
		var point = new PointF(e.Touch.GetLocalPosition(0).X.ToScaledDP(), e.Touch.GetLocalPosition(0).Y.ToScaledDP());
		switch (e.Touch.GetState(0))
		{
			case NPointStateType.Down:
				OnStart(point);
				break;
			case NPointStateType.Motion:
				OnMoving(point);
				break;
			case NPointStateType.Up:
				OnFinish();
				break;
			case NPointStateType.Interrupted:
				OnCancel();
				break;
		}
		Redraw();

		return true;
	}

	void Redraw()
	{
		Invalidate();
	}
}