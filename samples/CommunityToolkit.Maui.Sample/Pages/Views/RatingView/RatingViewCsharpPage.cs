// Ignore Spelling: csharp, color, colors

using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Markup;
using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls.Shapes;
using static CommunityToolkit.Maui.Markup.GridRowsColumns;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public class RatingViewCsharpPage : BasePage<RatingViewCsharpViewModel>
{
	public RatingViewCsharpPage(RatingViewCsharpViewModel viewModel) : base(viewModel)
	{
		Title = "RatingView C# Syntax";

		Content = new ScrollView
		{
			Content = new Grid
			{
				RowDefinitions = Rows.Define(
					(Row.DefaultsHeader, SectionHeader.RequestedHeight),
					(Row.DefaultsRatingView, Auto),
					(Row.DefaultsRatingViewUsingProperties, Auto),
					(Row.DefaultsRatingViewUsingStyles, Auto),
					(Row.AvailableShapesHeader, SectionHeader.RequestedHeight),
					(Row.AvailableShapesStar, Auto),
					(Row.AvailableShapesCircle, Auto),
					(Row.AvailableShapesHeart, Auto),
					(Row.AvailableShapesLike, Auto),
					(Row.AvailableShapesDislike, Auto),
					(Row.AvailableShapesCustomDog, Auto),
					(Row.AvailableShapesCustomLogo, Auto),
					(Row.MaximumRatingsHeader, SectionHeader.RequestedHeight),
					(Row.MaximumRatingsStepper, Auto),
					(Row.MaximumRatingsRatingView, Auto),
					(Row.ColorsHeader, SectionHeader.RequestedHeight),
					(Row.ColorsEmptyRatingViewPicker, Auto),
					(Row.ColorsFilledRatingViewPicker, Auto),
					(Row.ColorsBorderRatingViewPicker, Auto),
					(Row.ColorsShapeFillTitle, Auto),
					(Row.ColorsShapeFillRatingView, Auto),
					(Row.ColorsItemFillTitle, Auto),
					(Row.ColorsItemFillRatingView, Auto),
					(Row.BorderThicknessHeader, SectionHeader.RequestedHeight),
					(Row.BorderThicknessStepper, Auto),
					(Row.BorderThicknessRatingView, Auto),
					(Row.ReadOnlyHeader, SectionHeader.RequestedHeight),
					(Row.ReadOnlyCheckBox, Auto),
					(Row.ReadOnlyRatingView, Auto),
					(Row.PaddingHeader, SectionHeader.RequestedHeight),
					(Row.PaddingLeftStepper, Auto),
					(Row.PaddingTopStepper, Auto),
					(Row.PaddingRightStepper, Auto),
					(Row.PaddingBottomStepper, Auto),
					(Row.PaddingRatingView, Auto),
					(Row.RatingHeader, SectionHeader.RequestedHeight),
					(Row.RatingSlider, Auto),
					(Row.RatingShapeFillTitle, Auto),
					(Row.RatingShapeFillRatingView, Auto),
					(Row.RatingItemFillTitle, Auto),
					(Row.RatingItemFillRatingView, Auto),
					(Row.SizingHeader, SectionHeader.RequestedHeight),
					(Row.SizingRatingViewSmallest, Auto),
					(Row.SizingRatingViewSmaller, Auto),
					(Row.SizingRatingViewLarger, Auto),
					(Row.SizingRatingViewLargest, Auto),
					(Row.SpacingHeader, SectionHeader.RequestedHeight),
					(Row.SpacingStepper, Auto),
					(Row.SpacingRatingView, Auto)),

				ColumnDefinitions = Columns.Define(
					(Column.Input, 56),
					(Column.Result, Star)),

				Padding = new Thickness(0, 6, 0, 0),
				RowSpacing = 12,
				Children =
				{
					new SectionHeader("Defaults")
						.Row(Row.DefaultsHeader).ColumnSpan(All<Column>()),

					new HorizontalStackLayout
					{
						Spacing = 8,
						Children =
						{
							new Label()
								.Text("Default")
								.CenterVertical(),

							new RatingView()
								.SemanticDescription("A RatingView showing the defaults.")
						}
					}.Row(Row.DefaultsRatingView).ColumnSpan(All<Column>()),

					new HorizontalStackLayout
					{
						Spacing = 8,
						Children =
						{
							new Label()
								.Text("Using Properties")
								.CenterVertical(),

							new RatingView
							{
								BackgroundColor = Colors.Red,
								EmptyColor = Colors.Green,
								FilledColor = Colors.Blue,
								MaximumRating = 5,
								Rating = 2.5
							}.SemanticDescription("A RatingView customised by setting properties.")
						}
					}.Row(Row.DefaultsRatingViewUsingProperties).ColumnSpan(All<Column>()),

					new HorizontalStackLayout
					{
						Spacing = 8,
						Children =
						{
							new Label()
								.Text("Using Styles")
								.CenterVertical(),

							new RatingView
							{
								MaximumRating = 5,
								Rating = 2.5,
								Style = new Style<RatingView>()
									.Add(RatingView.EmptyColorProperty, Colors.Green)
									.Add(RatingView.FilledColorProperty, Colors.Blue)
									.Add(BackgroundColorProperty, Colors.Red)
							}.SemanticDescription("A RatingView customised by setting a style.")
						}
					}.Row(Row.DefaultsRatingViewUsingStyles).ColumnSpan(All<Column>()),

					new SectionHeader("Available Shapes, and example custom shape")
						.Row(Row.AvailableShapesHeader).ColumnSpan(All<Column>()),

					new Label()
						.Row(Row.AvailableShapesStar).Column(Column.Input)
						.Text("Star")
						.CenterVertical(),

					new RatingView
						{
							ItemShapeSize = 40,
							MaximumRating = 5,
							EmptyColor = Colors.White,
							FilledColor = Colors.Blue,
							Rating = 2,
							Shape = RatingViewShape.Star,
							ShapeBorderThickness = 1
						}.Row(Row.AvailableShapesStar).Column(Column.Result)
						.SemanticDescription("A RatingView showing the 'Star' shape."),

					new Label()
						.Row(Row.AvailableShapesCircle).Column(Column.Input)
						.Text("Circle")
						.CenterVertical(),

					new RatingView
						{
							ItemShapeSize = 40,
							MaximumRating = 5,
							EmptyColor = Colors.Red,
							FilledColor = Colors.Blue,
							Rating = 2,
							Shape = RatingViewShape.Circle,
							ShapeBorderThickness = 1
						}.Row(Row.AvailableShapesCircle).Column(Column.Result)
						.SemanticDescription("A RatingView showing the 'Circle' shape."),

					new Label()
						.Row(Row.AvailableShapesHeart).Column(Column.Input)
						.Text("Heart")
						.CenterVertical(),

					new RatingView
						{
							ItemShapeSize = 40,
							MaximumRating = 5,
							FilledColor = Colors.White,
							Rating = 5,
							Shape = RatingViewShape.Heart,
							ShapeBorderThickness = 1
						}.Row(Row.AvailableShapesHeart).Column(Column.Result)
						.SemanticDescription("A RatingView showing the 'Heart' shape."),

					new Label()
						.Row(Row.AvailableShapesLike).Column(Column.Input)
						.Text("Like")
						.CenterVertical(),

					new RatingView
						{
							ItemShapeSize = 40,
							MaximumRating = 5,
							Rating = 5,
							FilledColor = Colors.Red,
							Shape = RatingViewShape.Like,
							ShapeBorderThickness = 1
						}.Row(Row.AvailableShapesLike).Column(Column.Result)
						.SemanticDescription("A RatingView showing the 'Like' shape."),

					new Label()
						.Row(Row.AvailableShapesDislike).Column(Column.Input)
						.Text("Dislike")
						.CenterVertical(),

					new RatingView
						{
							ItemShapeSize = 40,
							MaximumRating = 5,
							Rating = 5,
							FilledColor = Colors.White,
							Shape = RatingViewShape.Dislike,
							ShapeBorderThickness = 1
						}
						.Row(Row.AvailableShapesDislike).Column(Column.Result)
						.SemanticDescription("A RatingView showing the 'Dislike' shape."),

					new Label()
						.Row(Row.AvailableShapesCustomDog).Column(Column.Input)
						.Text("Custom")
						.CenterVertical(),

					new RatingView
						{
							CustomShape =
								"M3.15452 1.01195C5.11987 1.32041 7.17569 2.2474 8.72607 3.49603C9.75381 3.17407 10.8558 2.99995 12 2.99995C13.1519 2.99995 14.261 3.17641 15.2946 3.5025C16.882 2.27488 18.8427 1.31337 20.8354 1.01339C21.2596 0.95092 21.7008 1.16534 21.8945 1.55273C22.6719 3.38958 22.6983 5.57987 22.2202 7.49248L22.2128 7.52213C22.0847 8.03536 21.9191 8.69868 21.3876 8.92182C21.7827 9.89315 22 10.9466 22 12.0526C22 14.825 20.8618 17.6774 19.8412 20.2348L19.8412 20.2348L19.7379 20.4936C19.1182 22.0486 17.7316 23.1196 16.125 23.418L13.8549 23.8397C13.1549 23.9697 12.4562 23.7172 12 23.2082C11.5438 23.7172 10.8452 23.9697 10.1452 23.8397L7.87506 23.418C6.26852 23.1196 4.88189 22.0486 4.26214 20.4936L4.15891 20.2348C3.13833 17.6774 2.00004 14.825 2.00004 12.0526C2.00004 10.9466 2.21737 9.89315 2.6125 8.92182C2.08046 8.69845 1.91916 8.05124 1.7909 7.53658L1.7799 7.49248C1.32311 5.66527 1.23531 3.2968 2.10561 1.55273C2.29827 1.16741 2.72906 0.945855 3.15452 1.01195ZM6.58478 4.44052C5.45516 5.10067 4.47474 5.9652 3.71373 6.98132C3.41572 5.76461 3.41236 4.41153 3.67496 3.18754C4.68842 3.48029 5.68018 3.89536 6.58478 4.44052ZM20.2863 6.98133C19.5303 5.97184 18.5577 5.11195 17.4374 4.45347C18.3364 3.9005 19.3043 3.45749 20.3223 3.17455C20.5884 4.40199 20.5853 5.76068 20.2863 6.98133ZM8.85364 5.56694C9.81678 5.20285 10.8797 4.99995 12 4.99995C13.1204 4.99995 14.1833 5.20285 15.1464 5.56694C18.0554 6.66661 20 9.1982 20 12.0526C20 14.4676 18.9891 16.9876 18.0863 19.238L18.0862 19.2382C18.0167 19.4115 17.9478 19.5832 17.8801 19.7531C17.5291 20.6338 16.731 21.2712 15.7597 21.4516L13.4896 21.8733L12.912 20.5896C12.7505 20.2307 12.3935 19.9999 12 19.9999C11.6065 19.9999 11.2496 20.2307 11.0881 20.5896L10.5104 21.8733L8.24033 21.4516C7.26908 21.2712 6.471 20.6338 6.12001 19.7531C6.05237 19.5834 5.98357 19.4119 5.91414 19.2388L5.91395 19.2384L5.91381 19.238C5.01102 16.9876 4.00004 14.4676 4.00004 12.0526C4.00004 9.1982 5.94472 6.66661 8.85364 5.56694ZM10.5 15.9999C10.1212 15.9999 9.77497 16.2139 9.60557 16.5527C9.43618 16.8915 9.47274 17.2969 9.7 17.5999L11.2 19.5999C11.3889 19.8517 11.6852 19.9999 12 19.9999C12.3148 19.9999 12.6111 19.8517 12.8 19.5999L14.3 17.5999C14.5273 17.2969 14.5638 16.8915 14.3944 16.5527C14.225 16.2139 13.8788 15.9999 13.5 15.9999H10.5ZM9.62134 11.1212C9.62134 11.9497 8.94977 12.6212 8.12134 12.6212C7.29291 12.6212 6.62134 11.9497 6.62134 11.1212C6.62134 10.2928 7.29291 9.62125 8.12134 9.62125C8.94977 9.62125 9.62134 10.2928 9.62134 11.1212ZM16 12.4999C16.8284 12.4999 17.5 11.8284 17.5 10.9999C17.5 10.1715 16.8284 9.49994 16 9.49994C15.1716 9.49994 14.5 10.1715 14.5 10.9999C14.5 11.8284 15.1716 12.4999 16 12.4999Z",
							ItemShapeSize = 40,
							MaximumRating = 5,
							FilledColor = Colors.Red,
							Rating = 5,
							Shape = RatingViewShape.Custom,
							ShapeBorderThickness = 1,
						}
						.Row(Row.AvailableShapesCustomDog).Column(Column.Result)
						.SemanticDescription("A RatingView showing the 'Custom' shape and passing in the required custom shape path."),

					new Label()
						.Row(Row.AvailableShapesCustomLogo).Column(Column.Input)
						.Text("Custom")
						.CenterVertical(),

					new RatingView
						{
							CustomShape = "M23.07 8h2.89l-6.015 5.957a5.621 5.621 0 01-7.89 0L6.035 8H8.93l4.57 4.523a3.556 3.556 0 004.996 0L23.07 8zM8.895 24.563H6l6.055-5.993a5.621 5.621 0 017.89 0L26 24.562h-2.895L18.5 20a3.556 3.556 0 00-4.996 0l-4.61 4.563z",
							EmptyColor = Colors.Red,
							FilledColor = Colors.White,
							ItemShapeSize = 40,
							MaximumRating = 5,
							Rating = 5,
							Shape = RatingViewShape.Custom,
							ShapeBorderColor = Colors.Grey,
							ShapeBorderThickness = 1,
						}
						.Row(Row.AvailableShapesCustomLogo).Column(Column.Result)
						.SemanticDescription("A RatingView showing the 'Custom' shape and passing in the required custom shape path."),

					new SectionHeader("Maximum ratings")
						.Row(Row.MaximumRatingsHeader).ColumnSpan(All<Column>()),

					new HorizontalStackLayout
					{
						Spacing = 8,
						Children =
						{
							new Stepper
								{
									Increment = 1,
									Minimum = 1,
									Maximum = 25,
									Value = 0
								}
								.Assign(out Stepper stepperMaximumRating)
								.SemanticHint("Change the maximum number of ratings."),

							new Label()
								.Bind(Label.TextProperty,
									getter: static stepper => stepper.Value,
									mode: BindingMode.OneWay,
									convert: static stepperValue => $": {stepperValue}",
									source: stepperMaximumRating),
						}
					}.Row(Row.MaximumRatingsStepper).ColumnSpan(All<Column>()),

					new RatingView()
						.Row(Row.MaximumRatingsRatingView).ColumnSpan(All<Column>())
						.Invoke(static ratingView => ratingView.RatingChanged += HandleRatingChanged)
						.Bind(RatingView.MaximumRatingProperty,
							getter: static stepper => (int)stepper.Value,
							mode: BindingMode.OneWay,
							source: stepperMaximumRating)
						.SemanticDescription("A RatingView showing changes to the 'MaximumRating' property and with an event handler when the 'RatingChanged' event is triggered."),

					new SectionHeader("Colors")
						.Row(Row.ColorsHeader).ColumnSpan(All<Column>()),

					new HorizontalStackLayout
					{
						Spacing = 8,
						Children =
						{
							new Label()
								.Text("Empty Color: ")
								.CenterVertical(),

							new Picker()
								.Bind(Picker.ItemsSourceProperty,
									getter: static (RatingViewCsharpViewModel vm) => vm.ColorsForPickers,
									mode: BindingMode.OneTime)
								.Bind(Picker.SelectedIndexProperty,
									getter: static (RatingViewCsharpViewModel vm) => vm.ColorPickerEmptyBackgroundSelectedIndex,
									setter: static (RatingViewCsharpViewModel vm, int index) => vm.ColorPickerEmptyBackgroundSelectedIndex = index,
									mode: BindingMode.TwoWay)
								.SemanticHint("Pick to change the empty rating background color."),
						}
					}.Row(Row.ColorsEmptyRatingViewPicker).ColumnSpan(All<Column>()),

					new HorizontalStackLayout
					{
						Spacing = 8,
						Children =
						{
							new Label()
								.Text("Filled Color: ")
								.CenterVertical(),

							new Picker()
								.Bind(Picker.ItemsSourceProperty,
									getter: static (RatingViewCsharpViewModel vm) => vm.ColorsForPickers,
									mode: BindingMode.OneTime)
								.Bind(Picker.SelectedIndexProperty,
									getter: static (RatingViewCsharpViewModel vm) => vm.ColorPickerFilledBackgroundSelectedIndex,
									setter: static (RatingViewCsharpViewModel vm, int value) => vm.ColorPickerFilledBackgroundSelectedIndex = value,
									mode: BindingMode.TwoWay)
								.SemanticHint("Pick to change the filled rating background color."),
						}
					}.Row(Row.ColorsFilledRatingViewPicker).ColumnSpan(All<Column>()),

					new HorizontalStackLayout
					{
						Spacing = 8,
						Children =
						{
							new Label()
								.Text("Border Color: ")
								.CenterVertical(),

							new Picker()
								.Bind(Picker.ItemsSourceProperty,
									getter: static (RatingViewCsharpViewModel vm) => vm.ColorsForPickers,
									mode: BindingMode.OneTime)
								.Bind(Picker.SelectedIndexProperty,
									getter: static (RatingViewCsharpViewModel vm) => vm.ColorPickerRatingShapeBorderColorSelectedIndex,
									setter: static (RatingViewCsharpViewModel vm, int index) => vm.ColorPickerRatingShapeBorderColorSelectedIndex = index,
									mode: BindingMode.TwoWay)
								.SemanticHint("Pick to change the rating shape border color."),
						}
					}.Row(Row.ColorsBorderRatingViewPicker).ColumnSpan(All<Column>()),

					new Label()
						.Row(Row.ColorsShapeFillTitle).ColumnSpan(All<Column>())
						.Text("Shape Fill"),

					new RatingView
						{
							ItemShapeSize = 40,
							MaximumRating = 5,
							Rating = 2.7,
							ShapeBorderThickness = 1
						}.Row(Row.ColorsShapeFillRatingView).ColumnSpan(All<Column>())
						.Bind(RatingView.EmptyColorProperty,
							static (RatingViewCsharpViewModel vm) => vm.ColorPickerEmptyBackgroundTarget,
							mode: BindingMode.OneWay)
						.Bind(RatingView.FilledColorProperty,
							static (RatingViewCsharpViewModel vm) => vm.ColorPickerFilledBackgroundTarget,
							mode: BindingMode.OneWay)
						.Bind(RatingView.ShapeBorderColorProperty,
							static (RatingViewCsharpViewModel vm) => vm.ColorPickerRatingShapeBorderColorTarget,
							mode: BindingMode.OneWay)
						.SemanticDescription("A RatingView showing the fill, empty and border color changes, shown using the fill type of 'Shape'."),

					new Label()
						.Row(Row.ColorsItemFillTitle).ColumnSpan(All<Column>())
						.Text("Item Fill"),

					new RatingView
						{
							ItemShapeSize = 40,
							MaximumRating = 5,
							Rating = 2.7,
							RatingFill = RatingFillElement.Item,
							ShapeBorderThickness = 1
						}.Row(Row.ColorsItemFillRatingView).ColumnSpan(All<Column>())
						.Bind(RatingView.EmptyColorProperty,
							getter: static (RatingViewCsharpViewModel vm) => vm.ColorPickerEmptyBackgroundTarget,
							mode: BindingMode.OneWay)
						.Bind(RatingView.FilledColorProperty,
							getter: static (RatingViewCsharpViewModel vm) => vm.ColorPickerFilledBackgroundTarget,
							mode: BindingMode.OneWay)
						.Bind(RatingView.ShapeBorderColorProperty,
							getter: static (RatingViewCsharpViewModel vm) => vm.ColorPickerRatingShapeBorderColorTarget,
							mode: BindingMode.OneWay)
						.SemanticDescription("A RatingView showing the fill, empty and border color changes, shown using the fill type of 'Item'."),

					new SectionHeader("Shape Border Thickness")
						.Row(Row.BorderThicknessHeader).ColumnSpan(All<Column>()),

					new HorizontalStackLayout
					{
						Spacing = 8,
						Children =
						{
							new Stepper
							{
								Increment = 1,
								Minimum = 0,
								Maximum = 10,
								Value = 1
							}.Assign(out Stepper stepperShapeBorderThickness).SemanticHint("Change the rating shape border thickness."),

							new Label().CenterVertical()
								.Bind(Label.TextProperty,
									getter: static stepper => stepper.Value,
									mode: BindingMode.OneWay,
									convert: static stepperValue => $": {stepperValue}",
									source: stepperShapeBorderThickness),
						}
					}.Row(Row.BorderThicknessStepper).ColumnSpan(All<Column>()),

					new RatingView
						{
							MaximumRating = 5,
							Rating = 2.5
						}
						.Row(Row.BorderThicknessRatingView).ColumnSpan(All<Column>())
						.Bind(RatingView.ShapeBorderThicknessProperty,
							getter: static stepper => (int)stepper.Value,
							mode: BindingMode.OneWay,
							convert: static stepperValue => stepperValue,
							source: stepperShapeBorderThickness
						).SemanticDescription("A RatingView showing the shape border thickness changes."),

					new SectionHeader("ReadOnly")
						.Row(Row.ReadOnlyHeader).ColumnSpan(All<Column>()),

					new HorizontalStackLayout
					{
						Spacing = 8,
						Children =
						{
							new CheckBox
								{
									IsChecked = true
								}
								.AppThemeColorBinding(CheckBox.BackgroundColorProperty, Colors.Black, Colors.White)
								.AppThemeColorBinding(CheckBox.ColorProperty, Colors.White, Colors.Black)
								.Assign(out CheckBox checkBox)
								.SemanticHint("Check to make read only."),

							new Label()
								.Text("IsReadOnly")
								.CenterVertical(),
						}
					}.Row(Row.ReadOnlyCheckBox).ColumnSpan(All<Column>()),

					new RatingView
						{
							Rating = 2.5,
							MaximumRating = 5
						}
						.Row(Row.ReadOnlyRatingView).ColumnSpan(All<Column>())
						.Bind(RatingView.IsReadOnlyProperty,
							getter: static checkBox => checkBox.IsChecked,
							mode: BindingMode.OneWay,
							source: checkBox
						)
						.SemanticDescription("A RatingView showing the IsReadOnly changes."),

					new SectionHeader("Shape Padding")
						.Row(Row.PaddingHeader).ColumnSpan(All<Column>()),

					new HorizontalStackLayout
					{
						Spacing = 8,
						Children =
						{
							new Stepper
								{
									Increment = 1,
									Minimum = 0,
									Maximum = 10,
									Value = 0
								}
								.Bind(Stepper.ValueProperty,
									getter: static (RatingViewCsharpViewModel vm) => vm.RatingViewShapePaddingLeft,
									setter: static (RatingViewCsharpViewModel vm, double value) => vm.RatingViewShapePaddingLeft = value)
								.Assign(out Stepper stepperPaddingLeft).SemanticHint("Change the rating view padding left."),

							new Label
								{
									VerticalOptions = LayoutOptions.Center
								}.Bind(Label.TextProperty,
									static stepper => stepper.Value,
									mode: BindingMode.OneWay,
									convert: static stepperValue => $": Left: {stepperValue}",
									source: stepperPaddingLeft)
								.SemanticDescription("Shape left padding applied to the RatingView sample."),
						}
					}.Row(Row.PaddingLeftStepper).ColumnSpan(All<Column>()),

					new HorizontalStackLayout
					{
						Spacing = 8,
						Children =
						{
							new Stepper
								{
									Increment = 1,
									Minimum = 0,
									Maximum = 10,
									Value = 0
								}
								.Bind(Stepper.ValueProperty,
									getter: static (RatingViewCsharpViewModel vm) => vm.RatingViewShapePaddingTop,
									setter: static (RatingViewCsharpViewModel vm, double value) => vm.RatingViewShapePaddingTop = value)
								.Assign(out Stepper stepperPaddingTop)
								.SemanticHint("Change the rating view padding top."),

							new Label()
								.CenterVertical()
								.Bind(Label.TextProperty,
									getter: static stepper => stepper.Value,
									mode: BindingMode.OneWay,
									convert: static stepperValue => $": Top: {stepperValue}",
									source: stepperPaddingTop)
								.SemanticDescription("Shape top padding applied to the RatingView sample."),
						}
					}.Row(Row.PaddingTopStepper).ColumnSpan(All<Column>()),

					new HorizontalStackLayout
					{
						Spacing = 8,
						Children =
						{
							new Stepper
								{
									Increment = 1,
									Minimum = 0,
									Maximum = 10,
									Value = 0
								}
								.Bind(Stepper.ValueProperty,
									getter: static (RatingViewCsharpViewModel vm) => vm.RatingViewShapePaddingRight,
									setter: static (RatingViewCsharpViewModel vm, double value) => vm.RatingViewShapePaddingRight = value)
								.Assign(out Stepper stepperPaddingRight)
								.SemanticHint("Change the rating view padding right."),

							new Label()
								.CenterVertical()
								.Bind(Label.TextProperty,
									getter: static stepper => stepper.Value,
									mode: BindingMode.OneWay,
									convert: static stepperValue => $": Right: {stepperValue}",
									source: stepperPaddingRight)
								.SemanticDescription("Shape right padding applied to the RatingView sample."),
						}
					}.Row(Row.PaddingRightStepper).ColumnSpan(All<Column>()),

					new HorizontalStackLayout
					{
						Spacing = 8,
						Children =
						{
							new Stepper
								{
									Increment = 1,
									Minimum = 0,
									Maximum = 10,
									Value = 0,
								}
								.Bind(Stepper.ValueProperty,
									getter: static (RatingViewCsharpViewModel vm) => vm.RatingViewShapePaddingBottom,
									setter: static (RatingViewCsharpViewModel vm, double value) => vm.RatingViewShapePaddingBottom = value)
								.Assign(out Stepper stepperPaddingBottom)
								.SemanticHint("Change the rating view padding bottom."),

							new Label()
								.CenterVertical()
								.Bind(Label.TextProperty,
									getter: static stepper => stepper.Value,
									mode: BindingMode.OneWay,
									convert: static stepperValue => $": Bottom: {stepperValue}",
									source: stepperPaddingBottom)
								.SemanticDescription("Shape bottom padding applied to the RatingView sample."),
						}
					}.Row(Row.PaddingBottomStepper).ColumnSpan(All<Column>()),

					new RatingView
						{
							MaximumRating = 5,
							Rating = 4.5,
						}
						.Row(Row.PaddingRatingView).ColumnSpan(All<Column>())
						.BackgroundColor(Colors.Purple)
						.Start().CenterVertical()
						.Bind(RatingView.ItemPaddingProperty,
							getter: static (RatingViewCsharpViewModel vm) => vm.RatingViewShapePaddingValue)
						.SemanticDescription("A RatingView sample showing the padding changes."),

					new SectionHeader("Rating")
						.Row(Row.RatingHeader).ColumnSpan(All<Column>()),

					new HorizontalStackLayout
					{
						Spacing = 8,
						Children =
						{
							new Slider
							{
								Maximum = 7,
								Minimum = 0,
								Value = 0
							}.Assign(out Slider ratingViewSlider).SemanticHint("Slide to change the rating."),

							new Label()
								.Center()
								.Bind(Label.TextProperty,
									getter: static slider => slider.Value,
									mode: BindingMode.OneWay,
									convert: static sliderValue => $": {sliderValue:F2}",
									source: ratingViewSlider)
								.SemanticDescription("RatingView rating value."),
						}
					}.Row(Row.RatingSlider).ColumnSpan(All<Column>()),

					new Label()
						.Row(Row.RatingShapeFillTitle).ColumnSpan(All<Column>())
						.Text("Shape Fill"),

					new RatingView
						{
							BackgroundColor = Colors.Purple,
							EmptyColor = Colors.Blue,
							FilledColor = Colors.Green,
							HorizontalOptions = LayoutOptions.Start,
							IsReadOnly = false,
							ItemShapeSize = 30,
							MaximumRating = 7,
							RatingFill = RatingFillElement.Shape,
							ShapeBorderColor = Colors.Grey,
							ShapeBorderThickness = 1,
							Spacing = 3,
						}
						.Row(Row.RatingShapeFillRatingView).ColumnSpan(All<Column>())
						.Bind(RatingView.RatingProperty,
							getter: static slider => slider.Value,
							mode: BindingMode.OneWay,
							convert: static sliderValue => sliderValue,
							source: ratingViewSlider)
						.SemanticDescription("A RatingView sample showing the rating changes and the fill type of 'Shape'."),

					new Label()
						.Row(Row.RatingItemFillTitle).ColumnSpan(All<Column>())
						.Text("Item Fill"),

					new RatingView
						{
							BackgroundColor = Colors.Purple,
							EmptyColor = Colors.Blue,
							FilledColor = Colors.Green,
							HorizontalOptions = LayoutOptions.Start,
							IsReadOnly = false,
							ItemShapeSize = 30,
							MaximumRating = 7,
							RatingFill = RatingFillElement.Item,
							ShapeBorderColor = Colors.Grey,
							ShapeBorderThickness = 1,
							Spacing = 3,
						}
						.Row(Row.RatingItemFillRatingView).ColumnSpan(All<Column>())
						.Bind(RatingView.RatingProperty,
							getter: static slider => slider.Value,
							mode: BindingMode.OneWay,
							convert: static sliderValue => sliderValue,
							source: ratingViewSlider)
						.SemanticDescription("A RatingView sample showing the rating changes and the fill type of 'Item'."),

					new SectionHeader("Sizing")
						.Row(Row.SizingHeader).ColumnSpan(All<Column>()),

					new RatingView
						{
							ItemShapeSize = 30,
							MaximumRating = 5,
							Rating = 5,
						}
						.Row(Row.SizingRatingViewSmallest).ColumnSpan(All<Column>())
						.SemanticDescription("A RatingView sample showing the size of 30"),

					new RatingView
						{
							ItemShapeSize = 40,
							MaximumRating = 5,
							Rating = 5,
						}
						.Row(Row.SizingRatingViewSmaller).ColumnSpan(All<Column>())
						.SemanticDescription("A RatingView sample showing the size of 40"),

					new RatingView
						{
							ItemShapeSize = 50,
							MaximumRating = 5,
							Rating = 5,
						}
						.Row(Row.SizingRatingViewLarger).ColumnSpan(All<Column>())
						.SemanticDescription("A RatingView sample showing the size of 50"),

					new RatingView
						{
							ItemShapeSize = 60,
							MaximumRating = 5,
							Rating = 5,
						}
						.Row(Row.SizingRatingViewLargest).ColumnSpan(All<Column>())
						.SemanticDescription("A RatingView sample showing the size of 60"),


					new SectionHeader("Spacing")
						.Row(Row.SpacingHeader).ColumnSpan(All<Column>()),

					new HorizontalStackLayout
					{
						Spacing = 8,
						Children =
						{
							new Stepper
							{
								Increment = 1,
								Minimum = 0,
								Maximum = 10,
								Value = 0
							}.Assign(out Stepper stepperSpacing).SemanticHint("Change the spacing between rating items."),

							new Label()
								.CenterVertical()
								.Bind(Label.TextProperty,
									getter: static stepper => stepper.Value,
									mode: BindingMode.OneWay,
									convert: static stepperValue => $": {stepperValue}",
									source: stepperSpacing),
						}
					}.Row(Row.SpacingStepper).ColumnSpan(All<Column>()),

					new RatingView
						{
							MaximumRating = 5,
							Rating = 2.5
						}
						.Bind(RatingView.SpacingProperty,
							getter: static stepper => (int)stepper.Value,
							mode: BindingMode.OneWay,
							convert: static stepperValue => stepperValue,
							source: stepperSpacing
						)
						.Row(Row.SpacingRatingView).ColumnSpan(All<Column>())
						.SemanticDescription("A RatingView sample showing the spacing changes."),
				}
			}
		};
	}

	enum Column { Input, Result }

	enum Row
	{
		DefaultsHeader, DefaultsRatingView, DefaultsRatingViewUsingProperties, DefaultsRatingViewUsingStyles,
		AvailableShapesHeader, AvailableShapesStar, AvailableShapesCircle, AvailableShapesHeart, AvailableShapesLike, AvailableShapesDislike, AvailableShapesCustomDog, AvailableShapesCustomLogo,
		MaximumRatingsHeader, MaximumRatingsStepper, MaximumRatingsRatingView,
		ColorsHeader, ColorsEmptyRatingViewPicker, ColorsFilledRatingViewPicker, ColorsBorderRatingViewPicker, ColorsShapeFillTitle, ColorsShapeFillRatingView, ColorsItemFillTitle, ColorsItemFillRatingView,
		BorderThicknessHeader, BorderThicknessStepper, BorderThicknessRatingView,
		ReadOnlyHeader, ReadOnlyCheckBox, ReadOnlyRatingView,
		PaddingHeader, PaddingLeftStepper, PaddingTopStepper, PaddingRightStepper, PaddingBottomStepper, PaddingRatingView,
		RatingHeader, RatingSlider, RatingShapeFillTitle, RatingShapeFillRatingView, RatingItemFillTitle, RatingItemFillRatingView,
		SizingHeader, SizingRatingViewSmallest, SizingRatingViewSmaller, SizingRatingViewLarger, SizingRatingViewLargest,
		SpacingHeader, SpacingStepper, SpacingRatingView
	}

	static async void HandleRatingChanged(object? sender, RatingChangedEventArgs e)
	{
		ArgumentNullException.ThrowIfNull(sender);
		var ratingView = (RatingView)sender;

		// This is the weak event raised when the rating is changed.  The developer can then perform further actions (such as save to DB).
		await Toast.Make($"New Rating: {ratingView.Rating:F2}").Show(CancellationToken.None);
	}

	sealed class SectionHeader : Grid
	{
		public const int RequestedHeight = separatorRowHeight * 2 + titleHeight;
		const int separatorRowHeight = 8;
		const int titleHeight = 32;

		public SectionHeader(in string titleText)
		{


			RowDefinitions = Rows.Define(
				(SectionHeaderRow.TopSeparator, separatorRowHeight),
				(SectionHeaderRow.Title, titleHeight),
				(SectionHeaderRow.BottomSeparator, separatorRowHeight));

			Children.Add(GetSeparator().Row(SectionHeaderRow.TopSeparator));

			Children.Add(new TitleLabel(titleText).Row(SectionHeaderRow.Title));

			Children.Add(GetSeparator().Row(SectionHeaderRow.BottomSeparator));
		}

		enum SectionHeaderRow { TopSeparator, Title, BottomSeparator }

		static Line GetSeparator() => new Line
		{
			X2 = 300
		}.Center().AppThemeBinding(Line.StrokeProperty, Colors.Black, Colors.White);
	}

	sealed class TitleLabel : Label
	{
		public TitleLabel(in string text)
		{
			LineBreakMode = LineBreakMode.WordWrap;

			this.Center()
				.TextCenter()
				.Text(text)
				.Margin(4, 0);
		}
	}
}