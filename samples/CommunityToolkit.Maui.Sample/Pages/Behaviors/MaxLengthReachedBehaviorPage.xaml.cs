using CommunityToolkit.Maui.Behaviors;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

partial class MaxLengthReachedBehaviorPage : BasePage
{
    public MaxLengthReachedBehaviorPage()
    {
        InitializeComponent();

        NextEntry ??= new Entry();
        MaxLengthSetting ??= new Entry();
        AutoDismissKeyboardSetting ??= new Switch();
    }

    void MaxLengthReachedBehavior_MaxLengthReached(object? sender, MaxLengthReachedEventArgs e)
    {
        NextEntry.Focus();
    }
}