using CommunityToolkit.Maui.Markup;
using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.ViewModels;
using static CommunityToolkit.Maui.Markup.GridRowsColumns;

namespace CommunityToolkit.Maui.Sample.Pages;

public abstract class BaseGalleryPage<TViewModel> : BasePage<TViewModel> where TViewModel : BaseGalleryViewModel
{
	protected BaseGalleryPage(string title, IDeviceInfo deviceInfo, TViewModel viewModel) : base(viewModel)
	{
		Title = title;

		Padding = 0;

		Content = new CollectionView
		{
			SelectionMode = SelectionMode.Single,
		}.ItemTemplate(new GalleryDataTemplate())
			.Bind(ItemsView.ItemsSourceProperty,
				static (BaseGalleryViewModel vm) => vm.Items,
				mode: BindingMode.OneTime)
			.Invoke(static collectionView => collectionView.SelectionChanged += HandleSelectionChanged);
	}

	static async void HandleSelectionChanged(object? sender, SelectionChangedEventArgs e)
	{
		ArgumentNullException.ThrowIfNull(sender);

		var collectionView = (CollectionView)sender;
		collectionView.SelectedItem = null;

		if (e.CurrentSelection.FirstOrDefault() is SectionModel sectionModel)
		{
			await Shell.Current.GoToAsync(AppShell.GetPageRoute(sectionModel.ViewModelType));
		}
	}

	sealed class GalleryDataTemplate() : DataTemplate(CreateDataTemplate)
	{

		enum Row { TopPadding, Content, BottomPadding }
		enum Column { LeftPadding, Content, RightPadding }

		static Grid CreateDataTemplate() => new()
		{
			RowDefinitions = Rows.Define(
				(Row.TopPadding, 12),
				(Row.Content, Star),
				(Row.BottomPadding, 12)),

			ColumnDefinitions = Columns.Define(
				(Column.LeftPadding, 24),
				(Column.Content, Star),
				(Column.RightPadding, 24)),

			Children =
			{
				new Card().Row(Row.Content).Column(Column.Content).DynamicResource(StyleProperty, "BorderGalleryCard")
			}
		};

		sealed class Card : Border
		{
			public Card()
			{
				Content = new Grid
				{
					BackgroundColor = Colors.Transparent,

					RowSpacing = 4,

					RowDefinitions = Rows.Define(
						(CardRow.Title, 24),
						(CardRow.Description, Auto)),

					ColumnDefinitions = Columns.Define(Star),

					Children =
					{
						new Label()
							.Row(CardRow.Title)
							.Bind(Label.TextProperty,
								static (SectionModel section) => section.Title,
								mode: BindingMode.OneTime)
							.DynamicResource(Label.StyleProperty, "LabelSectionTitle"),

						new Label
							{
								MaxLines = 4,
								LineBreakMode = LineBreakMode.WordWrap
							}
							.Row(CardRow.Description).TextStart().TextTop()
							.Bind(Label.TextProperty,
								static (SectionModel section) => section.Description,
								mode: BindingMode.OneTime)
							.DynamicResource(Label.StyleProperty, "LabelSectionText")
					}
				};
			}
		}

		enum CardRow { Title, Description }
	}
}