using System.Collections.ObjectModel;

namespace CommunityToolkit.Maui.Core.Extensions;

/// <summary>
/// <see cref="ObservableCollection{T}"/> extensions
/// </summary>
public static class ObservableCollectionExtensions
{
	/// <summary>
	/// Create new <see cref="ObservableCollection{T}"/> from <see cref="IEnumerable{T}"/>
	/// </summary>
	public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> collection)
	{
		return new ObservableCollection<T>(collection);
	}
}