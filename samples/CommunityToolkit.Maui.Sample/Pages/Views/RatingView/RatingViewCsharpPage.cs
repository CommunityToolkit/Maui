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

		Style HR = new(typeof(Line))
		{
			Setters =
				{
					new Setter { Property = Line.StrokeProperty, Value = new AppThemeBindingExtension { Light = Colors.Black, Dark = Colors.White } },
					new Setter { Property = Line.X2Property, Value = 300 },
					new Setter { Property = Line.HorizontalOptionsProperty, Value = LayoutOptions.Center }
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

		Content = new ScrollView
		{
			Content = new VerticalStackLayout
			{
				Padding = new Thickness(0, 12, 0, 0),
				Spacing = 12,
				Children =
				{
					new Line { Style = HR },
					new Label { Style = Description, Text = "RatingView defaults." },
					new Line { Style = HR },
					new HorizontalStackLayout
					{
						Spacing = 8,
						Children =
						{
							new Label { Text = "Default" },
							new RatingView()
						}
					},
					new HorizontalStackLayout
					{
						Spacing = 8,
						Children =
						{
							new Label { Text = "By properties" },
							new RatingView
							{
								BackgroundColor = Colors.Red,
								EmptyBackgroundColor = Colors.Green,
								FilledBackgroundColor = Colors.Blue,
								MaximumRating = 5,
								Rating = 2.5
							}
						}
					},
					new HorizontalStackLayout
					{
						Spacing = 8,
						Children =
						{
							new Label { Text = "By style" },
							new RatingView
							{
								MaximumRating = 5,
								Rating = 2.5,
								Style = ByStyle
							}
						}
					},
					new Label { Style = Description, Text = "RatingView available shapes, and example custom shape." },
					new Line { Style = HR },
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
							new Label().Row(Row.Star).Column(Column.Input).Text("Star"),
							new RatingView {
								ItemShapeSize = 40,
								MaximumRating = 5,
								Rating = 5,
								Shape = RatingViewShape.Star,
								ShapeBorderThickness = 1
							}.Row(Row.Star).Column(Column.Result),
							new Label()
								.Row(Row.Circle).Column(Column.Input)
								.Text("Circle"),
							new RatingView
							{
								ItemShapeSize = 40,
								MaximumRating = 5,
								Rating = 5,
								Shape = RatingViewShape.Circle,
								ShapeBorderThickness = 1
							}.Row(Row.Circle).Column(Column.Result),
							new Label()
								.Row(Row.Heart).Column(Column.Input)
								.Text("Heart"),
							new RatingView
							{
								ItemShapeSize = 40,
								MaximumRating = 5,
								Rating = 5,
								Shape = RatingViewShape.Heart,
								ShapeBorderThickness = 1
							}.Row(Row.Heart).Column(Column.Result),
							new Label()
								.Row(Row.Like).Column(Column.Input)
								.Text("Like"),
							new RatingView
							{
								ItemShapeSize = 40,
								MaximumRating = 5,
								Rating = 5,
								Shape = RatingViewShape.Like,
								ShapeBorderThickness = 1
							}.Row(Row.Like).Column(Column.Result),
							new Label()
								.Row(Row.Dislike).Column(Column.Input)
								.Text("Dislike"),
							new RatingView
							{
								ItemShapeSize = 40,
								MaximumRating = 5,
								Rating = 5,
								Shape = RatingViewShape.Dislike,
								ShapeBorderThickness = 1
							}.Row(Row.Dislike).Column(Column.Result),
							new Label()
								.Row(Row.Custom).Column(Column.Input)
								.Text("Custom"),
							new RatingView
							{
								CustomShape = "M3.15452 1.01195C5.11987 1.32041 7.17569 2.2474 8.72607 3.49603C9.75381 3.17407 10.8558 2.99995 12 2.99995C13.1519 2.99995 14.261 3.17641 15.2946 3.5025C16.882 2.27488 18.8427 1.31337 20.8354 1.01339C21.2596 0.95092 21.7008 1.16534 21.8945 1.55273C22.6719 3.38958 22.6983 5.57987 22.2202 7.49248L22.2128 7.52213C22.0847 8.03536 21.9191 8.69868 21.3876 8.92182C21.7827 9.89315 22 10.9466 22 12.0526C22 14.825 20.8618 17.6774 19.8412 20.2348L19.8412 20.2348L19.7379 20.4936C19.1182 22.0486 17.7316 23.1196 16.125 23.418L13.8549 23.8397C13.1549 23.9697 12.4562 23.7172 12 23.2082C11.5438 23.7172 10.8452 23.9697 10.1452 23.8397L7.87506 23.418C6.26852 23.1196 4.88189 22.0486 4.26214 20.4936L4.15891 20.2348C3.13833 17.6774 2.00004 14.825 2.00004 12.0526C2.00004 10.9466 2.21737 9.89315 2.6125 8.92182C2.08046 8.69845 1.91916 8.05124 1.7909 7.53658L1.7799 7.49248C1.32311 5.66527 1.23531 3.2968 2.10561 1.55273C2.29827 1.16741 2.72906 0.945855 3.15452 1.01195ZM6.58478 4.44052C5.45516 5.10067 4.47474 5.9652 3.71373 6.98132C3.41572 5.76461 3.41236 4.41153 3.67496 3.18754C4.68842 3.48029 5.68018 3.89536 6.58478 4.44052ZM20.2863 6.98133C19.5303 5.97184 18.5577 5.11195 17.4374 4.45347C18.3364 3.9005 19.3043 3.45749 20.3223 3.17455C20.5884 4.40199 20.5853 5.76068 20.2863 6.98133ZM8.85364 5.56694C9.81678 5.20285 10.8797 4.99995 12 4.99995C13.1204 4.99995 14.1833 5.20285 15.1464 5.56694C18.0554 6.66661 20 9.1982 20 12.0526C20 14.4676 18.9891 16.9876 18.0863 19.238L18.0862 19.2382C18.0167 19.4115 17.9478 19.5832 17.8801 19.7531C17.5291 20.6338 16.731 21.2712 15.7597 21.4516L13.4896 21.8733L12.912 20.5896C12.7505 20.2307 12.3935 19.9999 12 19.9999C11.6065 19.9999 11.2496 20.2307 11.0881 20.5896L10.5104 21.8733L8.24033 21.4516C7.26908 21.2712 6.471 20.6338 6.12001 19.7531C6.05237 19.5834 5.98357 19.4119 5.91414 19.2388L5.91395 19.2384L5.91381 19.238C5.01102 16.9876 4.00004 14.4676 4.00004 12.0526C4.00004 9.1982 5.94472 6.66661 8.85364 5.56694ZM10.5 15.9999C10.1212 15.9999 9.77497 16.2139 9.60557 16.5527C9.43618 16.8915 9.47274 17.2969 9.7 17.5999L11.2 19.5999C11.3889 19.8517 11.6852 19.9999 12 19.9999C12.3148 19.9999 12.6111 19.8517 12.8 19.5999L14.3 17.5999C14.5273 17.2969 14.5638 16.8915 14.3944 16.5527C14.225 16.2139 13.8788 15.9999 13.5 15.9999H10.5ZM9.62134 11.1212C9.62134 11.9497 8.94977 12.6212 8.12134 12.6212C7.29291 12.6212 6.62134 11.9497 6.62134 11.1212C6.62134 10.2928 7.29291 9.62125 8.12134 9.62125C8.94977 9.62125 9.62134 10.2928 9.62134 11.1212ZM16 12.4999C16.8284 12.4999 17.5 11.8284 17.5 10.9999C17.5 10.1715 16.8284 9.49994 16 9.49994C15.1716 9.49994 14.5 10.1715 14.5 10.9999C14.5 11.8284 15.1716 12.4999 16 12.4999Z",
								ItemShapeSize = 40,
								MaximumRating = 5,
								Rating = 5,
								Shape = RatingViewShape.Custom,
								ShapeBorderThickness = 1,
							}.Row(Row.Custom).Column(Column.Result),
						}
					},
					new Line { Style = HR },
					new Label { Style = Description, Text = "RatingView maximum ratings." },
					new Line { Style = HR },
					new HorizontalStackLayout
					{
						Spacing = 8,
						Children =
						{
							new Stepper { Increment = 1, Minimum = 1, Maximum = 25, Value = 0 }.Assign(out Stepper stepperMaximumRating),
							new Label().Bind(
								Label.TextProperty,
								static (Stepper stepper) => stepper.Value,
								mode: BindingMode.OneWay,
								convert: (double stepperValue) => $"Change maximum number of ratings: {stepperValue}",
								source: stepperMaximumRating),
							RatingViewMaximumRatingProperty.Bind(
								RatingView.MaximumRatingProperty,
								static (Stepper stepper) => (int)stepper.Value,
								mode: BindingMode.OneWay,
								convert: (int steperValue) => steperValue,
								source: stepperMaximumRating),
						}
					},
					new Line { Style = HR },
					new Label { Style = Description, Text = "RatingView colors." },
					new Line { Style = HR },
					new Label { Text = "Empty background color: " },
					new Picker
					{
						ItemsSource = vm.ColorsForPickers,
						SelectedIndex = vm.ColorPickerEmptyBackgroundSelectedIndex,
						Behaviors =
						{
							new EventToCommandBehavior
							{
								Command = vm.ColorPickerEmptyBackgroundCommand,
								EventName = "SelectedIndexChanged"
							}
						}
					},
					new Label { Text = "Filled background color: " },
					new Picker
					{
						ItemsSource = vm.ColorsForPickers,
						SelectedIndex = vm.ColorPickerFilledBackgroundSelectedIndex,
						Behaviors =
						{
							new EventToCommandBehavior
							{
								Command = vm.ColorPickerFilledBackgroundCommand,
								EventName = "SelectedIndexChanged"
							}
						}
					},
					new Label { Text = "Shape border color:" },
					new Picker
					{
						ItemsSource = vm.ColorsForPickers,
						SelectedIndex = vm.ColorPickerRatingShapeBorderColorSelectedIndex,
						Behaviors =
						{
							new EventToCommandBehavior
							{
								Command = vm.ColorPickerRatingShapeBorderColorCommand,
								EventName = "SelectedIndexChanged"
							}
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
					},
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
					}
				}
			}
		};
	}

	enum Column
	{ Input, Result }

	enum Row
	{
		Star, Circle, Heart, Like, Dislike, Custom
	}

	void RatingViewShapePaddingBottom_ValueChanged(object sender, ValueChangedEventArgs e)
	{
		Thickness currentThickness = vm.RatingViewShapePadding;
		currentThickness.Bottom = e.NewValue;
		vm.RatingViewShapePadding = currentThickness;
	}

	void RatingViewShapePaddingLeft_ValueChanged(object sender, ValueChangedEventArgs e)
	{
		Thickness currentThickness = vm.RatingViewShapePadding;
		currentThickness.Left = e.NewValue;
		vm.RatingViewShapePadding = currentThickness;
	}

	void RatingViewShapePaddingRight_ValueChanged(object sender, ValueChangedEventArgs e)
	{
		Thickness currentThickness = vm.RatingViewShapePadding;
		currentThickness.Right = e.NewValue;
		vm.RatingViewShapePadding = currentThickness;
	}

	void RatingViewShapePaddingTop_ValueChanged(object sender, ValueChangedEventArgs e)
	{
		Thickness currentThickness = vm.RatingViewShapePadding;
		currentThickness.Top = e.NewValue;
		vm.RatingViewShapePadding = currentThickness;
	}

	void StepperMaximumRating_RatingChanged(object? sender, RatingChangedEventArgs e)
	{
		// This is the weak event raised when the rating is changed.  The developer can then perform further actions (such as save to DB).
		if (sender is RatingView ratingView)
		{
			_ = ratingView.Rating;
		}
	}
}