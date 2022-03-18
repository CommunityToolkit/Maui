using CommunityToolkit.Maui.Sample.ViewModels.Converters;
using Button = Microsoft.Maui.Controls.Button;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class BoolToObjectConverterPage : BasePage<BoolToObjectConverterViewModel>
{
	public BoolToObjectConverterPage(IDeviceInfo deviceInfo, BoolToObjectConverterViewModel boolToObjectConverterViewModel)
		: base(deviceInfo, boolToObjectConverterViewModel)
	{
		InitializeComponent();

		CheckBox ??= new();
		Ellipse ??= new();
	}

	void OnButtonClicked(object? sender, EventArgs args)
	{
		ArgumentNullException.ThrowIfNull(sender);

		var button = (Button)sender;

		Ellipse.Fill = new SolidColorBrush(button.BackgroundColor);
	}
}