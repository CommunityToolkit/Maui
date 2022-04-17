﻿using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class IsNotNullConverterPage : BasePage<IsNotNullConverterViewModel>
{
	public IsNotNullConverterPage(IDeviceInfo deviceInfo, IsNotNullConverterViewModel isNotNullConverterViewModel)
		: base(deviceInfo, isNotNullConverterViewModel)
	{
		InitializeComponent();
	}
}