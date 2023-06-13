using System.Reflection;
using System.Security.Cryptography;
using CommunityToolkit.Maui.Converters;
using CommunityToolkit.Maui.Markup;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.Views.Popups;

public class NestedPopup : Popup
{
	readonly IReadOnlyDictionary<string, Color> colors = typeof(Colors)
		.GetFields(BindingFlags.Static | BindingFlags.Public)
		.ToDictionary(c => c.Name, c => (Color)(c.GetValue(null) ?? throw new InvalidOperationException()));

	public NestedPopup()
	{
		Content = new Button
		{
			Command = new AsyncRelayCommand(ButtonTappedCommand)
		}.Padding(42, 24)
		 .Text("Open Popup")
		 .BackgroundColor(GenerateRandomColor())
		 .Assign(out Button button)
		 .Bind(Button.TextColorProperty,
				nameof(button.BackgroundColor),
				converter: new ColorToColorForTextConverter(),
				source: button);
	}

	Color GenerateRandomColor()
	{
		var colorNames = colors.Keys.ToList();
		var randomNumber = RandomNumberGenerator.GetInt32(colorNames.Count);

		return colors.ElementAt(randomNumber).Value;
	}

	async Task ButtonTappedCommand()
	{
		if (Parent is Page page)
		{
			await page.ShowPopupAsync(new NestedPopup());
		}
	}
}

