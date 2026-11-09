using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Markup;
using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;
using static CommunityToolkit.Maui.Markup.GridRowsColumns;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class TouchBehaviorCollectionViewMultipleSelectionPage : BasePage<TouchBehaviorCollectionViewMultipleSelectionViewModel>
{
	readonly CollectionView collectionView;

	public TouchBehaviorCollectionViewMultipleSelectionPage(TouchBehaviorCollectionViewMultipleSelectionViewModel viewModel) : base(viewModel)
	{
		// The CollectionView gets the height the description leaves, so that it scrolls through the creators below the screen;
		// a StackLayout would make it as tall as all of its items
		Content = new Grid
		{
			RowDefinitions = Rows.Define(
				(Row.Description, Auto),
				(Row.ContentCreators, Star)),

			Children =
			{
				new Label()
					.Row(Row.Description)
					.Text($"This page demonstrates how to use the TouchBehavior inside of a CollectionView when {nameof(SelectionMode)}.{nameof(SelectionMode.Multiple)} is in use")
					.Center()
					.TextCenter(),

				new CollectionView { SelectionMode = SelectionMode.Multiple }
					.Row(Row.ContentCreators)
					.ItemTemplate(new CreatorsDataTemplate(viewModel))
					.Invoke(collectionView => collectionView.SelectionChanged += HandleSelectionChanged)
					.Bind(CollectionView.ItemsSourceProperty,
						getter: static (TouchBehaviorCollectionViewMultipleSelectionViewModel vm) => vm.ContentCreators,
						mode: BindingMode.OneTime)
					.Assign(out collectionView)
			}
		};
	}

	enum Row { Description, ContentCreators }

	async void HandleSelectionChanged(object? sender, SelectionChangedEventArgs e)
	{
		await Toast.Make($"Number of Creators Selected: {collectionView.SelectedItems?.Count ?? 0}").Show();
	}

	sealed class CreatorsDataTemplate(TouchBehaviorCollectionViewMultipleSelectionViewModel viewModel) : DataTemplate(() => CreateLayout(viewModel))
	{
		static VerticalStackLayout CreateLayout(TouchBehaviorCollectionViewMultipleSelectionViewModel viewModel) => new VerticalStackLayout
		{
			Children =
			{
				new Label()
					.Center()
					.Bind(Label.TextProperty,
						getter: static (ContentCreator creator) => creator.Resource,
						mode: BindingMode.OneTime),

				new Image()
					.Size(100, 100)
					.Bind(Image.SourceProperty,
						getter: static (ContentCreator creator) => creator.Image,
						mode: BindingMode.OneTime)
			}
		}.Assign(out VerticalStackLayout stackLayout)
		 .Behaviors(new TouchBehavior()
			 .Bind(BindableObject.BindingContextProperty,
				 getter: static (VerticalStackLayout layout) => layout.BindingContext,
				 source: stackLayout)
			 .Bind(TouchBehavior.CommandProperty,
				 getter: static vm => vm.RowTappedCommand,
				 source: viewModel)
			 .Bind(TouchBehavior.CommandParameterProperty));
	}
}