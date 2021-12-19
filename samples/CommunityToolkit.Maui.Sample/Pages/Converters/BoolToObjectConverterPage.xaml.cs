using Microsoft.Maui.Controls;
using Button = Microsoft.Maui.Controls.Button;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class BoolToObjectConverterPage : BasePage
{
	public BoolToObjectConverterPage()
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