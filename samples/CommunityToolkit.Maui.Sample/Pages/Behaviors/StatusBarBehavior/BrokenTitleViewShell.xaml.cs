using System;
using System.Diagnostics;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class BrokenTitleViewShell : Shell
{
	bool hasNavigatedToTabbar;

	public BrokenTitleViewShell()
	{
		InitializeComponent();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		if (hasNavigatedToTabbar)
		{
			return;
		}

		hasNavigatedToTabbar = true;

		try
		{
			await GoToAsync("//Tabbar");
		}
		catch (Exception exception)
		{
			hasNavigatedToTabbar = false;
			Trace.WriteLine(exception);
		}
	}
}