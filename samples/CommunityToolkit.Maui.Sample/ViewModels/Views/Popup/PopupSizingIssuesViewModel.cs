using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Markup;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class PopupSizingIssuesViewModel : BaseViewModel
{
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

	[ObservableProperty]
	public partial ContainerModel SelectedContainer { get; set; }

	[ObservableProperty]
	public partial int Padding { get; set; } = 6;

	[ObservableProperty]
	public partial int Margin { get; set; } = 12;

	[RelayCommand]
	async Task OnShowPopup()
	{
		var popup = new Popup();

		if (SelectedContainer.ControlTemplate.LoadTemplate() is not View view)
		{
			await Toast.Make("Invalid Container Selected").Show();
			return;
		}

		view.SetValue(View.MarginProperty, new Thickness(Margin));

		const string longText = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";

		switch (view)
		{
			case Layout layout:
				layout.Children.Add(GetContentLabel(longText));
				layout.SetValue(Layout.PaddingProperty, new Thickness(Padding));
				break;

			case ItemsView itemsView:
				itemsView.ItemsSource = Enumerable.Repeat(longText, 10);
				itemsView.ItemTemplate = new DataTemplate(() => GetContentLabel(longText));
				break;

			case Border border:
				border.Content = GetContentLabel(longText);
				break;

			default:
				throw new NotSupportedException($"{view.GetType().FullName} is not yet supported");
		}

		popup.Content = view;
		popup.Margin = Margin;
		popup.Padding = Padding;

		if (Application.Current?.Windows[0] is not { Page: not null } window)
		{
			throw new InvalidOperationException("Unable to find page");
		}

		window.Page.ShowPopup(popup);
	}

	static Label GetContentLabel(in string text) => new Label { LineBreakMode = LineBreakMode.WordWrap }.Text(text, Colors.Black).Center();
}

public partial class ContainerModel(in string name, in ControlTemplate controlTemplate) : ObservableObject
{
	public string Name { get; } = name;

	public ControlTemplate ControlTemplate { get; } = controlTemplate;

	public override string ToString() => Name;
}