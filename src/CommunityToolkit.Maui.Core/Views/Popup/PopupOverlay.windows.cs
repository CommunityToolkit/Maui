using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// Displays Overlay in the Popup background.
/// </summary>
public class PopupOverlay : WindowOverlay
{
	IWindowOverlayElement popupOverlayElement;

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
		RectF overlayRect = new RectF();
		Color overlayColor = Color.FromRgba(255, 255, 255, 125);

		public PopupOverlayElement(IWindowOverlay overlay, Color? overlayColor = null)
		{
			this.overlay = overlay;
			if (overlayColor != null)
			{
				this.overlayColor = overlayColor;
			}
		}

		public bool Contains(Point point)
		{
			return this.overlayRect.Contains(new Point(point.X / overlay.Density, point.Y / overlay.Density));
		}

		public void Draw(ICanvas canvas, RectF dirtyRect)
		{
			this.overlayRect = dirtyRect;
			canvas.FillColor = overlayColor;
			canvas.FillRectangle(dirtyRect.X, dirtyRect.Y, dirtyRect.Width, dirtyRect.Height);
		}
	}
}
