using System;
using CommunityToolkit.Maui.Categories;
using CommunityToolkit.Maui.Mappings;

namespace  CommunityToolkit.Maui.Mappings;

/// <summary>
/// Size mapping info about the screen diagonal-size and the category it falls into.
/// </summary>
public class SizeMappingInfo
{
	/// <summary>
	/// 
	/// </summary>
	/// <param name="diagonalSize">Diagonal Size to compare against the actual device diagonal size of the device</param>
	/// <param name="category">The category that must be assumed in case the comparison is match</param>
	/// <param name="comparisonMode">How the comparison should be performed</param>
	public SizeMappingInfo(double diagonalSize, ScreenCategories category, ScreenSizeCompareModes comparisonMode)
	{
		DiagonalSize = diagonalSize;
		Category = category;
		ComparisonMode = comparisonMode;
		
		
	}

	
	/// <summary>
	/// Determine how a mapping comparison should be performed against the actual diagonal screen size of the device which is running the code.
	/// </summary>
	public ScreenSizeCompareModes ComparisonMode { get; init; }

	/// <summary>
	/// Physical/actual diagonal size of the screen in inches.
	/// </summary>
	public double DiagonalSize { get; init; }
	
	/// <summary>
	/// The category the screen falls into.
	/// </summary>
	public ScreenCategories Category { get; init; }
	
}