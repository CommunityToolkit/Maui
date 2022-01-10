﻿using CommunityToolkit.Maui.Sample.ViewModels;

namespace CommunityToolkit.Maui.Sample.Pages;

public class MainGalleryPage : BaseGalleryPage<MainGalleryViewModel>
{
	public MainGalleryPage(MainGalleryViewModel mainGalleryViewModel) : base("Samples", mainGalleryViewModel)
	{
	}
}