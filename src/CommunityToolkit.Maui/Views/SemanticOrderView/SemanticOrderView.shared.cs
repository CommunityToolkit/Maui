using System.Collections;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Views;

/// <summary>
/// TBD
/// </summary>
public partial class SemanticOrderView : ContentView, ISemanticOrderView
{
	/// <summary>
	/// TBD
	/// </summary>
	public static readonly BindableProperty ViewOrderProperty =
		BindableProperty.Create(nameof(ViewOrder), typeof(IEnumerable), typeof(SemanticOrderView), Array.Empty<View>());
	
	/// <inheritdoc/>
	public IEnumerable ViewOrder
	{
		get => (IEnumerable)GetValue(ViewOrderProperty);
		set => SetValue(ViewOrderProperty, value);
	}

	/// <summary>
	/// TBD
	/// </summary>
	public SemanticOrderView()
	{
	}
}