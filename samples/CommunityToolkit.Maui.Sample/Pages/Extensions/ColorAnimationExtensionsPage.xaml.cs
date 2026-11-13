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

	readonly HeadlineView headlineView = new("My text color is animated by the generated TextColorTo extension method");

	public ColorAnimationExtensionsPage(ColorAnimationExtensionsViewModel colorAnimationExtensionsViewModel)
		: base(colorAnimationExtensionsViewModel)
	{
		InitializeComponent();
		HeadlineLayout.Add(headlineView);
	}

	protected override void OnAppearing()
	{
		ColorPicker.ItemsSource = colors.Keys.ToList();
		EasingPicker.ItemsSource = easings.Keys.ToList();
		SetPickersRandomValue();
	}

	void SetPickersRandomValue()
	{
		ColorPicker.SelectedIndex = Random.Shared.Next(ColorPicker.ItemsSource.Count);
		EasingPicker.SelectedIndex = Random.Shared.Next(EasingPicker.ItemsSource.Count);
	}

	async void Button_Clicked(object? sender, EventArgs? e)
	{
		var color = colors.ElementAtOrDefault(ColorPicker.SelectedIndex).Value ?? Colors.Transparent;

		if (!uint.TryParse(DurationInput.Text, out var duration))
		{
			duration = 1500;
		}

		// The rate is the time between two frames of the animation, so it must be at least 1 ms: BackgroundColorTo and TextColorTo cannot animate with a rate of 0
		if (!uint.TryParse(RateInput.Text, out var rate) || rate is 0)
		{
			rate = 16;
		}

		var easing = easings.ElementAtOrDefault(EasingPicker.SelectedIndex).Value;

		await Task.WhenAll(ColorBorder.BackgroundColorTo(color, rate, duration, easing),
							TextColorToDescriptionLabel.TextColorTo(color, rate, duration, easing),
							headlineView.TextColorTo(color, rate, duration, easing));

		SetPickersRandomValue();
	}

	// Deriving from a MAUI text control would make the generator reuse that control's TextColorTo, so this view only implements ITextStyle
	internal sealed class HeadlineView : ContentView, ITextStyle
	{
		readonly Label label;

		public HeadlineView(string text)
		{
			label = new Label { Text = text };
			Content = label;
		}

		public Color TextColor
		{
			get => label.TextColor;
			set => label.TextColor = value;
		}

		public Microsoft.Maui.Font Font => ((ITextStyle)label).Font;

		public double CharacterSpacing => ((ITextStyle)label).CharacterSpacing;
	}
}