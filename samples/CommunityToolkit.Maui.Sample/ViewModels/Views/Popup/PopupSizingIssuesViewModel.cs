using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class PopupSizingIssuesViewModel : BaseViewModel
{
	[ObservableProperty]
	public partial ContainerModel SelectedContainer { get; set; }

	[ObservableProperty]
	public partial int Padding { get; set; } = 6;

	[ObservableProperty]
	public partial int Adding { get; set; } = 6;

	[ObservableProperty]
	public partial int Margin { get; set; } = 12;

	public PopupSizingIssuesViewModel()
	{
		SelectedContainer = Containers[0];
	}

	public IReadOnlyList<ContainerModel> Containers { get; } =
	[
		new("HorizontalStackLayout", new ControlTemplate(() => new HorizontalStackLayout())),
		new("VerticalStackLayout", new ControlTemplate(() => new VerticalStackLayout())),
		new("Border", new ControlTemplate(() => new Border())),
		new("Grid", new ControlTemplate(() => new Grid())),
		new("CollectionView", new ControlTemplate(() => new CollectionView()))
	];

	[RelayCommand]
	async Task OnShowPopup(Page page)
	{
		var popup = new Popup();

		if (SelectedContainer?.ControlTemplate.LoadTemplate() is not (View container and ContentView contentView))
		{
			await Toast.Make("Invalid Container Selected").Show();
			return;
		}

		container.SetValue(Layout.PaddingProperty, new Thickness(Padding));
		container.SetValue(View.MarginProperty, new Thickness(Margin));

		const string longText = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";
		contentView.Content = GetContentLabel(longText);

		if (container is Layout layout)
		{
			layout.Children.Add(GetContentLabel(longText));
		}
		else if (container is ItemsView itemsView)
		{
			itemsView.ItemsSource = Enumerable.Repeat(longText, 10);
			itemsView.ItemTemplate = new DataTemplate(() => GetContentLabel(longText));
		}

		popup.Content = container;

		page.ShowPopup(popup);
	}

	static Label GetContentLabel(in string text) => new()
	{
		Text = text,
		HorizontalOptions = LayoutOptions.Center,
		VerticalOptions = LayoutOptions.Center
	};
}

public partial class ContainerModel(string name, ControlTemplate controlTemplate) : ObservableObject
{
	public string Name { get; } = name;

	public ControlTemplate ControlTemplate { get; } = controlTemplate;
}