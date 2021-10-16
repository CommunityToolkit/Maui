﻿using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class UserStoppedTypingBehaviorPage : BasePage
{
    public UserStoppedTypingBehaviorPage()
    {
        InitializeComponent();

        TimeThresholdSetting ??= new Entry();
        AutoDismissKeyboardSetting ??= new Switch();
        MinimumLengthThresholdSetting ??= new Entry();
    }
}