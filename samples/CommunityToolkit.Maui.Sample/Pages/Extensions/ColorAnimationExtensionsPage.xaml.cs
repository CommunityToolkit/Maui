using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Extensions;
using Microsoft.Maui.Graphics;

namespace CommunityToolkit.Maui.Sample.Pages.Extensions;

public partial class ColorAnimationExtensionsPage : BasePage
{
	public ColorAnimationExtensionsPage()
	{
		InitializeComponent();

		TestPane ??= new();
	}

	private async void Button_Clicked(object sender, EventArgs e)
	{
		var r = new Random().Next(255) / 255f;
		var g = new Random().Next(255) / 255f;
		var b = new Random().Next(255) / 255f;

		var c = new Color(r, g, b);

		await TestPane.ColorTo(c, length: 200);
	}
}
