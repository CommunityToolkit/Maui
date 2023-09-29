using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Views.Popups;

/// <summary>
/// This popup demonstrates how the ApplyToDerivedTypes property can allow for Popup implementations
/// that inherit from Popup to still use a common Style definition.
/// </summary>
public partial class ApplyToDerivedTypesPopup : Popup
{
	public ApplyToDerivedTypesPopup()
	{
		InitializeComponent();
	}
}