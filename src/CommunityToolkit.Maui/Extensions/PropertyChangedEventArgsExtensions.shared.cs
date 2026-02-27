//inspired from https://github.com/xamarin/Xamarin.Forms/blob/5.0.0/Xamarin.Forms.Platform.Android/CollectionView/PropertyChangedEventArgsExtensions.cs

using System.Runtime.CompilerServices;

namespace CommunityToolkit.Maui.Extensions;

static class PropertyChangedEventArgsExtensions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsOneOf(this string propertyName, params IReadOnlyList<BindableProperty> bindableProperties)
	{
		return bindableProperties.Any(bindableProperty => bindableProperty.PropertyName == propertyName);
	}
}