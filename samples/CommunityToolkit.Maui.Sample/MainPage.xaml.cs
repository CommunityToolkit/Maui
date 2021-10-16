using CommunityToolkit.Maui.Sample.Pages;

namespace CommunityToolkit.Maui.Sample;

public partial class MainPage : BasePage
{
    public MainPage()
    {
        InitializeComponent();

        Page ??= this;
    }
}