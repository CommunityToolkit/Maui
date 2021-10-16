using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace CommunityToolkit.Maui.Sample.Pages;

public class BaseNavigationPage : NavigationPage
{
    public BaseNavigationPage(Page page) : base(page)
    {
        BarTextColor = (Color)(Application.Current?.Resources["NavigationBarTextColor"] ?? throw new InvalidOperationException());
        BarBackgroundColor = (Color)((Application.Current?.Resources["PrimaryColor"]) ?? throw new InvalidOperationException());
    }
}
