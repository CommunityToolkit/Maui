using System.Collections;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Views;

/// <inheritdoc cref="ISemanticOrderView"/>
public partial class SemanticOrderView : ContentView, ISemanticOrderView
{
	/// <summary>
	/// Gets or sets the collection of child views that defines their order for accessibility and navigation purposes.
	/// </summary>
	/// <remarks>
	/// <para>On iOS, Mac Catalyst and Android 5.1 (API 22) or later, the order of views in this collection determines how assistive
	/// technologies, such as screen readers, traverse the child elements. If not set, the default order is used. Modifying this property
	/// allows customization of the navigation sequence for improved accessibility.</para>
	/// <para>Child views that are not in this collection remain accessible. On iOS and Mac Catalyst, assistive technologies
	/// visit them after the views in this collection, in the order of the visual tree.</para>
	/// <para>On Windows, this collection sets the keyboard Tab order of the views. Narrator's scan order follows the visual tree, so this
	/// collection does not change it.</para>
	/// </remarks>
	[BindableProperty]
	public partial IEnumerable<IView> ViewOrder { get; set; } = [];
}