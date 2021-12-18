using System;
using CommunityToolkit.Maui.Sample.Pages;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Sample.Pages;

public partial class MainPage : BasePage
{
	public MainPage()
	{
		InitializeComponent();

		Page ??= this;

		Padding = new Thickness(20, 0);
		entry ??= new();

		Device.StartTimer(TimeSpan.FromSeconds(20), () =>
		{
			entry.Behaviors.Clear();
			return false;
		});
	}
}