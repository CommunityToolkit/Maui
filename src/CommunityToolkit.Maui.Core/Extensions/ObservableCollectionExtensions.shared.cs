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
	/// <param name="collection">The items to copy into the new <see cref="ObservableCollection{T}"/>.</param>
	/// <returns>A new <see cref="ObservableCollection{T}"/> containing the items of <paramref name="collection"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="collection"/> is null.</exception>
	public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> collection)
	{
		ArgumentNullException.ThrowIfNull(collection);

		return new ObservableCollection<T>(collection);
	}
}