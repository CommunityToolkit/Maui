using System;
using System.Linq;
using CommunityToolkit.Maui.Markup;
using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.ViewModels;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using static CommunityToolkit.Maui.Markup.GridRowsColumns;

namespace CommunityToolkit.Maui.Sample.Pages;

public abstract class BaseGalleryPage<TViewModel> : BasePage where TViewModel : BaseGalleryViewModel, new()
{
	public BaseGalleryPage(string title)
	{
		Title = title;
		BindingContext = new TViewModel();

		Padding = new Thickness(20, 0);

		Content = new CollectionView
		{
			SelectionMode = SelectionMode.Single,
			ItemTemplate = new GalleryDataTemplate()
		}.Bind(CollectionView.ItemsSourceProperty, nameof(BaseGalleryViewModel.FilteredItems))
		 .Invoke(collectionView => collectionView.SelectionChanged += HandleSelectionChanged);
	}

	async void HandleSelectionChanged(object? sender, SelectionChangedEventArgs e)
	{
		ArgumentNullException.ThrowIfNull(sender);

		var collectionView = (CollectionView)sender;
		collectionView.SelectedItem = null;

		if (e.CurrentSelection.FirstOrDefault() is SectionModel sectionModel)
			await Navigation.PushAsync(PreparePage(sectionModel));
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
			BackgroundColor = (Color)(Application.Current?.Resources["AppBackgroundColor"] ?? throw new InvalidOperationException()),

			RowDefinitions = Rows.Define(
				(Row.TopPadding, 8),
				(Row.Content, Star),
				(Row.BottomPadding, 8)),

			ColumnDefinitions = Columns.Define(
				(Column.LeftPadding, 6),
				(Column.Content, Star),
				(Column.RightPadding, 6)),

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
						(CardRow.Description, Auto)),

					Children =
					{
						new Label { Style = (Style)(Application.Current?.Resources["label_section_header"] ?? throw new InvalidOperationException()) }
							.Row(CardRow.Title)
							.Bind(Label.TextProperty, nameof(SectionModel.Title)),

						new Label()
							.Row(CardRow.Description)
							.Bind(Label.TextProperty, nameof(SectionModel.Description))
					}
				};
			}
		}

		enum CardRow { Title, Description }
	}
}