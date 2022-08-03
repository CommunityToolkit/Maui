using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Core.Views.OnScreenSize;
using CommunityToolkit.Maui.Extensions;

namespace CommunityToolkit.Maui.Helpers;

/// <summary>
/// Helper methods for supporting OnScreenSize on code-behind.
/// </summary>
public static class OnScreenSizeHelper
{
	
	 internal static ScreenCategories GetCategory()
	{
		if (TryGetCategory(out var category))
		{

			return category;
		}

		return ScreenCategories.NotSet;
	}


	static bool TryGetCategory(out ScreenCategories category)
	{
		if (OnScreenSizeManager.Current.CurrentCategory != null)
		{
			if (OnScreenSizeManager.Current.CurrentCategory.Value != ScreenCategories.NotSet)
			{
				category = OnScreenSizeManager.Current.CurrentCategory.Value;
				return true;
			}
		}
		category = GetCategoryInternal();

		OnScreenSizeManager.Current.CurrentCategory =category;
		return true;
	}

	static ScreenCategories GetCategoryInternal()
	{
#if !(ANDROID || IOS)
		    var defaultCategory = ScreenCategories.ExtraLarge;
		    LogHelpers.Log(string.Format("{0} - Detected platform \"{1}\" assuming \"{2}\"!", nameof(OnScreenSizeExtension), DeviceInfo.Platform.ToString(), defaultCategory));
		    return defaultCategory;
#endif
	    
		var diagonalSize = OnScreenSizeHelper.GetScreenDiagonalInches();
            
		var category = OnScreenSizeManager.Current.Categorizer.GetCategoryByDiagonalSize(OnScreenSizeManager.Current.Mappings, diagonalSize);

		LogHelpers.Log(string.Format("{0} - Current screen category is \"{1}\", and screen diagonal size is \"{2}\"",nameof(OnScreenSizeExtension),category, diagonalSize));
            
		if (category == ScreenCategories.NotSet)
		{
			throw new InvalidOperationException(string.Format("Fail to categorize your current screen. Screen-Diagonal-Size:{0}.", diagonalSize));
		}
		return category;
	}

	 static double GetScreenDiagonalInches()
	{
		OnScreenSizePlatform.TryGetPixelPerInches(out var xdpi, out var ydpi);
		
		var displayInfo = DeviceDisplay.Current.MainDisplayInfo;
	     
		return GetScreenDiagonalInches(displayInfo.Width, displayInfo.Height,xdpi, ydpi );
	}
        
	static double GetScreenDiagonalInches(double width, double height, double xDpi, double yDpi)
	{
		var horizontal = width / xDpi;
		var vertical = height / yDpi;

		var diagonal = Math.Sqrt(Math.Pow(horizontal, 2) + Math.Pow(vertical, 2));

		var diagonalReturnValue = (double)Math.Round((Math.Ceiling(diagonal * 100) / 100), 1);

		LogHelpers.Log($"{nameof(OnScreenSizeExtension)} - DiagonalSize: {diagonalReturnValue},  PPI/DPI: x:\"{xDpi}\", y:\"{yDpi}\"");
	        
		return (double)Math.Round((Math.Ceiling(diagonal * 100) / 100), 1);
	}

	/// <summary>
	/// OnScreenSize's code behind support.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="default"></param>
	/// <param name="extraSmall"></param>
	/// <param name="small"></param>
	/// <param name="medium"></param>
	/// <param name="large"></param>
	/// <param name="extraLarge"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentException"></exception>
	public static object OnScreenSize(object @default,
		object? extraSmall = null,
		object? small = null,
		object? medium = null,
		object? large = null,
		object? extraLarge = null)
	{
		var screenSize = OnScreenSizeHelper.GetCategory();
		
		object? value = null;
		switch (screenSize)
		{
			case ScreenCategories.ExtraSmall:
				value = extraSmall;
				break;
			case ScreenCategories.Small:
				value = small;
				break;
			case ScreenCategories.Medium:
				value =  medium;
				break;
			case ScreenCategories.Large:
				value = large;
				break;
			case ScreenCategories.ExtraLarge:
				value = extraLarge;
				break;
		}

		if (value == null)
		{
			return @default;
		}
		
		return value;
	}
}