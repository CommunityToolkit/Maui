namespace CommunityToolkit.Maui.Sample.ViewModels.Converters
{
    public class IntToBoolConverterViewModel : BaseViewModel
	{
		int index;

		public int Number
		{
			get => index;
			set => SetProperty(ref index, value);
		}
	}
}
