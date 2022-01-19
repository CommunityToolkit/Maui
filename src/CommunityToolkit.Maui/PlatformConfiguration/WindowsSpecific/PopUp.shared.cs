using WindowsPlatform = Microsoft.Maui.Controls.PlatformConfiguration.Windows;
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

	public static IPlatformElementConfiguration<WindowsPlatform, MCTElement> SetBorderColor(this IPlatformElementConfiguration<WindowsPlatform, MCTElement> config, Color value)
	{
		SetBorderColor(config.Element, value);
		return config;
	}
}
