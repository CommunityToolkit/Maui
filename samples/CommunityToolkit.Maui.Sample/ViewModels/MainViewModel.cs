﻿using System.Collections.Generic;
using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.Pages.Behaviors;
using CommunityToolkit.Maui.Sample.Pages.Converters;
using Microsoft.Maui.Graphics;

namespace CommunityToolkit.Maui.Sample.ViewModels;

public class MainViewModel : BaseGalleryViewModel
{
    protected override IEnumerable<SectionModel> CreateItems() => new[]
    {
        new SectionModel(typeof(BehaviorsGalleryPage), "Behaviors", Color.FromArgb("#8E8CD8"),
            "Behaviors lets you add functionality to user interface controls without having to subclass them. Behaviors are written in code and added to controls in XAML or code"),

        new SectionModel(typeof(ConvertersGalleryPage), "Converters", Color.FromArgb("#EA005E"),
            "Converters let you convert bindings of a certain type to a different value, based on custom logic"),
    };
}