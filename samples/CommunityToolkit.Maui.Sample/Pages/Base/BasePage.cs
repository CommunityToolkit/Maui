using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Sample.Pages;

public abstract class BasePage : ContentPage
{
	public BasePage()
	{
		Padding = 20;

		NavigateCommand = new AsyncRelayCommand<SectionModel>(parameter => parameter switch
		{
			null => Task.CompletedTask,
			_ => Navigation.PushAsync(PreparePage(parameter))
		});
	}

	public ICommand NavigateCommand { get; }

	protected override void OnAppearing()
	{
		Debug.WriteLine($"OnAppearing: {this}");
	}

	protected override void OnDisappearing()
	{
		Debug.WriteLine($"OnDisappearing: {this}");
	}

	protected static Page PreparePage(SectionModel sectionModel)
	{
		ArgumentNullException.ThrowIfNull(sectionModel);

		var page = (Page)(Activator.CreateInstance(sectionModel.Type) ?? throw new ArgumentException("Invalid SectionModel"));
		page.Title = sectionModel.Title;

		return page;
	}
}