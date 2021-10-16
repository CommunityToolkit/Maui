using System.Diagnostics;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Sample.Pages;

public abstract class BasePage : ContentPage
{
    protected override void OnAppearing()
    {
        Debug.WriteLine($"OnAppearing: {this}");
    }

    protected override void OnDisappearing()
    {
        Debug.WriteLine($"OnDisappearing: {this}");
    }
}
