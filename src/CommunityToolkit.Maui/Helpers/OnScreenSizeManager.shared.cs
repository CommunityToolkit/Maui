using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Maui.Categories;
using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Mappings;
using Microsoft.Extensions.Logging;
using OnScreenSizeMarkup.Maui.Mappings;

namespace  CommunityToolkit.Maui.Helpers;

/// <summary>
/// Central point for defining specific settings for the Markup extension.
/// </summary>
public class OnScreenSizeManager
{
     OnScreenSizeManager()
    {
    }

    /// <summary>
    /// Gets the singleton instance of this class.
    /// </summary>
     static readonly Lazy<OnScreenSizeManager> current = new (() => new OnScreenSizeManager());

    /// <summary>
    /// List of mappings that defines which screen diagonal-sizes (in inches) corresponds to a specific <see cref="ScreenCategories"/> and also
    /// how they should be evaluated during runtime in order to correctly classify screens.
    /// </summary>
    /// <remarks>
    /// You can override this values by setting your own mappings.
    /// </remarks>
    public List<SizeMappingInfo> Mappings { get; set; } = DefaultMappings.MobileMappings;

    /// <summary>
    /// Returns the _Current <see cref="ScreenCategories"/> set for the device.
    /// </summary>
    public ScreenCategories? CurrentCategory { get; internal set; }

    internal IScreenCategorizer Categorizer { get; } = new ScreenCategorizer();

    /// <summary>
    /// Gets the singleton instance of this class.
    /// </summary>
    public static OnScreenSizeManager Current => current.Value;
    

}

