using CommunityToolkit.Maui.Converters;
using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class IsInRangeConverterPage : BasePage<IsInRangeConverterViewModel>
{
	readonly IsInRangeConverter isInRangeConverterText = new()
	{
		TrueObject = "TRUE",
		FalseObject = "FALSE",
	};

	readonly IsInRangeConverter isInRangeConverterBackground = new()
	{
		TrueObject = Colors.Green,
		FalseObject = Colors.Red,
	};

	readonly IsInRangeConverterViewModel vm;

	public IsInRangeConverterPage(IsInRangeConverterViewModel viewModel)
		: base(viewModel)
	{
		InitializeComponent();
		vm = viewModel;
		char min = viewModel.CharMin;
		char max = viewModel.CharMax;
		SetCharLabelBindings(viewModel, LabelCharInRange, isInRangeConverterText, Label.TextProperty, min, max);
		SetCharLabelBindings(viewModel, LabelCharInRange, isInRangeConverterBackground, Label.BackgroundProperty, min, max);
		SetCharLabelBindings(viewModel, LabelCharGreater, isInRangeConverterText, Label.TextProperty, min, null);
		SetCharLabelBindings(viewModel, LabelCharGreater, isInRangeConverterBackground, Label.BackgroundProperty, min, null);
		SetCharLabelBindings(viewModel, LabelCharLess, isInRangeConverterText, Label.TextProperty, null, max);
		SetCharLabelBindings(viewModel, LabelCharLess, isInRangeConverterBackground, Label.BackgroundProperty, null, max);
	}

	void CharText_TextChanged(object sender, TextChangedEventArgs e)
	{
		if (vm is null)
		{
			return;
		}

		char min = vm.CharMin;
		char max = vm.CharMax;
		SetCharLabelBindings(vm, LabelCharInRange, isInRangeConverterText, Label.TextProperty, min, max);
		SetCharLabelBindings(vm, LabelCharInRange, isInRangeConverterBackground, Label.BackgroundProperty, min, max);
		SetCharLabelBindings(vm, LabelCharGreater, isInRangeConverterText, Label.TextProperty, min, null);
		SetCharLabelBindings(vm, LabelCharGreater, isInRangeConverterBackground, Label.BackgroundProperty, min, null);
		SetCharLabelBindings(vm, LabelCharLess, isInRangeConverterText, Label.TextProperty, null, max);
		SetCharLabelBindings(vm, LabelCharLess, isInRangeConverterBackground, Label.BackgroundProperty, null, max);
	}

	void SetCharLabelBindings(IsInRangeConverterViewModel vmodel, Label? label, IsInRangeConverter converter, BindableProperty prop, char? min, char? max)
	{
		converter.Min = min;
		converter.Max = max;
		label?.SetBinding(prop, new Binding
		{
			Source = vmodel.CharIsInRange,
			Mode = BindingMode.OneWay,
			Converter = converter,
		});
	}
}