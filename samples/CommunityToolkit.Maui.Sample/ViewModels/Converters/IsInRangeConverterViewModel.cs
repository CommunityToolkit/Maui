namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

using CommunityToolkit.Mvvm.ComponentModel;

public partial class IsInRangeConverterViewModel : BaseViewModel
{
	[ObservableProperty]
	char charMin = 'G';

	[ObservableProperty]
	char charMax = 'L';

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CharIsInRange))]
	string stringCharInRange = "H";

	public char CharIsInRange => char.Parse(StringCharInRange);
}