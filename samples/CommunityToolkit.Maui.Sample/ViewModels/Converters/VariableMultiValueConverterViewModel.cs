namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public class VariableMultiValueConverterViewModel : BaseViewModel
{
	bool isAllGroupSwitch1On, isAllGroupSwitch2On, isAnyGroupSwitch1On, isAnyGroupSwitch2On,
		isGreaterThanGroupSwitch1On, isGreaterThanGroupSwitch2On, isGreaterThanGroupSwitch3On, isGreaterThanGroupSwitch4On;

	public bool IsAllGroupSwitch1On
	{
		get => isAllGroupSwitch1On;
		set => SetProperty(ref isAllGroupSwitch1On, value);
	}

	public bool IsAllGroupSwitch2On
	{
		get => isAllGroupSwitch2On;
		set => SetProperty(ref isAllGroupSwitch2On, value);
	}

	public bool IsAnyGroupSwitch1On
	{
		get => isAnyGroupSwitch1On;
		set => SetProperty(ref isAnyGroupSwitch1On, value);
	}

	public bool IsAnyGroupSwitch2On
	{
		get => isAnyGroupSwitch2On;
		set => SetProperty(ref isAnyGroupSwitch2On, value);
	}

	public bool IsGreaterThanGroupSwitch1On
	{
		get => isGreaterThanGroupSwitch1On;
		set => SetProperty(ref isGreaterThanGroupSwitch1On, value);
	}

	public bool IsGreaterThanGroupSwitch2On
	{
		get => isGreaterThanGroupSwitch2On;
		set => SetProperty(ref isGreaterThanGroupSwitch2On, value);
	}

	public bool IsGreaterThanGroupSwitch3On
	{
		get => isGreaterThanGroupSwitch3On;
		set => SetProperty(ref isGreaterThanGroupSwitch3On, value);
	}

	public bool IsGreaterThanGroupSwitch4On
	{
		get => isGreaterThanGroupSwitch4On;
		set => SetProperty(ref isGreaterThanGroupSwitch4On, value);
	}
}