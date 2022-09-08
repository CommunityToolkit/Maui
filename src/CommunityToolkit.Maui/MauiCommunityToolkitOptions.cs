using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace CommunityToolkit.Maui;

/// <summary>
/// .NET MAUI Community Toolkit Options.
/// </summary>
public class MauiCommunityToolkitOptions
{
	/// <summary>
	/// Allows to
	/// </summary>
	public static bool ThrowException { get; set; } = true;
	
	[return: NotNullIfNotNull("defaultValue")]
	internal static T? PerformOperation<T>(Func<T?> operation, T? defaultValue)
	{
		if (ThrowException)
		{
			return operation();
		}
		
		try
		{
			return operation();
		}
		catch (Exception ex)
		{
			Debug.WriteLine(ex.Message);
			return defaultValue;
		}
	}
}