using System;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class BoolToObjectConverterPage : BasePage
{
    public BoolToObjectConverterPage()
    {
        InitializeComponent();
		CheckBox ??= new();
		Ellipse ??= new();
	}

	void OnButtonClicked(object sender, EventArgs args)
	{
		if (sender is not Button button)
			return;
		Ellipse.Fill = new SolidColorBrush(button.BackgroundColor);
	}
}