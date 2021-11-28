using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using CommunityToolkit.Maui.UI.Views;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// This converter can be used with <see cref="StateLayout"/> to determine if a certain state is visible. This can be useful, for instance, in scenarios where you want to show/hide certain elements based on the current <see cref="StateLayout"/>.CurrentState. Additionally a <see cref="StateLayout"/> can be supplied in the parameter of the Convert method to compare against that.
/// </summary>
public class StateToBooleanConverter : ICommunityToolkitValueConverter
{
	/// <summary>
	/// The <see cref="LayoutState"/> to compare to.
	/// </summary>
	public LayoutState StateToCompare { get; set; }

	/// <summary>
	/// Takes the incoming <see cref="StateLayout"/> in <paramref name="value"/> and compares it to <see cref="StateToCompare"/>. If they are equal it returns True, if they are not equal it returns False. Additionally a state to compare against can be provided in <paramref name="parameter"/>.
	/// </summary>
	/// <param name="value">The <see cref="StateLayout"/> to compare.</param>
	/// <param name="targetType">The type of the binding target property. This is not implemented.</param>
	/// <param name="parameter">Optionally, a <see cref="StateLayout"/> can be supplied here to compare against.</param>
	/// <param name="culture">The culture to use in the converter. This is not implemented.</param>
	/// <returns>True if the provided <see cref="StateLayout"/>s match, otherwise False if they don't match.</returns>
	[return: NotNull]
	public object? Convert([NotNull] object? value, Type? targetType, object? parameter, CultureInfo? culture)
	{
		if (value is not LayoutState state)
			throw new ArgumentException("Value is not a valid State", nameof(value));

		if (parameter is LayoutState stateToCompare)
			return state == stateToCompare;

		return state == StateToCompare;
	}

	/// <summary>
	/// This method is not implemented and will throw a <see cref="NotImplementedException"/>.
	/// </summary>
	/// <param name="value">N/A</param>
	/// <param name="targetType">N/A</param>
	/// <param name="parameter">N/A</param>
	/// <param name="culture">N/A</param>
	/// <returns>N/A</returns>
	public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo? culture) => throw new NotImplementedException();
}