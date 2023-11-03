namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// Displays Overlay in the Popup background.
/// </summary>
class PopupOverlay : WindowOverlay
{
	readonly IWindowOverlayElement popupOverlayElement;

	/// <summary>
	/// Instantiates a new instance of <see cref="PopupOverlay"/>.
	/// </summary>
	/// <param name="window">An instance of <see cref="IWindow"/>.</param>
	/// <param name="overlayColor">Popup overlay color</param>
	public PopupOverlay(IWindow window, Color? overlayColor = null) : base(window)
	{
		popupOverlayElement = new PopupOverlayElement(this, overlayColor);

		AddWindowElement(popupOverlayElement);

		EnableDrawableTouchHandling = true;
	}

	class PopupOverlayElement : IWindowOverlayElement
	{
		readonly IWindowOverlay overlay;
		readonly Color overlayColor = Color.FromRgba(255, 255, 255, 153); // 60% Opacity 

		RectF overlayRect = new();

		public PopupOverlayElement(IWindowOverlay overlay, Color? overlayColor = null)
		{
			this.overlay = overlay;
			if (overlayColor is not null)
			{
				this.overlayColor = overlayColor;
			}
		}

		public bool Contains(Point point)
		{
			return overlayRect.Contains(new Point(point.X / overlay.Density, point.Y / overlay.Density));
		}

		public void Draw(ICanvas canvas, RectF dirtyRect)
		{
			overlayRect = dirtyRect;
			canvas.FillColor = overlayColor;
			canvas.FillRectangle(dirtyRect.X, dirtyRect.Y, dirtyRect.Width, dirtyRect.Height);
		}
	}
}