using System.Reflection;
using System.Security.Cryptography;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class ColorsConverterPage : BasePage
{
	readonly IReadOnlyDictionary<string, Color> colors = typeof(Colors)
		.GetFields(BindingFlags.Static | BindingFlags.Public)
		.ToDictionary(c => c.Name, c => (Color)(c.GetValue(null) ?? throw new InvalidOperationException()));

	public ColorsConverterPage()
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