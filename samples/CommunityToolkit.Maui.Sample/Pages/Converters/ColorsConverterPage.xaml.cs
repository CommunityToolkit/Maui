using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Maui.Graphics;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class ColorsConverterPage : BasePage
{
	readonly Dictionary<string, Color> colors = typeof(Colors)
		.GetFields(BindingFlags.Static | BindingFlags.Public)
		.ToDictionary(c => c.Name, c => (c.GetValue(null) as Color)?? Colors.White);

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public ColorsConverterPage() => InitializeComponent();
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    protected override void OnAppearing()
	{
		var keys = colors.Keys.ToList();
		picker.ItemsSource = keys;
	}

	void Picker_SelectedIndexChanged(object sender, EventArgs e)
	{
		if (colors.TryGetValue((string)picker.SelectedItem, out var color))
			boxView.BackgroundColor = color;
	}
}