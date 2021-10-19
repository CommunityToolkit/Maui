using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Maui.Graphics;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class ColorsConverterPage : BasePage
{
	readonly IReadOnlyDictionary<string, Color> colors = typeof(Colors)
		.GetFields(BindingFlags.Static | BindingFlags.Public)
		.ToDictionary(c => c.Name, c => (c.GetValue(null) as Color)?? Colors.White);

	public ColorsConverterPage()
	{
		InitializeComponent();
		Picker ??= new();
		BoxView ??= new();
	}

    protected override void OnAppearing()
	{
		var keys = colors.Keys.ToList();
		Picker.ItemsSource = keys;
	}

	void Picker_SelectedIndexChanged(object sender, EventArgs e)
	{
		if (colors.TryGetValue((string)Picker.SelectedItem, out var color))
			BoxView.BackgroundColor = color;
	}
}