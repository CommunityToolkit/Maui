using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Views.Popups;

/// <summary>
/// This popup demonstrates how a <see cref="Style"/> is applied to a <see cref="Popup"/> implictly
/// through the use of <b>only</b> the <see cref="Style.TargetType"/> property.
/// </summary>
public partial class ImplicitStylePopup : Popup
{
	public ImplicitStylePopup()
	{
		InitializeComponent();
	}
}