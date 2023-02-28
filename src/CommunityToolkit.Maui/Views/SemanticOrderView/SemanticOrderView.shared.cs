using System.Collections;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Views;

/// <inheritdoc cref="ISemanticOrderView"/>
public class SemanticOrderView : ContentView, ISemanticOrderView
{
	/// <summary>
	/// Backing BindableProperty for the <see cref="ViewOrder"/> property.
	/// </summary>
	public static readonly BindableProperty ViewOrderProperty =
		BindableProperty.Create(nameof(ViewOrder), typeof(IEnumerable), typeof(SemanticOrderView), Enumerable.Empty<View>());

	/// <inheritdoc />
	public IEnumerable<IView> ViewOrder
	{
		get => (IEnumerable<IView>)GetValue(ViewOrderProperty);
		set => SetValue(ViewOrderProperty, value);
	}
}