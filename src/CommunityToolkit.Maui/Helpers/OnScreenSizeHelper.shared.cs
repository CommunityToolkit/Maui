using System.Collections.Generic;
using CommunityToolkit.Maui.Categories;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Helpers;
using CommunityToolkit.Maui.Core.Views.OnScreenSize;

namespace CommunityToolkit.Maui.Helpers;

/// <summary>
/// Helper methods for supporting OnScreenSize on code-behind.
/// </summary>
public static class OnScreenSizeHelpers
{
	
	internal static double GetScreenDiagonalInches()
	{
		var dpi =  OnScreenSizePlatform.GetPixelPerInches();
		
		var displayInfo = Microsoft.Maui.Devices.DeviceDisplay.Current.MainDisplayInfo;
	     
		return GetScreenDiagonalInches(displayInfo.Width, displayInfo.Height,displayInfo.Density, dpi.xdpi, dpi.ydpi );
	}
        
	internal static double GetScreenDiagonalInches(double width, double height, double scale, double xDpi, double yDpi)
	{
		var width1 = width;
		var height1 = height;
	        
		var horizontal = width1 / xDpi;
		var vertical = height1 / yDpi;

		var diagonal = Math.Sqrt(Math.Pow(horizontal, 2) + Math.Pow(vertical, 2));

		var diagonalReturnValue = diagonal.RoundUp();

		LogHelpers.WriteLine($"{nameof(OnScreenSizeExtension)} - DiagonalSize: {diagonalReturnValue},  PPI/DPI: x:\"{xDpi}\", y:\"{yDpi}\"", LogLevels.Info);
	        
		return diagonal.RoundUp();
	}

	/// <summary>
	/// OnScreenSize's code behind support.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="defaultSize"></param>
	/// <param name="extraSmall"></param>
	/// <param name="small"></param>
	/// <param name="medium"></param>
	/// <param name="large"></param>
	/// <param name="extraLarge"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentException"></exception>
	public static T OnScreenSize<T>(T defaultSize= default(T)!,
		T extraSmall = default(T)!,
		T small = default(T)!,
		T medium = default(T)!,
		T large = default(T)!,
		T extraLarge = default(T)!)
	{
		var screenSize = ScreenCategoryHelper.GetCategory();
		switch (screenSize)
		{
			case ScreenCategories.ExtraSmall:
				return extraSmall;
			case ScreenCategories.Small:
				return small;
			case ScreenCategories.Medium:
				return medium;
			case ScreenCategories.Large:
				return large;
			case ScreenCategories.ExtraLarge:
				return extraLarge;
		}

		if (EqualityComparer<T>.Default.Equals(defaultSize, default(T)))
		{
			throw new ArgumentException($"{nameof(OnScreenSizeExtension)} markup requires a {nameof(defaultSize)} set.");
		}
		
		return defaultSize;
	}
}