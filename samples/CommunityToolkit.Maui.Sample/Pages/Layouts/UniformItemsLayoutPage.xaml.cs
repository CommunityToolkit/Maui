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
		UniformItemsLayout_Default ??= new();
		UniformItemsLayout_MaxColumns1 ??= new();
		UniformItemsLayout_MaxRows1 ??= new();
		UniformItemsLayout_MaxRows2MaxColumns2 ??= new();
	}

	void HandleAddButtonClicked(object? sender, System.EventArgs e)
	{
		const int widthRequest = 25;
		const int heightRequest = 25;
		var randomColor = colors[new Random().Next(colors.Count)];

		UniformItemsLayout_Default.Children.Add(new BoxView
		{
			HeightRequest = widthRequest,
			WidthRequest = heightRequest,
			Color = randomColor
		});

		UniformItemsLayout_MaxRows1.Children.Add(new BoxView
		{
			HeightRequest = widthRequest,
			WidthRequest = heightRequest,
			Color = randomColor
		});

		UniformItemsLayout_MaxColumns1.Children.Add(new BoxView
		{
			HeightRequest = widthRequest,
			WidthRequest = heightRequest,
			Color = randomColor
		});

		var boxView = new BoxView
		{
			HeightRequest = widthRequest,
			WidthRequest = heightRequest,
			Color = randomColor
		};

		UniformItemsLayout_MaxRows2MaxColumns2.Children.Add(boxView);
	}
}
