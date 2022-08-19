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
			bool success = char.TryParse(StringCharInRange, out char returnChar);
			if (success)
			{
				return returnChar;
			}

			return (char)0;
		}
	}
}