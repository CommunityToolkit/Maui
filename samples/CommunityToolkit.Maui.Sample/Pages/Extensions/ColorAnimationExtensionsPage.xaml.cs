using System.Reflection;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Extensions;

public partial class ColorAnimationExtensionsPage : BasePage<ColorAnimationExtensionsViewModel>
{
	readonly IReadOnlyDictionary<string, Color> colors = typeof(Colors)
		.GetFields(BindingFlags.Static | BindingFlags.Public)
		.ToDictionary(c => c.Name, c => (Color)(c.GetValue(null) ?? throw new InvalidOperationException()));

	readonly IReadOnlyDictionary<string, Easing> easings = typeof(Easing)
		.GetFields(BindingFlags.Static | BindingFlags.Public)
		.ToDictionary(c => c.Name, c => (Easing)(c.GetValue(null) ?? throw new InvalidOperationException()));

	public ColorAnimationExtensionsPage(ColorAnimationExtensionsViewModel colorAnimationExtensionsViewModel)
		: base(colorAnimationExtensionsViewModel)
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
		ColorPicker.ItemsSource = colors.Keys.ToList();
		EasingPicker.ItemsSource = easings.Keys.ToList();
		SetPickersRandomValue();
	}

	void SetPickersRandomValue()
	{
		ColorPicker.SelectedIndex = new Random().Next(ColorPicker.ItemsSource.Count);
		EasingPicker.SelectedIndex = new Random().Next(EasingPicker.ItemsSource.Count);
	}

	async void Button_Clicked(object sender, EventArgs e)
	{
		var color = colors.ElementAtOrDefault(ColorPicker.SelectedIndex).Value ?? Colors.Transparent;

		if (!uint.TryParse(DurationInput.Text, out var duration))
		{
			duration = 1500;
		}

		if (!uint.TryParse(RateInput.Text, out var rate))
		{
			rate = 16;
		}

		var easing = easings.ElementAtOrDefault(EasingPicker.SelectedIndex).Value;

		await Task.WhenAll(ColorFrame.BackgroundColorTo(color, rate, duration, easing),
							TextColorToDescriptionLabel.TextColorTo(color, rate, duration, easing));

		SetPickersRandomValue();
	}
}