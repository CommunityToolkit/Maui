namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public class IsNotNullConverterViewModel : BaseViewModel
{
	int? intCheck;
	public int? IntCheck
	{
		get => intCheck;
		set => SetProperty(ref intCheck, value);
	}

	List<string>? listCheck;
	public List<string>? ListCheck
	{
		get => listCheck;
		set => SetProperty(ref listCheck, value);
	}

	string? stringCheck;
	public string? StringCheck
	{
		get => stringCheck;
		set => SetProperty(ref stringCheck, value);
	}

	object? objectCheck;
	public object? ObjectCheck
	{
		get => objectCheck;
		set => SetProperty(ref objectCheck, value);
	}
}