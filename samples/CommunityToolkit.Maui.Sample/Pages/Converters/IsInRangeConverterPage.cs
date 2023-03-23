using CommunityToolkit.Maui.Converters;
using CommunityToolkit.Maui.Markup;
using CommunityToolkit.Maui.Sample.ViewModels.Converters;
using Microsoft.Maui.Controls.Shapes;
using static CommunityToolkit.Maui.Markup.GridRowsColumns;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public class IsInRangeConverterPage : BasePage<IsInRangeConverterViewModel>
{
	public IsInRangeConverterPage(IsInRangeConverterViewModel viewModel) : base(viewModel)
	{
		const int separatorRowHeight = 1;
		const int titleRowHeight = 36;
		const int inputRowHeight = 36;
		const int exampleRowHeight = 62;

		Padding = new Thickness(Padding.HorizontalThickness / 2, 0);

		Content = new ScrollView
		{
			Content = new Grid
			{
				RowSpacing = 12,

				RowDefinitions = Rows.Define(
					(Row.Title, 72),
					(Row.TitleSeparator, separatorRowHeight),
					(Row.StringTitle, titleRowHeight),
					(Row.StringInput, inputRowHeight),
					(Row.StringExample1, exampleRowHeight),
					(Row.StringExample2, exampleRowHeight),
					(Row.StringExample3, exampleRowHeight),
					(Row.StringSeparator, separatorRowHeight),
					(Row.DoubleTitle, titleRowHeight),
					(Row.DoubleInput, inputRowHeight),
					(Row.DoubleExample1, exampleRowHeight),
					(Row.DoubleExample2, exampleRowHeight),
					(Row.DoubleExample3, exampleRowHeight),
					(Row.DoubleSeparator, separatorRowHeight),
					(Row.TimeSpanTitle, titleRowHeight),
					(Row.TimeSpanInput, inputRowHeight),
					(Row.TimeSpanExample1, exampleRowHeight),
					(Row.TimeSpanExample2, exampleRowHeight),
					(Row.TimeSpanExample3, exampleRowHeight),
					(Row.TimeSpanSeparator, separatorRowHeight),
					(Row.CharTitle, titleRowHeight),
					(Row.CharInput, inputRowHeight),
					(Row.CharExample1, exampleRowHeight),
					(Row.CharExample2, exampleRowHeight),
					(Row.CharExample3, exampleRowHeight),
					(Row.CharSeparator, separatorRowHeight)),

				ColumnSpacing = 12,

				ColumnDefinitions = Columns.Define(
					(Column.Input, Stars(3)),
					(Column.Result, Star)),

				Children =
				{
					new Label()
						.Row(Row.Title).ColumnSpan(All<Column>())
						.Text("A converter that allows users to convert a object value into a boolean value, checking if the value is between MinValue and MaxValue, returning true if the value is within the range.")
						.Center().TextCenter(),

					GetSeparator()
						.Row(Row.TitleSeparator).ColumnSpan(All<Column>()),

					new Label()
						.Row(Row.StringTitle).Column(Column.Input)
						.Text("String Input").Font(size: 24, bold: true),

					new Entry { ClearButtonVisibility = ClearButtonVisibility.WhileEditing }
						.Row(Row.StringInput).Column(Column.Input)
						.Start().CenterVertical()
						.Text("Community").Width(200)
						.Assign(out Entry stringInputEntry),

					new ExampleLabel()
						.Row(Row.StringExample1).Column(Column.Input)
						.Text("String Compare Is Equal To or Between \".NET\" and \"Toolkit\""),

					new Label()
						.Row(Row.StringExample1).Column(Column.Result)
						.TextCenter()
						.Bind<Label, Entry, string, string>(
								Label.StyleProperty,
								static (Entry stringInputEntry) => stringInputEntry.Text,
								mode: BindingMode.OneWay,
								converter: new IsInRangeConverter
								{
									TrueObject = GetTrueLabelStyle(),
									FalseObject = GetFalseLabelStyle(),
									MinValue = ".NET",
									MaxValue = "Toolkit"
								},
								source: stringInputEntry),

					new ExampleLabel()
						.Row(Row.StringExample2).Column(Column.Input)
						.Text("String Compare Greater Than or Equal To \"MAUI\""),

					new Label()
						.Row(Row.StringExample2).Column(Column.Result)
						.TextCenter()
						.Bind<Label, Entry, string, string>(
								Label.StyleProperty,
								static (Entry stringInputEntry) => stringInputEntry.Text,
								mode: BindingMode.OneWay,
								converter: new IsInRangeConverter
								{
									TrueObject = GetTrueLabelStyle(),
									FalseObject = GetFalseLabelStyle(),
									MinValue = "MAUI",
								},
								source: stringInputEntry),

					new ExampleLabel()
						.Row(Row.StringExample3).Column(Column.Input)
						.Text("String Commpare Less Than or Equal To \"Community\""),

					new Label()
						.Row(Row.StringExample3).Column(Column.Result)
						.TextCenter()
						.Bind<Label, Entry, string, string>(
								Label.StyleProperty,
								static (Entry stringInputConverter) => stringInputConverter.Text,
								mode: BindingMode.OneWay,
								converter: new IsInRangeConverter
								{
									TrueObject = GetTrueLabelStyle(),
									FalseObject = GetFalseLabelStyle(),
									MaxValue = "Community",
								},
								source: stringInputEntry),

					GetSeparator()
						.Row(Row.StringSeparator).ColumnSpan(All<Column>()),

					new Label()
						.Row(Row.DoubleTitle).Column(Column.Input)
						.Text("Double Input").Font(size: 24, bold: true),

					new Stepper { Increment = 0.5, Minimum = 0, Maximum = 11, Value = 5 }
						.Row(Row.DoubleInput).Column(Column.Input)
						.Start().CenterVertical()
						.Assign(out Stepper doubleInputStepper),

					new ExampleLabel()
						.Row(Row.DoubleExample1).Column(Column.Input)
						.Bind(Label.TextProperty,
								static (Stepper doubleInputStepper) => doubleInputStepper.Value,
								mode: BindingMode.OneWay,
								convert: (double stepperValue) => $"Double Compare {stepperValue} is Equal To or Between 5-10",
								source: doubleInputStepper),

					new Label()
						.Row(Row.DoubleExample1).Column(Column.Result)
						.TextCenter()
						.Bind<Label, Stepper, double, double>(
								Label.StyleProperty,
								static (Stepper doubleInputStepper) => doubleInputStepper.Value,
								mode: BindingMode.OneWay,
								converter: new IsInRangeConverter
								{
									TrueObject = GetTrueLabelStyle(),
									FalseObject = GetFalseLabelStyle(),
									MinValue = 5.0,
									MaxValue = 10.0
								},
								source: doubleInputStepper),

					new ExampleLabel()
						.Row(Row.DoubleExample2).Column(Column.Input)
						.Bind(Label.TextProperty,
								static (Stepper stepper) => stepper.Value,
								mode: BindingMode.OneWay,
								convert: (double stepperValue) => $"Double Compare {stepperValue} is Greater Than or Equal to 5", source: doubleInputStepper),

					new Label()
						.Row(Row.DoubleExample2).Column(Column.Result)
						.TextCenter()
						.Bind<Label, Stepper, double, double>(
								Label.StyleProperty,
								static (Stepper doubleInputStepper) => doubleInputStepper.Value,
								mode: BindingMode.OneWay,
								converter: new IsInRangeConverter
								{
									TrueObject = GetTrueLabelStyle(),
									FalseObject = GetFalseLabelStyle(),
									MinValue = 5.0,
								},
								source: doubleInputStepper),

					new ExampleLabel()
						.Row(Row.DoubleExample3).Column(Column.Input)
						.Bind(Label.TextProperty,
								static (Stepper stepper) => stepper.Value,
								mode: BindingMode.OneWay,
								convert: (double stepperValue) => $"Double Compare {stepperValue} is Less Than or Equal To 10", source: doubleInputStepper),

					new Label()
						.Row(Row.DoubleExample3).Column(Column.Result)
						.TextCenter()
						.Bind<Label, Stepper, double, double>(
								Label.StyleProperty,
								static (Stepper doubleInputStepper) => doubleInputStepper.Value,
								mode: BindingMode.OneWay,
								converter: new IsInRangeConverter
								{
									TrueObject = GetTrueLabelStyle(),
									FalseObject = GetFalseLabelStyle(),
									MaxValue = 10.0,
								},
								source: doubleInputStepper),

					GetSeparator()
						.Row(Row.DoubleSeparator).ColumnSpan(All<Column>()),

					new Label()
						.Row(Row.TimeSpanTitle).Column(Column.Input)
						.Text("TimeSpan Input").Font(size: 24, bold: true),

					new TimePicker { Time = new TimeSpan(12, 0, 0),  }
						.Row(Row.TimeSpanInput).Column(Column.Input)
						.Start().CenterVertical()
						.Assign(out TimePicker timeSpanInputPicker),

					new ExampleLabel()
						.Row(Row.TimeSpanExample1).Column(Column.Input)
						.Text("Timespan Compare is Equal To or Between 0700 - 1700"),

					new Label()
						.Row(Row.TimeSpanExample1).Column(Column.Result)
						.TextCenter()
						.Bind<Label, TimePicker, TimeSpan, TimeSpan>(Label.StyleProperty,
								static (TimePicker timeSpanInputPicker) => timeSpanInputPicker.Time,
								mode: BindingMode.OneWay,
								converter: new IsInRangeConverter
								{
									TrueObject = GetTrueLabelStyle(),
									FalseObject = GetFalseLabelStyle(),
									MinValue = TimeSpan.FromHours(7),
									MaxValue = TimeSpan.FromHours(17)
								},
								source: timeSpanInputPicker),

					new ExampleLabel()
						.Row(Row.TimeSpanExample2).Column(Column.Input)
						.Text("Timespan Compare is Greater Than or Equal To 0700"),

					new Label()
						.Row(Row.TimeSpanExample2).Column(Column.Result)
						.TextCenter()
						.Bind<Label, TimePicker, TimeSpan, TimeSpan>(
								Label.StyleProperty,
								static (TimePicker timeSpanInputPicker) => timeSpanInputPicker.Time,
								mode: BindingMode.OneWay,
								converter: new IsInRangeConverter
								{
									TrueObject = GetTrueLabelStyle(),
									FalseObject = GetFalseLabelStyle(),
									MinValue = TimeSpan.FromHours(7),
								},
								source: timeSpanInputPicker),

					new ExampleLabel()
						.Row(Row.TimeSpanExample3).Column(Column.Input)
						.Text("Timespan Compare is Less Than or Equal To 1700"),

					new Label()
						.Row(Row.TimeSpanExample3).Column(Column.Result)
						.TextCenter()
						.Bind<Label, TimePicker, TimeSpan, TimeSpan>(
								Label.StyleProperty,
								static (TimePicker timeSpanInputPicker) => timeSpanInputPicker.Time,
								mode: BindingMode.OneWay,
								converter: new IsInRangeConverter
								{
									TrueObject = GetTrueLabelStyle(),
									FalseObject = GetFalseLabelStyle(),
									MaxValue = TimeSpan.FromHours(17),
								},
								source: timeSpanInputPicker),

					GetSeparator()
						.Row(Row.TimeSpanSeparator).ColumnSpan(All<Column>()),

					new Label()
						.Row(Row.CharTitle).Column(Column.Input)
						.Text("Char Input").Font(size: 24, bold: true),

					new Entry { MaxLength = 1  }
						.Row(Row.CharInput).Column(Column.Input)
						.Start().CenterVertical()
						.Bind(Entry.TextProperty,
								static (IsInRangeConverterViewModel vm) => vm.InputString,
								static (IsInRangeConverterViewModel vm, string text) => vm.InputString = text),

					new ExampleLabel()
						.Row(Row.CharExample1).Column(Column.Input)
						.Text("Char Compare is Equal To or Between H - L (case sensitive)"),

					new Label()
						.Row(Row.CharExample1).Column(Column.Result)
						.TextCenter()
						.Bind<Label, IsInRangeConverterViewModel, char, char>(
								Label.StyleProperty,
								static (IsInRangeConverterViewModel vm) => vm.InputChar,
								mode: BindingMode.OneWay,
								converter: new IsInRangeConverter
								{
									TrueObject = GetTrueLabelStyle(),
									FalseObject = GetFalseLabelStyle(),
									MinValue = 'H',
									MaxValue = 'L'
								}),

					new ExampleLabel()
						.Row(Row.CharExample2).Column(Column.Input)
						.Text("Char Compare is Greater Than or Equal To 'H' (case sensitive)"),

					new Label()
						.Row(Row.CharExample2).Column(Column.Result)
						.TextCenter()
						.Bind<Label, IsInRangeConverterViewModel, char, char>(
								Label.StyleProperty,
								static (IsInRangeConverterViewModel vm) => vm.InputChar,
								mode: BindingMode.OneWay,
								converter: new IsInRangeConverter
								{
									TrueObject = GetTrueLabelStyle(),
									FalseObject = GetFalseLabelStyle(),
									MinValue = 'H',
								}),

					new ExampleLabel()
						.Row(Row.CharExample3).Column(Column.Input)
						.Text("Char Compare is Less Than or Equal To 'L' (case sensitive)"),

					new Label()
						.Row(Row.CharExample3).Column(Column.Result)
						.TextCenter()
						.Bind<Label, IsInRangeConverterViewModel, char, char>(
								Label.StyleProperty,
								static (IsInRangeConverterViewModel vm) => vm.InputChar,
								mode: BindingMode.OneWay,
								converter: new IsInRangeConverter
								{
									TrueObject = GetTrueLabelStyle(),
									FalseObject = GetFalseLabelStyle(),
									MaxValue = 'L',
								}),

					GetSeparator()
						.Row(Row.CharSeparator).ColumnSpan(All<Column>()),
				}
			}.Center()
			 .Bind(Grid.WidthRequestProperty,
					static (Page page) => page.Width,
					convert: (double input) => input is -1 ? -1 : input - 30, // Work-around for ScrollView width bug
					source: this)
		}.Margin(0).Padding(0);
	}

	enum Row
	{
		Title, TitleSeparator,
		StringTitle, StringInput, StringExample1, StringExample2, StringExample3, StringSeparator,
		DoubleTitle, DoubleInput, DoubleExample1, DoubleExample2, DoubleExample3, DoubleSeparator,
		TimeSpanTitle, TimeSpanInput, TimeSpanExample1, TimeSpanExample2, TimeSpanExample3, TimeSpanSeparator,
		CharTitle, CharInput, CharExample1, CharExample2, CharExample3, CharSeparator,
	}

	enum Column { Input, Result }

	static Style<Label> GetTrueLabelStyle() => new((Label.TextProperty, "True"), (Label.BackgroundColorProperty, Colors.Green));
	static Style<Label> GetFalseLabelStyle() => new((Label.TextProperty, "False"), (Label.BackgroundColorProperty, Colors.Red));

	static Line GetSeparator() => new Line { X2 = 300 }.Center()
									.AppThemeBinding(Line.StrokeProperty, Colors.Black, Colors.White);

	class ExampleLabel : Label
	{
		public ExampleLabel()
		{
			this.Start().TextCenterVertical().Margin(4, 0);

			MaxLines = 2;
			LineBreakMode = LineBreakMode.WordWrap;
		}
	}
}