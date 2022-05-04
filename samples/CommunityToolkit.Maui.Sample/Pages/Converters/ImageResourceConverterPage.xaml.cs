using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class ImageResourceConverterPage : BasePage<ImageResourceConverterViewModel>
{
	public ImageResourceConverterPage(ImageResourceConverterViewModel imageResourceConverterViewModel)
		: base(imageResourceConverterViewModel)
	{
		InitializeComponent();
	}
}