﻿using CommunityToolkit.Maui.Behaviors;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors
{
    public partial class MaxLengthReachedBehaviorPage : BasePage
    {
        public MaxLengthReachedBehaviorPage()
        {
            InitializeComponent();
        }

        void MaxLengthReachedBehavior_MaxLengthReached(object? sender, MaxLengthReachedEventArgs e)
        {
            nextEntry.Focus();
        }
    }
}