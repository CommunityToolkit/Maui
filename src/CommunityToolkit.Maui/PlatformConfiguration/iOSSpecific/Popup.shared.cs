using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.PlatformConfiguration;
using MCTElement = CommunityToolkit.Maui.Views.BasePopup;

namespace CommunityToolkit.Maui.PlatformConfiguration.iOSSpecific;

public static class PopUp
{
	public static readonly BindableProperty ArrowDirectionProperty = BindableProperty.Create(
		"ArrowDirection", typeof(PopoverArrowDirection), typeof(MCTElement), PopoverArrowDirection.None);

	public static void SetArrowDirection(BindableObject element, PopoverArrowDirection color) =>
		element.SetValue(ArrowDirectionProperty, color);

	public static PopoverArrowDirection GetArrowDirection(BindableObject element) =>
		(PopoverArrowDirection)element.GetValue(ArrowDirectionProperty);

	public static IPlatformElementConfiguration<iOS, MCTElement> UseArrowDirection(this IPlatformElementConfiguration<iOS, MCTElement> config, PopoverArrowDirection value)
	{
		SetArrowDirection(config.Element, value);
		return config;
	}
}

public enum PopoverArrowDirection
{
	None,
	Up,
	Down,
	Left,
	Right,
	Any,
	Unknown
}
