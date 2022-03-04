using System.Reflection;
using System.Security.Cryptography;
using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class ColorsConverterPage : BasePage<ColorsConverterViewModel>
{
	readonly IReadOnlyDictionary<string, Color> colors = typeof(Colors)
		.GetFields(BindingFlags.Static | BindingFlags.Public)
		.ToDictionary(c => c.Name, c => (Color)(c.GetValue(null) ?? throw new InvalidOperationException()));

	public ColorsConverterPage(ColorsConverterViewModel colorsConvertersViewModel)
		: base(colorsConvertersViewModel)
	{
		InitializeComponent();

		Picker ??= new();
		BoxView ??= new();
	}

	protected override void OnAppearing()
	{
		Picker.ItemsSource = colors.Keys.ToList();
		Picker.SelectedIndex = RandomNumberGenerator.GetInt32(Picker.ItemsSource.Count);
	}

	void HandleSelectedIndexChanged(object? sender, EventArgs e)
	{
		if (colors.TryGetValue((string)Picker.SelectedItem, out var color))
		{
			BoxView.BackgroundColor = color;
		}
	}
}