using CommunityToolkit.Maui.Markup;
using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.ViewModels;
using static CommunityToolkit.Maui.Markup.GridRowsColumns;
using Application = Microsoft.Maui.Controls.Application;

namespace CommunityToolkit.Maui.Sample.Pages;

public abstract class BaseGalleryPage<TViewModel> : BasePage<TViewModel> where TViewModel : BaseGalleryViewModel
{
	protected BaseGalleryPage(string title, IDeviceInfo deviceInfo, TViewModel viewModel) : base(deviceInfo, viewModel)
	{
		Title = title;

		if (deviceInfo.Platform == DevicePlatform.iOS && deviceInfo.Idiom == DeviceIdiom.Phone) // iOS Phones
		{
			Padding = new Thickness(0, 96, 0, 0);
		}
		else if (deviceInfo.Platform == DevicePlatform.iOS || deviceInfo.Platform == DevicePlatform.MacCatalyst) //iOS Tablets + MacCatalyst
		{
			Padding = new Thickness(0, 84, 0, 0);
		}
		else
		{
			Padding = 0;
		}

		Content = new CollectionView
		{
			SelectionMode = SelectionMode.Single,
			ItemTemplate = new GalleryDataTemplate()
		}.Bind(ItemsView.ItemsSourceProperty, nameof(BaseGalleryViewModel.Items))
		 .Invoke(collectionView => collectionView.SelectionChanged += HandleSelectionChanged);
	}

	async void HandleSelectionChanged(object? sender, SelectionChangedEventArgs e)
	{
		ArgumentNullException.ThrowIfNull(sender);

		var collectionView = (CollectionView)sender;
		collectionView.SelectedItem = null;

		if (e.CurrentSelection.FirstOrDefault() is not SectionModel sectionModel)
		{
			return;
		}

		await Shell.Current.GoToAsync(AppShell.GetPageRoute(sectionModel.ViewModelType));
	}

	class GalleryDataTemplate : DataTemplate
	{
		public GalleryDataTemplate() : base(CreateDataTemplate)
		{

		}

		enum Row { TopPadding, Content, BottomPadding }
		enum Column { LeftPadding, Content, RightPadding }

		static Grid CreateDataTemplate() => new()
		{
			BackgroundColor = (Color)(Application.Current?.Resources["AppBackgroundColor"] ?? throw new InvalidOperationException("Application.Current cannot be null")),

			RowDefinitions = Rows.Define(
				(Row.TopPadding, 12),
				(Row.Content, GridLength.Star),
				(Row.BottomPadding, 12)),

			ColumnDefinitions = Columns.Define(
				(Column.LeftPadding, 24),
				(Column.Content, GridLength.Star),
				(Column.RightPadding, 24)),

			Children =
			{
				new Card().Row(Row.Content).Column(Column.Content)
			}
		};

		class Card : Frame
		{
			public Card()
			{
				Style = (Style)(Application.Current?.Resources["card"] ?? throw new InvalidOperationException());

				Content = new Grid
				{
					RowSpacing = 4,

					RowDefinitions = Rows.Define(
						(CardRow.Title, 24),
						(CardRow.Description, GridLength.Auto)),

					ColumnDefinitions = Columns.Define(GridLength.Star),

					Children =
					{
						new Label { Style = (Style)(Application.Current?.Resources["label_section_header"] ?? throw new InvalidOperationException()) }
							.Row(CardRow.Title)
							.Bind(Label.TextProperty, nameof(SectionModel.Title)),

						new Label { MaxLines = 4, LineBreakMode = LineBreakMode.WordWrap }
							.Row(CardRow.Description).TextStart().TextTop()
							.Bind(Label.TextProperty, nameof(SectionModel.Description))
					}
				};
			}
		}

		enum CardRow { Title, Description }
	}
}