using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Views.Popups;

/// <summary>
/// This popup demonstrates how the <see cref="Style.ApplyToDerivedTypes"/> property can allow for Popup implementations
/// that inherit from <see cref="Popup"/> to still use a common <see cref="Style"/> definition.
/// </summary>
public partial class StyleInheritancePopup : Popup
{
	public StyleInheritancePopup()
	{
		InitializeComponent();
	}
}