//inspired from https://github.com/xamarin/Xamarin.Forms/blob/5.0.0/Xamarin.Forms.Platform.Android/CollectionView/PropertyChangedEventArgsExtensions.cs

using System.Runtime.CompilerServices;

namespace CommunityToolkit.Maui.Extensions;

static class PropertyChangedEventArgsExtensions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool Is(this string propertyName, BindableProperty property) =>
		propertyName == property.PropertyName;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsOneOf(this string propertyName, BindableProperty p0, BindableProperty p1)
	{
		return propertyName == p0.PropertyName ||
			propertyName == p1.PropertyName;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsOneOf(this string propertyName, BindableProperty p0, BindableProperty p1, BindableProperty p2)
	{
		return propertyName == p0.PropertyName ||
			propertyName == p1.PropertyName ||
			propertyName == p2.PropertyName;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsOneOf(this string propertyName, BindableProperty p0, BindableProperty p1, BindableProperty p2, BindableProperty p3)
	{
		return propertyName == p0.PropertyName ||
			propertyName == p1.PropertyName ||
			propertyName == p2.PropertyName ||
			propertyName == p3.PropertyName;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsOneOf(this string propertyName, BindableProperty p0, BindableProperty p1, BindableProperty p2, BindableProperty p3, BindableProperty p4)
	{
		return propertyName == p0.PropertyName ||
			propertyName == p1.PropertyName ||
			propertyName == p2.PropertyName ||
			propertyName == p3.PropertyName ||
			propertyName == p4.PropertyName;
	}
}