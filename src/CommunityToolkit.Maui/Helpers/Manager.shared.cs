using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using CommunityToolkit.Maui.Categories;
using CommunityToolkit.Maui.Mappings;
using OnScreenSizeMarkup.Maui.Mappings;
#pragma warning disable CS8618
#pragma warning disable CS1591


namespace  CommunityToolkit.Maui.Helpers;

/// <summary>
/// Central point for defining specific settings for the Markup extension.
/// </summary>
[SuppressMessage("Style", "IDE0040:Adicionar modificadores de acessibilidade")]
[SuppressMessage("Style", "IDE1006:Estilos de Nomenclatura")]
public class Manager
{
    private Manager()
    {
    }

    /// <summary>
    /// Gets the singleton instance of this class.
    /// </summary>
    private static readonly Lazy<Manager> _Current = new Lazy<Manager>(() => new Manager());

    /// <summary>
    /// List of mappings that defines which screen diagonal-sizes (in inches) corresponds to a specific <see cref="ScreenCategories"/> and also
    /// how they should be evaluated during runtime in order to correctly classify screens.
    /// </summary>
    /// <remarks>
    /// You can override this values by setting your own mappings.
    /// </remarks>
    public List<SizeMappingInfo> Mappings { get; set; } = DefaultMappings.MobileMappings;
    
    /// <summary>
    /// Display console messages for debugging purposes.
    /// </summary>
    public bool IsLogEnabled { get; set; }

    /// <summary>
    /// When <see cref="IsLogEnabled"/> is true, defines how detailed the log messages should be logged to.
    /// </summary>
    public LogLevels LogLevel { get; set; } = LogLevels.Info;

    /// <summary>
    /// Returns the _Current <see cref="ScreenCategories"/> set for the device.
    /// </summary>
    public ScreenCategories? CurrentCategory { get; internal set; } = null;

    internal IScreenCategorizer Categorizer { get; } = new ScreenCategorizer();

    /// <summary>
    /// Gets the singleton instance of this class.
    /// </summary>
    public static Manager Current => _Current.Value;
    

}

