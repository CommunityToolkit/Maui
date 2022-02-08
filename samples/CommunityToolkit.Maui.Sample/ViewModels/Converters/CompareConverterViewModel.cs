using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;
public class CompareConverterViewModel : BaseViewModel
{
	double sliderValue;

	public double SliderValue
	{
		get => sliderValue;
		set => SetProperty(ref sliderValue, value);
	}


	public CompareConverterViewModel()
	{
		SliderValue = 2;
	}
}
