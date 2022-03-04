using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class ByteArrayToImageSourceConverterPage : BasePage<ByteArrayToImageSourceConverterViewModel>
{
	public ByteArrayToImageSourceConverterPage(ByteArrayToImageSourceConverterViewModel byteArrayToImageSourceConverterViewModel)
		: base(byteArrayToImageSourceConverterViewModel)
	{
		InitializeComponent();
	}
}

