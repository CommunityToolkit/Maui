using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CommunityToolkit.Maui.Extensions;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;

namespace CommunityToolkit.Maui.Sample.Pages.Extensions;

public partial class ColorAnimationExtensionsPage : BasePage
{
	readonly IReadOnlyDictionary<string, Color> _colors = typeof(Colors)
		.GetFields(BindingFlags.Static | BindingFlags.Public)
		.ToDictionary(c => c.Name, c => (Color)(c.GetValue(null) ?? throw new InvalidOperationException()));

	readonly IReadOnlyDictionary<string, Easing> _easings = typeof(Easing)
		.GetFields(BindingFlags.Static | BindingFlags.Public)
		.ToDictionary(c => c.Name, c => (Easing)(c.GetValue(null) ?? throw new InvalidOperationException()));

	public ColorAnimationExtensionsPage()
	{
		InitializeComponent();

		TestPane ??= new();
		ColorPicker ??= new();
		DurationInput ??= new();
		RateInput ??= new();
		EasingPicker ??= new();
	}

	protected override void OnAppearing()
	{
		ColorPicker.ItemsSource = _colors.Keys.ToList();
		EasingPicker.ItemsSource = _easings.Keys.ToList();
		SetPickersRandomValue();
	}

	private void SetPickersRandomValue()
	{
		ColorPicker.SelectedIndex = new Random().Next(ColorPicker.ItemsSource.Count);
		EasingPicker.SelectedIndex = new Random().Next(EasingPicker.ItemsSource.Count);
	}

	private async void Button_Clicked(object sender, EventArgs e)
	{
		var color = _colors.ElementAtOrDefault(ColorPicker.SelectedIndex).Value ?? Colors.Transparent;

		uint duration, rate;

		if (!uint.TryParse(DurationInput.Text, out duration))
			duration = 250;

		if (!uint.TryParse(RateInput.Text, out rate))
			rate = 16;

		var easing = _easings.ElementAtOrDefault(EasingPicker.SelectedIndex).Value;


		await TestPane.ColorTo(color, rate:rate, length: duration, easing: easing);

		SetPickersRandomValue();
	}
}
