﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
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
	public IEnumerable ViewOrder
	{
		get => (IEnumerable)GetValue(ViewOrderProperty);
		set => SetValue(ViewOrderProperty, value);
	}

	IEnumerable<IView> ISemanticOrderView.ViewOrder => ViewOrder.OfType<IView>();

	/// <summary>
	/// Initialize a new instance of <see cref="SemanticOrderView"/>.
	/// </summary>
	public SemanticOrderView()
	{
	}
}