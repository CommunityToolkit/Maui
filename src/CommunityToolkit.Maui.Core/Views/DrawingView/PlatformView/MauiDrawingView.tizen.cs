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

	/// <summary>
	/// Initialize resources
	/// </summary>
	public void Initialize()
	{
		Drawable = new DrawingViewDrawable(this);
		Lines.CollectionChanged += OnLinesCollectionChanged;
	}

	/// <inheritdoc />
	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			currentPath.Dispose();
			TouchEvent -= OnTouch;
		}

		base.Dispose(disposing);
	}

	bool OnTouch(object source, TouchEventArgs e)
	{
		var point = new PointF(e.Touch.GetLocalPosition(0).X.ToScaledDP(), e.Touch.GetLocalPosition(0).Y.ToScaledDP());
		var pointStateType = e.Touch.GetState(0);

		switch (pointStateType)
		{
			case NPointStateType.Leave:
			case NPointStateType.Stationary:
				break;

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

			default:
				throw new NotSupportedException($"{pointStateType} not supported");
		}

		Redraw();

		return true;
	}

	void Redraw()
	{
		Invalidate();
	}
}