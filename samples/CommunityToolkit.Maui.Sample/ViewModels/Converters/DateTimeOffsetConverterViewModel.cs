using System;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public class DateTimeOffsetConverterViewModel : BaseViewModel
{
	DateTimeOffset _theDate = DateTimeOffset.Now;

	public DateTimeOffset TheDate
	{
		get => _theDate;
		set => SetProperty(ref _theDate, value);
	}
}