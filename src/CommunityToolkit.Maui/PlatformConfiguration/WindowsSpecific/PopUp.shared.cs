using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.PlatformConfiguration;
using MCTElement = CommunityToolkit.Maui.Views.BasePopup;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.PlatformConfiguration.WindowsSpecific;

public static class PopUp
{
	public static readonly BindableProperty BorderColorProperty = BindableProperty.Create(
		"BorderColor", typeof(Color), typeof(BasePopup), default(Color));

	public static void SetBorderColor(BindableObject element, Color color) =>
		element.SetValue(BorderColorProperty, color);

	public static Color GetBorderColor(BindableObject element) =>
		(Color)element.GetValue(BorderColorProperty);

	public static IPlatformElementConfiguration<Windows, MCTElement> SetBorderColor(this IPlatformElementConfiguration<Windows, MCTElement> config, Color value)
	{
		SetBorderColor(config.Element, value);
		return config;
	}
}
