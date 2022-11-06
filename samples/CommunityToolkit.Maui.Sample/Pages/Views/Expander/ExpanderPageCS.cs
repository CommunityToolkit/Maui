using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Maui.Markup;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public class ExpanderPageCS : ContentPage
{
	readonly Expander expander;

	public ExpanderPageCS()
	{
		Content = new VerticalStackLayout()
		{
			Children =
			{
				new Label()
					.Text("Expander C# Sample")
					.Font(bold: true, size: 24)
					.CenterHorizontal(),

				new Expander
				{
					Header = new Label()
								.Text("Expander (Click Me)")
								.Font(bold: true, size: 18),

					Direction = ExpandDirection.Down,

					Content = new VerticalStackLayout()
					{
						new Image()
							.Source("https://avatars.githubusercontent.com/u/9011267?v=4")
							.Size(120)
							.Aspect(Aspect.AspectFit),

						new Label()
							.Text(".NET Multi-platform App UI (.NET MAUI) is a cross-platform framework for creating mobile and desktop apps with C# and XAML. Using .NET MAUI, you can develop apps that can run on Android, iOS, iPadOS, macOS, and Windows from a single shared codebase.")
							.Font(italic: true)

					}.Padding(10)

				}.CenterHorizontal()
				 .Assign(out expander),

				new Picker() { ItemsSource = Enum.GetValues<ExpandDirection>(), Title = "Direction" }
					.CenterHorizontal().TextCenter()
					.Invoke(picker => picker.SelectedIndexChanged += HandleSelectedIndexChanged),
			}
		};


	}

	void HandleSelectedIndexChanged(object? sender, EventArgs e)
	{
		ArgumentNullException.ThrowIfNull(sender);

		var picker = (Picker)sender;
		expander.Direction = Enum.Parse<ExpandDirection>(picker.SelectedIndex.ToString());
	}
}