using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Layouts;

/// <summary>
/// The <see cref="StateView"/> can be used as a template for one of the different states supported by StateLayout.
/// </summary>
public class StateView : ContentView
{
	/// <summary>
	/// Backing BindableProperty for the <see cref="StateKey"/> property.
	/// </summary>
	public static readonly BindableProperty StateKeyProperty
		= BindableProperty.Create(nameof(StateKey), typeof(LayoutState), typeof(StateView), default(LayoutState));

	/// <summary>
	/// Backing BindableProperty for the <see cref="CustomStateKey"/> property.
	/// </summary>
	public static readonly BindableProperty CustomStateKeyProperty
		= BindableProperty.Create(nameof(CustomStateKey), typeof(string), typeof(StateView));

	/// <summary>
	/// Backing BindableProperty for the <see cref="RepeatCount"/> property.
	/// </summary>
	public static readonly BindableProperty RepeatCountProperty
		= BindableProperty.Create(nameof(RepeatCount), typeof(int), typeof(StateView), 1);

	/// <summary>
	/// Backing BindableProperty for the <see cref="Template"/> property.
	/// </summary>
	public static readonly BindableProperty TemplateProperty
		= BindableProperty.Create(nameof(Template), typeof(DataTemplate), typeof(StateView));

	/// <summary>
	/// State key
	/// </summary>
	public LayoutState StateKey
	{
		get => (LayoutState)GetValue(StateKeyProperty);
		set => SetValue(StateKeyProperty, value);
	}

	/// <summary>
	/// Custom State key
	/// </summary>
	public string? CustomStateKey
	{
		get => (string?)GetValue(CustomStateKeyProperty);
		set => SetValue(CustomStateKeyProperty, value);
	}

	/// <summary>
	/// Repeat count
	/// </summary>
	public int RepeatCount
	{
		get => (int)GetValue(RepeatCountProperty);
		set => SetValue(RepeatCountProperty, value);
	}

	/// <summary>
	/// DataTemplate
	/// </summary>
	public DataTemplate? Template
	{
		get => (DataTemplate?)GetValue(TemplateProperty);
		set => SetValue(TemplateProperty, value);
	}
}
