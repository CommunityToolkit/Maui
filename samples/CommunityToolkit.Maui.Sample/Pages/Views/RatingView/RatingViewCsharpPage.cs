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
			Content = new VerticalStackLayout
			{
				Padding = new Thickness(0, 12, 0, 0),
				Spacing = 12,
				Children =
				{
					GetSeparator(),

					new TitleLabel("Defaults"),

					GetSeparator(),

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
					},

					new HorizontalStackLayout
					{
						Spacing = 8,
						Children =
						{
							new Label()
								.Text("By properties")
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
					},

					new HorizontalStackLayout
					{
						Spacing = 8,
						Children =
						{
							new Label()
								.Text("By style")
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
					},

					GetSeparator(),

					new TitleLabel("Available Shapes, and example custom shape"),

					GetSeparator(),

					new Grid
					{
						ColumnDefinitions = Columns.Define(
							(Column.Input, Auto),
							(Column.Result, Star)),

						RowDefinitions = Rows.Define(
							(Row.Star, Auto),
							(Row.Circle, Auto),
							(Row.Heart, Auto),
							(Row.Like, Auto),
							(Row.Dislike, Auto),
							(Row.Custom, Auto),
							(Row.Custom2, Auto)
						),

						Children =
						{
							new Label()
								.Text("Star")
								.CenterVertical()
								.Row(Row.Star).Column(Column.Input),

							new RatingView
								{
									ItemShapeSize = 40,
									MaximumRating = 5,
									EmptyColor = Colors.White,
									FilledColor = Colors.Blue,
									Rating = 2,
									Shape = RatingViewShape.Star,
									ShapeBorderThickness = 1
								}.Row(Row.Star).Column(Column.Result)
								.SemanticDescription("A RatingView showing the 'Star' shape."),

							new Label()
								.Text("Circle")
								.CenterVertical()
								.Row(Row.Circle).Column(Column.Input),

							new RatingView
								{
									ItemShapeSize = 40,
									MaximumRating = 5,
									EmptyColor = Colors.Red,
									FilledColor = Colors.Blue,
									Rating = 2,
									Shape = RatingViewShape.Circle,
									ShapeBorderThickness = 1
								}.Row(Row.Circle).Column(Column.Result)
								.SemanticDescription("A RatingView showing the 'Circle' shape."),

							new Label()
								.Text("Heart")
								.CenterVertical()
								.Row(Row.Heart).Column(Column.Input),

							new RatingView
								{
									ItemShapeSize = 40,
									MaximumRating = 5,
									FilledColor = Colors.White,
									Rating = 5,
									Shape = RatingViewShape.Heart,
									ShapeBorderThickness = 1
								}.Row(Row.Heart).Column(Column.Result)
								.SemanticDescription("A RatingView showing the 'Heart' shape."),

							new Label()
								.Text("Like")
								.CenterVertical()
								.Row(Row.Like).Column(Column.Input),

							new RatingView
								{
									ItemShapeSize = 40,
									MaximumRating = 5,
									Rating = 5,
									FilledColor = Colors.Red,
									Shape = RatingViewShape.Like,
									ShapeBorderThickness = 1
								}.Row(Row.Like).Column(Column.Result)
								.SemanticDescription("A RatingView showing the 'Like' shape."),

							new Label()
								.Text("Dislike")
								.CenterVertical()
								.Row(Row.Dislike).Column(Column.Input),

							new RatingView
								{
									ItemShapeSize = 40,
									MaximumRating = 5,
									Rating = 5,
									FilledColor = Colors.White,
									Shape = RatingViewShape.Dislike,
									ShapeBorderThickness = 1
								}
								.Row(Row.Dislike).Column(Column.Result)
								.SemanticDescription("A RatingView showing the 'Dislike' shape."),

							new Label()
								.Text("Custom")
								.CenterVertical()
								.Row(Row.Custom).Column(Column.Input),

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
								.Row(Row.Custom).Column(Column.Result)
								.SemanticDescription("A RatingView showing the 'Custom' shape and passing in the required custom shape path."),

							new Label()
								.Text("Custom")
								.CenterVertical()
								.Row(Row.Custom2).Column(Column.Input),

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
								.Row(Row.Custom2).Column(Column.Result)
								.SemanticDescription("A RatingView showing the 'Custom' shape and passing in the required custom shape path."),
						}
					},

					GetSeparator(),

					new TitleLabel("Maximum ratings"),

					GetSeparator(),

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
					},

					new RatingView()
						.Invoke(static ratingView => ratingView.RatingChanged += HandleRatingChanged)
						.Bind(RatingView.MaximumRatingProperty,
							getter: static stepper => (int)stepper.Value,
							mode: BindingMode.OneWay,
							source: stepperMaximumRating)
						.SemanticDescription("A RatingView showing changes to the 'MaximumRating' property and with an event handler when the 'RatingChanged' event is triggered."),

					GetSeparator(),

					new TitleLabel("Colors"),

					GetSeparator(),

					new HorizontalStackLayout
					{
						Spacing = 8,
						Children =
						{
							new Label()
								.Text("Empty color: ")
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
					},

					new HorizontalStackLayout
					{
						Spacing = 8,
						Children =
						{
							new Label()
								.Text("Filled color: ")
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
					},
					new HorizontalStackLayout
					{
						Spacing = 8,
						Children =
						{
							new Label()
								.Text("Shape border color: ")
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
					},

					new Label().Text("Shape Fill"),

					new RatingView
						{
							ItemShapeSize = 40,
							MaximumRating = 5,
							Rating = 2.7,
							ShapeBorderThickness = 1
						}.Bind(RatingView.EmptyColorProperty,
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
						.Text("Item Fill"),

					new RatingView
						{
							ItemShapeSize = 40,
							MaximumRating = 5,
							Rating = 2.7,
							RatingFill = RatingFillElement.Item,
							ShapeBorderThickness = 1
						}.Bind(RatingView.EmptyColorProperty,
							getter: static (RatingViewCsharpViewModel vm) => vm.ColorPickerEmptyBackgroundTarget,
							mode: BindingMode.OneWay)
						.Bind(RatingView.FilledColorProperty,
							getter: static (RatingViewCsharpViewModel vm) => vm.ColorPickerFilledBackgroundTarget,
							mode: BindingMode.OneWay)
						.Bind(RatingView.ShapeBorderColorProperty,
							getter: static (RatingViewCsharpViewModel vm) => vm.ColorPickerRatingShapeBorderColorTarget,
							mode: BindingMode.OneWay)
						.SemanticDescription("A RatingView showing the fill, empty and border color changes, shown using the fill type of 'Item'."),

					GetSeparator(),

					new TitleLabel("Shape border thickness"),

					GetSeparator(),

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
							}.Assign(out Stepper stepperShapeBorderThickness).SemanticHint("Change the rating shape border thickness."),
							new Label
							{
								VerticalOptions = LayoutOptions.Center
							}.Bind(
								Label.TextProperty,
								getter: static stepper => stepper.Value,
								mode: BindingMode.OneWay,
								convert: static stepperValue => $": {stepperValue}",
								source: stepperShapeBorderThickness),
						}
					},
					new RatingView
						{
							MaximumRating = 5,
							Rating = 2.5
						}
						.Bind(
							RatingView.ShapeBorderThicknessProperty,
							static stepper => (int)stepper.Value,
							mode: BindingMode.OneWay,
							convert: static stepperValue => stepperValue,
							source: stepperShapeBorderThickness
						).SemanticDescription("A RatingView showing the shape border thickness changes."),

					GetSeparator(),

					new TitleLabel("Read only"),

					GetSeparator(),
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
					},
					new RatingView
						{
							Rating = 2.5,
							MaximumRating = 5
						}
						.Bind(RatingView.IsReadOnlyProperty,
							getter: static checkBox => checkBox.IsChecked,
							mode: BindingMode.OneWay,
							source: checkBox
						)
						.SemanticDescription("A RatingView showing the IsReadOnly changes."),

					GetSeparator(),

					new TitleLabel("Shape padding"),

					GetSeparator(),

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
					},
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
					},
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
					},
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
					},
					new RatingView
						{
							MaximumRating = 5,
							Rating = 4.5,
						}
						.BackgroundColor(Colors.Purple)
						.Start().CenterVertical()
						.Bind(RatingView.ItemPaddingProperty,
							getter: static (RatingViewCsharpViewModel vm) => vm.RatingViewShapePaddingValue)
						.SemanticDescription("A RatingView sample showing the padding changes."),

					GetSeparator(),

					new TitleLabel("Rating"),

					GetSeparator(),

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
					},

					new Label()
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
						.Bind(RatingView.RatingProperty,
							getter: static slider => slider.Value,
							mode: BindingMode.OneWay,
							convert: static sliderValue => sliderValue,
							source: ratingViewSlider)
						.SemanticDescription("A RatingView sample showing the rating changes and the fill type of 'Shape'."),

					new Label().Text("Item Fill"),

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
						.Bind(RatingView.RatingProperty,
							getter: static slider => slider.Value,
							mode: BindingMode.OneWay,
							convert: static sliderValue => sliderValue,
							source: ratingViewSlider)
						.SemanticDescription("A RatingView sample showing the rating changes and the fill type of 'Item'."),

					GetSeparator(),

					new TitleLabel("Sizing"),

					GetSeparator(),

					new RatingView
						{
							ItemShapeSize = 30,
							MaximumRating = 5,
							Rating = 5,
						}
						.SemanticDescription("A RatingView sample showing the size of 30"),

					new RatingView
						{
							ItemShapeSize = 40,
							MaximumRating = 5,
							Rating = 5,
						}
						.SemanticDescription("A RatingView sample showing the size of 40"),

					new RatingView
						{
							ItemShapeSize = 50,
							MaximumRating = 5,
							Rating = 5,
						}
						.SemanticDescription("A RatingView sample showing the size of 50"),

					new RatingView
						{
							ItemShapeSize = 60,
							MaximumRating = 5,
							Rating = 5,
						}
						.SemanticDescription("A RatingView sample showing the size of 60"),

					GetSeparator(),

					new TitleLabel("Spacing"),

					GetSeparator(),

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
					},
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
						.SemanticDescription("A RatingView sample showing the spacing changes."),
				}
			}
		};
	}

	enum Column { Input, Result }

	enum Row { Star, Circle, Heart, Like, Dislike, Custom, Custom2 }

	static Line GetSeparator()
	{
		return new Line
		{
			X2 = 300
		}.Center().AppThemeBinding(Line.StrokeProperty, Colors.Black, Colors.White);
	}

	static async void HandleRatingChanged(object? sender, RatingChangedEventArgs e)
	{
		ArgumentNullException.ThrowIfNull(sender);
		var ratingView = (RatingView)sender;

		// This is the weak event raised when the rating is changed.  The developer can then perform further actions (such as save to DB).
		await Toast.Make($"New Rating: {ratingView.Rating:F2}").Show(CancellationToken.None);
	}

	sealed class TitleLabel : Label
	{
		public TitleLabel(in string text)
		{
			LineBreakMode = LineBreakMode.WordWrap;

			this.TextStart()
				.TextCenterHorizontal()
				.Text(text)
				.Margin(4, 0);
		}
	}
}