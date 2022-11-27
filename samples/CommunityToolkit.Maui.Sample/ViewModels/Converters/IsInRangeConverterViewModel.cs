namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

using CommunityToolkit.Mvvm.ComponentModel;

public partial class IsInRangeConverterViewModel : BaseViewModel
{
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CharIsInRange))]
	string stringCharInRange = "H";

	public char? CharIsInRange
	{
		get
		{
			var success = char.TryParse(StringCharInRange, out var returnChar);
			if (success)
			{
				return returnChar;
			}

			return (char)0;
		}
	}
}