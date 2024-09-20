// Ignore Spelling: csharp, color, colors
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Markup;
using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls.Shapes;
using static CommunityToolkit.Maui.Markup.GridRowsColumns;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public class RatingViewCsharpPage : BasePage<RatingViewCsharpViewModel>
{
	readonly RatingViewCsharpViewModel vm;

	public RatingViewCsharpPage(RatingViewCsharpViewModel viewModel) : base(viewModel)
	{
		Title = "RatingView C# Syntax";
		vm = viewModel;

		Style Description = new(typeof(Label))
		{
			Setters =
				{
					new Setter { Property = Label.VerticalTextAlignmentProperty, Value = TextAlignment.Start },
					new Setter { Property = Label.HorizontalTextAlignmentProperty, Value = TextAlignment.Center },
					new Setter { Property = Label.LineBreakModeProperty, Value = LineBreakMode.WordWrap },
					new Setter { Property = Label.MarginProperty, Value = new Thickness(4, 0) }
				}
		};

		Style ByStyle = new(typeof(RatingView))
		{
			Setters =
				{
					new Setter { Property = RatingView.EmptyBackgroundColorProperty, Value = Colors.Green },
					new Setter { Property = RatingView.FilledBackgroundColorProperty, Value = Colors.Blue },
					new Setter { Property = RatingView.BackgroundColorProperty, Value = Colors.Red }
				},
		};

		RatingView RatingViewMaximumRatingProperty = new();
		RatingViewMaximumRatingProperty.RatingChanged += StepperMaximumRating_RatingChanged;

		Picker EmptyBackgroundPicker = new();
		EmptyBackgroundPicker.SetBinding(Picker.ItemsSourceProperty, nameof(vm.ColorsForPickers));
		EmptyBackgroundPicker.SetBinding(Picker.SelectedIndexProperty, nameof(vm.ColorPickerEmptyBackgroundSelectedIndex), BindingMode.TwoWay);
		EmptyBackgroundPicker.SelectedIndexChanged += (sender, e) => vm.ColorPickerEmptyBackgroundCommand.Execute(null);

		Picker FilledBackgroundPicker = new();
		FilledBackgroundPicker.SetBinding(Picker.ItemsSourceProperty, nameof(vm.ColorsForPickers));
		FilledBackgroundPicker.SetBinding(Picker.SelectedIndexProperty, nameof(vm.ColorPickerFilledBackgroundSelectedIndex), BindingMode.TwoWay);
		FilledBackgroundPicker.SelectedIndexChanged += (sender, e) => vm.ColorPickerFilledBackgroundCommand.Execute(null);

		Picker ShapeBorderColorPicker = new();
		ShapeBorderColorPicker.SetBinding(Picker.ItemsSourceProperty, nameof(vm.ColorsForPickers));
		ShapeBorderColorPicker.SetBinding(Picker.SelectedIndexProperty, nameof(vm.ColorPickerRatingShapeBorderColorSelectedIndex), BindingMode.TwoWay);
		ShapeBorderColorPicker.SelectedIndexChanged += (sender, e) => vm.ColorPickerRatingShapeBorderColorCommand.Execute(null);

		Stepper PaddingLeftStepper = new()
		{
			Increment = 1,
			Minimum = 0,
			Maximum = 10,
			Value = 0
		};
		PaddingLeftStepper.ValueChanged += RatingViewShapePaddingLeft_ValueChanged;
		EventToCommandBehavior PaddingLeftBehavior = new()
		{
			EventName = nameof(PaddingLeftStepper.ValueChanged),
			Command = vm.RatingViewShapePaddingLeftCommandCommand
		};
		PaddingLeftStepper.Behaviors.Add(PaddingLeftBehavior);

		Stepper PaddingTopStepper = new()
		{
			Increment = 1,
			Minimum = 0,
			Maximum = 10,
			Value = 0
		};
		PaddingTopStepper.ValueChanged += RatingViewShapePaddingTop_ValueChanged;
		EventToCommandBehavior PaddingTopBehavior = new()
		{
			EventName = nameof(PaddingTopStepper.ValueChanged),
			Command = vm.RatingViewShapePaddingTopCommandCommand
		};
		PaddingTopStepper.Behaviors.Add(PaddingTopBehavior);

		Stepper PaddingRightStepper = new()
		{
			Increment = 1,
			Minimum = 0,
			Maximum = 10,
			Value = 0
		};
		PaddingRightStepper.ValueChanged += RatingViewShapePaddingRight_ValueChanged;
		EventToCommandBehavior PaddingRightBehavior = new()
		{
			EventName = nameof(PaddingRightStepper.ValueChanged),
			Command = vm.RatingViewShapePaddingRightCommandCommand
		};
		PaddingRightStepper.Behaviors.Add(PaddingRightBehavior);

		Stepper PaddingBottomStepper = new()
		{
			Increment = 1,
			Minimum = 0,
			Maximum = 10,
			Value = 0
		};
		PaddingBottomStepper.ValueChanged += RatingViewShapePaddingBottom_ValueChanged;
		EventToCommandBehavior PaddingBottomBehavior = new()
		{
			EventName = nameof(PaddingBottomStepper.ValueChanged),
			Command = vm.RatingViewShapePaddingBottomCommandCommand
		};
		PaddingBottomStepper.Behaviors.Add(PaddingBottomBehavior);

		RatingView PaddedRatingView = new()
		{
			BackgroundColor = Colors.Purple,
			HorizontalOptions = LayoutOptions.Start,
			MaximumRating = 5,
			Rating = 4.5,
			VerticalOptions = LayoutOptions.Center,
		};
		PaddedRatingView.SetBinding(RatingView.ItemPaddingProperty, nameof(vm.RatingViewShapePaddingValue));

		Content = new ScrollView
		{
			Content = new VerticalStackLayout
			{
				Padding = new Thickness(0, 12, 0, 0),
				Spacing = 12,
				Children =
				{
					GetSeparator(),
					new Label { Style = Description, Text = "Defaults" },
					GetSeparator(),
					new HorizontalStackLayout
					{
						Spacing = 8,
						Children =
						{
							new Label { Text = "Default", VerticalOptions = LayoutOptions.Center },
							new RatingView().SemanticDescription("A RatingView showing the defaults.")
						}
					},
					new HorizontalStackLayout
					{
						Spacing = 8,
						Children =
						{
							new Label { Text = "By properties", VerticalOptions = LayoutOptions.Center },
							new RatingView
							{
								BackgroundColor = Colors.Red,
								EmptyBackgroundColor = Colors.Green,
								FilledBackgroundColor = Colors.Blue,
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
							new Label { Text = "By style", VerticalOptions = LayoutOptions.Center },
							new RatingView
							{
								MaximumRating = 5,
								Rating = 2.5,
								Style = ByStyle
							}.SemanticDescription("A RatingView customised by setting a style.")
						}
					},

					GetSeparator(),
					new Label { Style = Description, Text = "Available Shapes, and example custom shape" },
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
							(Row.Custom, Auto)
							),

						Children =
						{
							new Label { Text = "Star", VerticalOptions = LayoutOptions.Center }.Row(Row.Star).Column(Column.Input),
							new RatingView {
								ItemShapeSize = 40,
								MaximumRating = 5,
								Rating = 5,
								Shape = RatingViewShape.Star,
								ShapeBorderThickness = 1
							}.Row(Row.Star).Column(Column.Result).SemanticDescription("A RatingView showing the 'Star' shape."),
							new Label { Text = "Circle", VerticalOptions = LayoutOptions.Center }.Row(Row.Circle).Column(Column.Input),
							new RatingView
							{
								ItemShapeSize = 40,
								MaximumRating = 5,
								Rating = 5,
								Shape = RatingViewShape.Circle,
								ShapeBorderThickness = 1
							}.Row(Row.Circle).Column(Column.Result).SemanticDescription("A RatingView showing the 'Circle' shape."),
							new Label{ Text = "Heart",  VerticalOptions = LayoutOptions.Center }.Row(Row.Heart).Column(Column.Input),
							new RatingView
							{
								ItemShapeSize = 40,
								MaximumRating = 5,
								Rating = 5,
								Shape = RatingViewShape.Heart,
								ShapeBorderThickness = 1
							}.Row(Row.Heart).Column(Column.Result).SemanticDescription("A RatingView showing the 'Heart' shape."),
							new Label { Text = "Like",  VerticalOptions = LayoutOptions.Center }.Row(Row.Like).Column(Column.Input),
							new RatingView
							{
								ItemShapeSize = 40,
								MaximumRating = 5,
								Rating = 5,
								Shape = RatingViewShape.Like,
								ShapeBorderThickness = 1
							}.Row(Row.Like).Column(Column.Result).SemanticDescription("A RatingView showing the 'Like' shape."),
							new Label { Text = "Dislike",  VerticalOptions = LayoutOptions.Center }.Row(Row.Dislike).Column(Column.Input),
							new RatingView
							{
								ItemShapeSize = 40,
								MaximumRating = 5,
								Rating = 5,
								Shape = RatingViewShape.Dislike,
								ShapeBorderThickness = 1
							}.Row(Row.Dislike).Column(Column.Result).SemanticDescription("A RatingView showing the 'Dislike' shape."),
							new Label { Text = "Custom", VerticalOptions = LayoutOptions.Center }.Row(Row.Custom).Column(Column.Input),
							new RatingView
							{
								CustomShape = "M3.15452 1.01195C5.11987 1.32041 7.17569 2.2474 8.72607 3.49603C9.75381 3.17407 10.8558 2.99995 12 2.99995C13.1519 2.99995 14.261 3.17641 15.2946 3.5025C16.882 2.27488 18.8427 1.31337 20.8354 1.01339C21.2596 0.95092 21.7008 1.16534 21.8945 1.55273C22.6719 3.38958 22.6983 5.57987 22.2202 7.49248L22.2128 7.52213C22.0847 8.03536 21.9191 8.69868 21.3876 8.92182C21.7827 9.89315 22 10.9466 22 12.0526C22 14.825 20.8618 17.6774 19.8412 20.2348L19.8412 20.2348L19.7379 20.4936C19.1182 22.0486 17.7316 23.1196 16.125 23.418L13.8549 23.8397C13.1549 23.9697 12.4562 23.7172 12 23.2082C11.5438 23.7172 10.8452 23.9697 10.1452 23.8397L7.87506 23.418C6.26852 23.1196 4.88189 22.0486 4.26214 20.4936L4.15891 20.2348C3.13833 17.6774 2.00004 14.825 2.00004 12.0526C2.00004 10.9466 2.21737 9.89315 2.6125 8.92182C2.08046 8.69845 1.91916 8.05124 1.7909 7.53658L1.7799 7.49248C1.32311 5.66527 1.23531 3.2968 2.10561 1.55273C2.29827 1.16741 2.72906 0.945855 3.15452 1.01195ZM6.58478 4.44052C5.45516 5.10067 4.47474 5.9652 3.71373 6.98132C3.41572 5.76461 3.41236 4.41153 3.67496 3.18754C4.68842 3.48029 5.68018 3.89536 6.58478 4.44052ZM20.2863 6.98133C19.5303 5.97184 18.5577 5.11195 17.4374 4.45347C18.3364 3.9005 19.3043 3.45749 20.3223 3.17455C20.5884 4.40199 20.5853 5.76068 20.2863 6.98133ZM8.85364 5.56694C9.81678 5.20285 10.8797 4.99995 12 4.99995C13.1204 4.99995 14.1833 5.20285 15.1464 5.56694C18.0554 6.66661 20 9.1982 20 12.0526C20 14.4676 18.9891 16.9876 18.0863 19.238L18.0862 19.2382C18.0167 19.4115 17.9478 19.5832 17.8801 19.7531C17.5291 20.6338 16.731 21.2712 15.7597 21.4516L13.4896 21.8733L12.912 20.5896C12.7505 20.2307 12.3935 19.9999 12 19.9999C11.6065 19.9999 11.2496 20.2307 11.0881 20.5896L10.5104 21.8733L8.24033 21.4516C7.26908 21.2712 6.471 20.6338 6.12001 19.7531C6.05237 19.5834 5.98357 19.4119 5.91414 19.2388L5.91395 19.2384L5.91381 19.238C5.01102 16.9876 4.00004 14.4676 4.00004 12.0526C4.00004 9.1982 5.94472 6.66661 8.85364 5.56694ZM10.5 15.9999C10.1212 15.9999 9.77497 16.2139 9.60557 16.5527C9.43618 16.8915 9.47274 17.2969 9.7 17.5999L11.2 19.5999C11.3889 19.8517 11.6852 19.9999 12 19.9999C12.3148 19.9999 12.6111 19.8517 12.8 19.5999L14.3 17.5999C14.5273 17.2969 14.5638 16.8915 14.3944 16.5527C14.225 16.2139 13.8788 15.9999 13.5 15.9999H10.5ZM9.62134 11.1212C9.62134 11.9497 8.94977 12.6212 8.12134 12.6212C7.29291 12.6212 6.62134 11.9497 6.62134 11.1212C6.62134 10.2928 7.29291 9.62125 8.12134 9.62125C8.94977 9.62125 9.62134 10.2928 9.62134 11.1212ZM16 12.4999C16.8284 12.4999 17.5 11.8284 17.5 10.9999C17.5 10.1715 16.8284 9.49994 16 9.49994C15.1716 9.49994 14.5 10.1715 14.5 10.9999C14.5 11.8284 15.1716 12.4999 16 12.4999Z",
								ItemShapeSize = 40,
								MaximumRating = 5,
								Rating = 5,
								Shape = RatingViewShape.Custom,
								ShapeBorderThickness = 1,
							}.Row(Row.Custom).Column(Column.Result).SemanticDescription("A RatingView showing the 'Custom' shape and passing in the required custom shape path."),
							new Label { Text = "Custom", VerticalOptions = LayoutOptions.Center }.Row(Row.Custom2).Column(Column.Input),
							new RatingView
							{
								CustomShape = "M23.07 8h2.89l-6.015 5.957a5.621 5.621 0 01-7.89 0L6.035 8H8.93l4.57 4.523a3.556 3.556 0 004.996 0L23.07 8zM8.895 24.563H6l6.055-5.993a5.621 5.621 0 017.89 0L26 24.562h-2.895L18.5 20a3.556 3.556 0 00-4.996 0l-4.61 4.563z",
								EmptyBackgroundColor = Colors.Red,
								FilledBackgroundColor = Colors.White,
								ItemShapeSize = 40,
								MaximumRating = 5,
								Rating = 5,
								Shape = RatingViewShape.Custom,
								ShapeBorderColor = Colors.White,
								ShapeBorderThickness = 1,
							}.Row(Row.Custom).Column(Column.Result).SemanticDescription("A RatingView showing the 'Custom' shape and passing in the required custom shape path."),
						}
					},

					GetSeparator(),
					new Label { Style = Description, Text = "Maximum ratings" },
					GetSeparator(),
					new HorizontalStackLayout
					{
						Spacing = 8,
						Children =
						{
							new Stepper { Increment = 1, Minimum = 1, Maximum = 25, Value = 0 }.Assign(out Stepper stepperMaximumRating).SemanticHint("Change the maximum number of ratings."),
							new Label().Bind(
								Label.TextProperty,
								static (Stepper stepper) => stepper.Value,
								mode: BindingMode.OneWay,
								convert: (double stepperValue) => $"Change the maximum number of ratings: {stepperValue}",
								source: stepperMaximumRating),
						}
					},
					RatingViewMaximumRatingProperty
						.Bind(RatingView.MaximumRatingProperty,
						static (Stepper stepper) => (int)stepper.Value,
						mode: BindingMode.OneWay,
						convert: (int steperValue) => steperValue,
						source: stepperMaximumRating)
						.SemanticDescription("A RatingView showing changes to the 'MaximumRating' property and with an event handler when the 'RatingChanged' event is triggered."),

					GetSeparator(),
					new Label { Style = Description, Text = "Colors" },
					GetSeparator(),
					new HorizontalStackLayout
					{
						Spacing = 8,
						Children =
						{
							new Label { Text = "Empty background color: ", VerticalOptions = LayoutOptions.Center },
							EmptyBackgroundPicker.SemanticHint("Pick to change the empty rating background color."),
						}
					},
					new HorizontalStackLayout
					{
						Spacing = 8,
						Children =
						{
							new Label { Text = "Filled background color: ", VerticalOptions = LayoutOptions.Center },
							FilledBackgroundPicker.SemanticHint("Pick to change the filled rating background color."),
						}
					},
					new HorizontalStackLayout
					{
						Spacing = 8,
						Children =
						{
							new Label { Text = "Shape border color: ", VerticalOptions = LayoutOptions.Center },
							ShapeBorderColorPicker.SemanticHint("Pick to change the rating shape border color."),
						}
					},

					new Label { Text = "Shape Fill" },
					new RatingView
					{
						EmptyBackgroundColor = vm.ColorPickerEmptyBackgroundTarget,
						FilledBackgroundColor = vm.ColorPickerFilledBackgroundTarget,
						ItemShapeSize = 40,
						MaximumRating = 5,
						Rating = 2.7,
						RatingFill = RatingFillElement.Shape,
						ShapeBorderColor = vm.ColorPickerRatingShapeBorderColorTarget,
						ShapeBorderThickness = 1
					}.Bind<RatingView, RatingViewCsharpViewModel, Color, Color>(
						RatingView.EmptyBackgroundColorProperty,
						static (RatingViewCsharpViewModel vm) => vm.ColorPickerEmptyBackgroundTarget,
						mode: BindingMode.OneWay,
						convert: (Color? colour) => colour!
						)
					.Bind<RatingView, RatingViewCsharpViewModel, Color, Color>(
						RatingView.FilledBackgroundColorProperty,
						static (RatingViewCsharpViewModel vm) => vm.ColorPickerFilledBackgroundTarget,
						mode: BindingMode.OneWay,
						convert: (Color? colour) => colour!
						)
					.Bind<RatingView, RatingViewCsharpViewModel, Color, Color>(
						RatingView.ShapeBorderColorProperty,
						static (RatingViewCsharpViewModel vm) => vm.ColorPickerRatingShapeBorderColorTarget,
						mode: BindingMode.OneWay,
						convert: (Color? colour) => colour!
						)
					.SemanticDescription("A RatingView showing the fill, empty and border color changes, shown using the fill type of 'Shape'."),
					new Label { Text = "Item Fill" },
					new RatingView
					{
						EmptyBackgroundColor = vm.ColorPickerEmptyBackgroundTarget,
						FilledBackgroundColor = vm.ColorPickerFilledBackgroundTarget,
						ItemShapeSize = 40,
						MaximumRating = 5,
						Rating = 2.7,
						RatingFill = RatingFillElement.Item,
						ShapeBorderColor = vm.ColorPickerRatingShapeBorderColorTarget,
						ShapeBorderThickness = 1
					}.Bind<RatingView, RatingViewCsharpViewModel, Color, Color>(
						RatingView.EmptyBackgroundColorProperty,
						static (RatingViewCsharpViewModel vm) => vm.ColorPickerEmptyBackgroundTarget,
						mode: BindingMode.OneWay,
						convert: (Color? colour) => colour!
						)
					.Bind<RatingView, RatingViewCsharpViewModel, Color, Color>(
						RatingView.FilledBackgroundColorProperty,
						static (RatingViewCsharpViewModel vm) => vm.ColorPickerFilledBackgroundTarget,
						mode: BindingMode.OneWay,
						convert: (Color? colour) => colour!
						)
					.Bind<RatingView, RatingViewCsharpViewModel, Color, Color>(
						RatingView.ShapeBorderColorProperty,
						static (RatingViewCsharpViewModel vm) => vm.ColorPickerRatingShapeBorderColorTarget,
						mode: BindingMode.OneWay,
						convert: (Color? colour) => colour!
						)
					.SemanticDescription("A RatingView showing the fill, empty and border color changes, shown using the fill type of 'Item'."),

					GetSeparator(),
					new Label { Style = Description, Text = "Shape border thickness" },
					GetSeparator(),
					new HorizontalStackLayout
					{
						Spacing = 8,
						Children =
						{
							new Stepper { Increment = 1, Minimum = 0, Maximum = 10, Value = 0 }.Assign(out Stepper stepperShapeBorderThickness).SemanticHint("Change the rating shape border thickness."),
							new Label { VerticalOptions = LayoutOptions.Center }.Bind(
								Label.TextProperty,
								static (Stepper stepper) => stepper.Value,
								mode: BindingMode.OneWay,
								convert: (double stepperValue) => $": {stepperValue}",
								source: stepperShapeBorderThickness),
						}
					},
					new RatingView { MaximumRating = 5, Rating = 2.5}
						.Bind(
							RatingView.ShapeBorderThicknessProperty,
							static (Stepper stepper) => (int)stepper.Value,
							mode: BindingMode.OneWay,
							convert: (int stepperValue) => stepperValue,
							source: stepperShapeBorderThickness
							)
						.SemanticDescription("A RatingView showing the shape border thickness changes."),

					GetSeparator(),
					new Label { Style = Description, Text = "Enable and disable. Ratings can only be changed when enabled." },
					GetSeparator(),
					new HorizontalStackLayout
					{
						Spacing = 8,
						Children =
						{
							new CheckBox { IsChecked = true }.Assign(out CheckBox checkBox).SemanticHint("Check to enable; Otherwise disable."),
							new Label { Text = ": IsEnabled", VerticalOptions = LayoutOptions.Center },
						}
					},
					new RatingView { Rating = 2.5, MaximumRating = 5 }
						.Bind(
							RatingView.IsEnabledProperty,
							static (CheckBox checkB) => checkB.IsChecked,
							mode: BindingMode.OneWay,
							convert: (bool isChecked) => isChecked,
							source: checkBox
						)
						.SemanticDescription("A RatingView showing the IsEnabled changes."),

					GetSeparator(),
					new Label { Style = Description, Text = "Shape padding" },
					GetSeparator(),
					new HorizontalStackLayout
					{
						Spacing = 8,
						Children =
						{
							PaddingLeftStepper.Assign(out Stepper stepperPaddingLeft).SemanticHint("Change the rating view padding left."),
							new Label { VerticalOptions = LayoutOptions.Center }.Bind(
								Label.TextProperty,
								static (Stepper stepper) => stepper.Value,
								mode: BindingMode.OneWay,
								convert: (double stepperValue) => $": Left: {stepperValue}",
								source: stepperPaddingLeft)
							.SemanticDescription("Shape left padding applied to the RatingView sample."),
						}
					},
					new HorizontalStackLayout
					{
						Spacing = 8,
						Children =
						{
							PaddingTopStepper.Assign(out Stepper stepperPaddingTop).SemanticHint("Change the rating view padding top."),
							new Label { VerticalOptions = LayoutOptions.Center }.Bind(
								Label.TextProperty,
								static (Stepper stepper) => stepper.Value,
								mode: BindingMode.OneWay,
								convert: (double stepperValue) => $": Top: {stepperValue}",
								source: stepperPaddingTop)
							.SemanticDescription("Shape top padding applied to the RatingView sample."),
						}
					},
					new HorizontalStackLayout
					{
						Spacing = 8,
						Children =
						{
							PaddingRightStepper.Assign(out Stepper stepperPaddingRight).SemanticHint("Change the rating view padding right."),
							new Label { VerticalOptions = LayoutOptions.Center }.Bind(
								Label.TextProperty,
								static (Stepper stepper) => stepper.Value,
								mode: BindingMode.OneWay,
								convert: (double stepperValue) => $": Right: {stepperValue}",
								source: stepperPaddingRight)
							.SemanticDescription("Shape right padding applied to the RatingView sample."),
						}
					},
					new HorizontalStackLayout
					{
						Spacing = 8,
						Children =
						{
							PaddingBottomStepper.Assign(out Stepper stepperPaddingBottom).SemanticHint("Change the rating view padding bottom."),
							new Label { VerticalOptions = LayoutOptions.Center }.Bind(
								Label.TextProperty,
								static (Stepper stepper) => stepper.Value,
								mode: BindingMode.OneWay,
								convert: (double stepperValue) => $": Bottom: {stepperValue}",
								source: stepperPaddingBottom)
							.SemanticDescription("Shape bottom padding applied to the RatingView sample."),
						}
					},
					PaddedRatingView.SemanticDescription("A RatingView sample showing the padding changes."),

					GetSeparator(),
					new Label { Style = Description, Text = "Rating" },
					GetSeparator(),
					new Slider { Maximum = 10, Minimum = 0, Value = 0 }.Assign(out Slider ratingRatingView).SemanticHint("Slide to change the rating."),
					new Label { HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }.Bind(
						Label.TextProperty,
						static (Slider slider) => slider.Value,
						mode: BindingMode.OneWay,
						convert: (double sliderValue) => $": {sliderValue:F2}",
						source: ratingRatingView)
					.SemanticDescription("RatingView rating value."),
					new Label { Text = "Shape Fill"},
					new RatingView
						{
							BackgroundColor = Colors.Purple,
							EmptyBackgroundColor = Colors.Blue,
							FilledBackgroundColor = Colors.Green,
							HorizontalOptions = LayoutOptions.Start,
							IsEnabled = false,
							ItemShapeSize = 40,
							MaximumRating = 10,
							RatingFill = RatingFillElement.Shape,
							ShapeBorderColor = Colors.Grey,
							ShapeBorderThickness = 1,
							Spacing = 3,
						}
					.Bind(
						RatingView.RatingProperty,
						static (Slider slider) => slider.Value,
						mode: BindingMode.OneWay,
						convert: (double sliderValue) => sliderValue,
						source: ratingRatingView)
					.SemanticDescription("A RatingView sample showing the rating changes and the fill type of 'Shape'."),
					new Label { Text = "Item Fill"},
					new RatingView
						{
							BackgroundColor = Colors.Purple,
							EmptyBackgroundColor = Colors.Blue,
							FilledBackgroundColor = Colors.Green,
							HorizontalOptions = LayoutOptions.Start,
							IsEnabled = false,
							ItemShapeSize = 40,
							MaximumRating = 10,
							RatingFill = RatingFillElement.Item,
							ShapeBorderColor = Colors.Grey,
							ShapeBorderThickness = 1,
							Spacing = 3,
						}
					.Bind(
						RatingView.RatingProperty,
						static (Slider slider) => slider.Value,
						mode: BindingMode.OneWay,
						convert: (double sliderValue) => sliderValue,
						source: ratingRatingView)
					.SemanticDescription("A RatingView sample showing the rating changes and the fill type of 'Item'."),

					GetSeparator(),
					new Label { Style = Description, Text = "Sizing" },
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
					new Label { Style = Description, Text = "Spacing" },
					GetSeparator(),
					new HorizontalStackLayout
					{
						Spacing = 8,
						Children =
						{
							new Stepper { Increment = 1, Minimum = 0, Maximum = 10, Value = 0 }.Assign(out Stepper stepperSpacing).SemanticHint("Change the spacing between rating items."),
							new Label { VerticalOptions = LayoutOptions.Center }.Bind(
								Label.TextProperty,
								static (Stepper stepper) => stepper.Value,
								mode: BindingMode.OneWay,
								convert: (double stepperValue) => $": {stepperValue}",
								source: stepperSpacing),
						}
					},
					new RatingView { MaximumRating = 5, Rating = 2.5}
						.Bind(
							RatingView.SpacingProperty,
							static (Stepper stepper) => (int)stepper.Value,
							mode: BindingMode.OneWay,
							convert: (int stepperValue) => stepperValue,
							source: stepperSpacing
							)
						.SemanticDescription("A RatingView sample showing the spacing changes."),
				}
			}
		};
	}

	enum Column
	{ Input, Result }

	enum Row
	{
		Star, Circle, Heart, Like, Dislike, Custom, Custom2
	}

	static Line GetSeparator()
	{
		return new Line { X2 = 300 }.Center().AppThemeBinding(Line.StrokeProperty, Colors.Black, Colors.White);
	}

	void RatingViewShapePaddingBottom_ValueChanged(object? sender, ValueChangedEventArgs e) => vm.RatingViewShapePaddingBottom = e.NewValue;

	void RatingViewShapePaddingLeft_ValueChanged(object? sender, ValueChangedEventArgs e) => vm.RatingViewShapePaddingLeft = e.NewValue;

	void RatingViewShapePaddingRight_ValueChanged(object? sender, ValueChangedEventArgs e) => vm.RatingViewShapePaddingRight = e.NewValue;

	void RatingViewShapePaddingTop_ValueChanged(object? sender, ValueChangedEventArgs e) => vm.RatingViewShapePaddingTop = e.NewValue;

	void StepperMaximumRating_RatingChanged(object? sender, RatingChangedEventArgs e)
	{
		// This is the weak event raised when the rating is changed.  The developer can then perform further actions (such as save to DB).
		if (sender is RatingView ratingView)
		{
			_ = ratingView.Rating;
		}
	}
}