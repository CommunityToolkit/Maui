using IElement = Microsoft.Maui.IElement;
using LayoutAlignment = Microsoft.Maui.Primitives.LayoutAlignment;

namespace CommunityToolkit.Maui.Core;

public interface IBasePopup : IElement
{
	IView? Anchor { get; }
	Color Color { get; }
	IView? Content { get; }
	LayoutAlignment HorizontalOptions { get; }
	bool IsLightDismissEnabled { get; }
	Size Size { get; }
	LayoutAlignment VerticalOptions { get; }
	void OnDismissed(object? result);
	void OnOpened();
	void LightDismiss();
}