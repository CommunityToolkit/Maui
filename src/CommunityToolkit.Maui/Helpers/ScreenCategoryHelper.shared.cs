using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Maui.Categories;
using CommunityToolkit.Maui.Extensions;

namespace CommunityToolkit.Maui.Helpers;

[SuppressMessage("Style", "IDE0040:Add accessibility modifiers")]
[SuppressMessage("ReSharper", "UseStringInterpolation")]
internal static class ScreenCategoryHelper
{

    public static ScreenCategories GetCategory()
    {
        if (TryGetCategory(out var category))
        {

            return category;
        }

        return ScreenCategories.NotSet;
    }


    private static bool TryGetCategory(out ScreenCategories category)
    {
        if (Manager.Current.CurrentCategory != null)
        {
            if (Manager.Current.CurrentCategory.Value != ScreenCategories.NotSet)
            {
                category = Manager.Current.CurrentCategory.Value;
                return true;
            }
        }
        category = GetCategoryInternal();

        Manager.Current.CurrentCategory =category;
        return true;
    }

    private static ScreenCategories GetCategoryInternal()
    {
        var diagonalSize = OnScreenSizeHelpers.GetScreenDiagonalInches();
            
        var category = Manager.Current.Categorizer.GetCategoryByDiagonalSize(Manager.Current.Mappings, diagonalSize);

		LogHelpers.WriteLine(string.Format("{0} - Current screen category is \"{1}\", and screen diagonal size is \"{2}\"",nameof(OnScreenSizeExtension),category, diagonalSize), LogLevels.Info);
            
        if (category == ScreenCategories.NotSet)
        {
            throw new InvalidOperationException(string.Format("Fail to categorize your current screen. Screen-Diagonal-Size:{0}.", diagonalSize));
        }
            
        return category;
    }

}
