using System.Reflection;
using CommunityToolkit.Maui.Sample.ViewModels.Layouts;

namespace CommunityToolkit.Maui.Sample.Pages.Layouts;

public partial class UniformItemsLayoutPage : BasePage<UniformItemsLayoutViewModel>
{
	readonly IReadOnlyList<Color> colors = typeof(Colors)
											.GetFields(BindingFlags.Static | BindingFlags.Public)
											.ToDictionary(c => c.Name, c => (Color)(c.GetValue(null) ?? throw new InvalidOperationException()))
											.Values.ToList();

	public UniformItemsLayoutPage(UniformItemsLayoutViewModel uniformItemsLayoutViewModel)
		: base(uniformItemsLayoutViewModel)
	{
		InitializeComponent();
	}

	void HandleAddButtonClicked(object? sender, System.EventArgs e)
	{
		var randomColor = colors[Random.Shared.Next(colors.Count)];

		UniformItemsLayout_Default.Children.Add(new Button
		{
			BackgroundColor = randomColor
		});

		UniformItemsLayout_MaxRows1.Children.Add(new Button
		{
			BackgroundColor = randomColor
		});

		UniformItemsLayout_MaxColumns1.Children.Add(new Button
		{
			BackgroundColor = randomColor
		});

		var button = new Button
		{
			BackgroundColor = randomColor
		};

		UniformItemsLayout_MaxRows2MaxColumns2.Children.Add(button);
	}
}