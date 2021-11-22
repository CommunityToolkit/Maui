using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Maui.Behaviors.Internals;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// The MaskedBehavior is a behavior that allows the user to define an input mask for data entry. Adding this behavior to an <see cref="InputView"/> (i.e. <see cref="Entry"/>) control will force the user to only input values matching a given mask. Examples of its usage include input of a credit card number or a phone number.
/// </summary>
public class MaskedBehavior : BaseBehavior<InputView>
{
	/// <summary>
	/// Backing BindableProperty for the <see cref="Mask"/> property.
	/// </summary>
	public static readonly BindableProperty MaskProperty =
		BindableProperty.Create(nameof(Mask), typeof(string), typeof(MaskedBehavior), propertyChanged: OnMaskPropertyChanged);

	/// <summary>
	/// Backing BindableProperty for the <see cref="UnMaskedCharacter"/> property.
	/// </summary>
	public static readonly BindableProperty UnMaskedCharacterProperty =
		BindableProperty.Create(nameof(UnMaskedCharacter), typeof(char), typeof(MaskedBehavior), 'X', propertyChanged: OnUnMaskedCharacterPropertyChanged);

	IDictionary<int, char>? positions;

	bool applyingMask;

	/// <summary>
	/// The mask that the input value needs to match. This is a bindable property.
	/// </summary>
	public string? Mask
	{
		get => (string?)GetValue(MaskProperty);
		set => SetValue(MaskProperty, value);
	}

	/// <summary>
	/// The placeholder character for when no input has been given yet. This is a bindable property.
	/// </summary>
	public char UnMaskedCharacter
	{
		get => (char)GetValue(UnMaskedCharacterProperty);
		set => SetValue(UnMaskedCharacterProperty, value);
	}

	static void OnMaskPropertyChanged(BindableObject bindable, object oldValue, object newValue)
		=> ((MaskedBehavior)bindable).SetPositions();

	static void OnUnMaskedCharacterPropertyChanged(BindableObject bindable, object oldValue, object newValue)
		=> ((MaskedBehavior)bindable).OnMaskChanged();

	/// <inheritdoc />
	protected override void OnViewPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		base.OnViewPropertyChanged(sender, e);

		if (e.PropertyName == InputView.TextProperty.PropertyName)
			OnTextPropertyChanged();
	}

	void OnTextPropertyChanged()
	{
		if (applyingMask)
			return;

		applyingMask = true;
		ApplyMask(View?.Text);
		applyingMask = false;
	}

	void SetPositions()
	{
		if (string.IsNullOrEmpty(Mask))
		{
			positions = null;
			return;
		}

		var list = new Dictionary<int, char>();
		if (Mask != null)
		{
			for (var i = 0; i < Mask.Length; i++)
			{
				if (Mask[i] != UnMaskedCharacter)
					list.Add(i, Mask[i]);
			}
		}

		positions = list;
	}

	void OnMaskChanged()
	{
		if (string.IsNullOrEmpty(Mask))
		{
			positions = null;
			return;
		}

		var originalText = RemoveMaskNullableString(View?.Text);
		SetPositions();
		ApplyMask(originalText);
	}

	string? RemoveMaskNullableString(string? text)
	{
		if (text == null || string.IsNullOrEmpty(text))
			return text;

		return RemoveMask(text);
	}

	string RemoveMask(string text)
	{
		var maskChars = positions?
			.Select(c => c.Value)
			.Distinct()
			.ToArray();

		return string.Join(string.Empty, text.Split(maskChars));
	}

	void ApplyMask(string? text)
	{
		if (text != null && !string.IsNullOrWhiteSpace(text) && positions != null)
		{
			if (text.Length > (Mask?.Length ?? 0))
				text = text.Remove(text.Length - 1);

			text = RemoveMask(text);
			foreach (var position in positions)
			{
				if (text.Length < position.Key + 1)
					continue;

				var value = position.Value.ToString();

				// !important - If user types in masked value, don't add masked value
				if (text.Substring(position.Key, 1) != value)
					text = text.Insert(position.Key, value);
			}
		}

		if (View != null)
			View.Text = text;
	}
}