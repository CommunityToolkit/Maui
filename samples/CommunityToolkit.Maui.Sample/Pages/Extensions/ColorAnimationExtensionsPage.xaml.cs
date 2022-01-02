using System.Reflection;
using CommunityToolkit.Maui.Extensions;

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

		RateInput ??= new();
		ColorFrame ??= new();
		ColorPicker ??= new();
		EasingPicker ??= new();
		DurationInput ??= new();
		TextColorToDescriptionLabel ??= new();
	}

	protected override void OnAppearing()
	{
		ColorPicker.ItemsSource = _colors.Keys.ToList();
		EasingPicker.ItemsSource = _easings.Keys.ToList();
		SetPickersRandomValue();
	}

	void SetPickersRandomValue()
	{
		ColorPicker.SelectedIndex = new Random().Next(ColorPicker.ItemsSource.Count);
		EasingPicker.SelectedIndex = new Random().Next(EasingPicker.ItemsSource.Count);
	}

	async void Button_Clicked(object sender, EventArgs e)
	{
		var color = _colors.ElementAtOrDefault(ColorPicker.SelectedIndex).Value ?? Colors.Transparent;

		if (!uint.TryParse(DurationInput.Text, out var duration))
			duration = 1500;

		if (!uint.TryParse(RateInput.Text, out var rate))
			rate = 16;

		var easing = _easings.ElementAtOrDefault(EasingPicker.SelectedIndex).Value;

		await Task.WhenAll(ColorFrame.BackgroundColorTo(color, rate, duration, easing),
							TextColorToDescriptionLabel.TextColorTo(color, rate, duration, easing));

		SetPickersRandomValue();
	}
}