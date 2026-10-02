using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Layouts;

/// <summary>
/// The <see cref="StateView"/> attached properties can be used on any <see cref="IView"/> inheriting element to represent a state inside a <see cref="StateContainer.StateViewsProperty"/>.
/// </summary>
[AttachedBindableProperty<string>("StateKey", DefaultValue = StateViewDefaults.StateKey)]
public static partial class StateView
{
}