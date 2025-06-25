using CommunityToolkit.Maui.Markup;
using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Views.Popups;

public partial class CsharpBindingPopup : Popup
{
	public CsharpBindingPopup(CsharpBindingPopupViewModel csharpBindingPopupViewModel)
	{
		BindingContext = csharpBindingPopupViewModel;

		Content = new VerticalStackLayout
		{
			Children =
			{
				new Label()
					.Font(size: 26, bold: true)
					.TextColor(Colors.Black)
					.TextCenter()
					.Top().Center()
					.Bind(Label.TextProperty,
						getter: static (CsharpBindingPopupViewModel vm) => vm.Title),

				new BoxView()
					.Height(1)
					.Margin(50, 25)
					.BackgroundColor(Color.FromArgb("#c3c3c3")),

				new Label()
					.Center()
					.TextCenter()
					.TextColor(Colors.Black)
					.Bind(Label.TextProperty,
						getter: static (CsharpBindingPopupViewModel vm) => vm.Message)

			}
		};
	}
}