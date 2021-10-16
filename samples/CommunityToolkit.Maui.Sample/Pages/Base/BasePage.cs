using System;
using System.Diagnostics;
using System.Windows.Input;
using CommunityToolkit.Maui.Sample.Models;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Sample.Pages;

public abstract class BasePage : ContentPage
{
    public BasePage()
    {
        NavigateCommand = new Command(async parameter =>
        {
            if (parameter != null)
            {
                await Navigation.PushAsync(PreparePage(parameter));
            }
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

    static Page PreparePage(object parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        var model = (SectionModel)parameter;

        var page = (Page)(Activator.CreateInstance(model.Type) ?? throw new ArgumentException("Parameter must of Type Page", nameof(parameter)));
        page.Title = model.Title;

        return page;
    }
}
