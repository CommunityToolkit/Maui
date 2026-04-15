using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public partial class EnumDescriptionConverterViewModel : BaseViewModel
{
    [ObservableProperty]
	public partial ModeName SelectedMode { get; set; }

	public EnumDescriptionConverterViewModel()
	{
		SelectedMode = ModeName.DarkMode;
	}
}

public enum ModeName
{
	//  No Description needed for one word enum members that
	//  are spelled the way you want to display them

	[Description("Light Mode")] // Can Use Description attribute
	LightMode,
	[Display(Name = "Dark Mode")] // Or Display attribute with Name property
	DarkMode,
	System
}