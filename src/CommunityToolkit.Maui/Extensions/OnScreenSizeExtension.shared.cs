using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using CommunityToolkit.Maui.Categories;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Helpers;

namespace CommunityToolkit.Maui.Extensions;

/// <summary>
/// Markup extension that allows specify values to be applied to a physical screen size according to the category
/// the screen it fits in, such as ExtraSmall, Small, Medium, Large, ExtraLarge, or Default.
/// </summary>
[SuppressMessage("Style", "IDE0040:Adicionar modificadores de acessibilidade")]
[SuppressMessage("ReSharper", "UseStringInterpolation")]
public class OnScreenSizeExtension : IMarkupExtension<object>
{
	static readonly object defaultNull = new ();

	private Dictionary<ScreenCategories, object> categoryPropertyValues = new () {
		{ ScreenCategories.ExtraSmall, defaultNull},
		{ ScreenCategories.Small, defaultNull},
		{ ScreenCategories.Medium,  defaultNull},
		{ ScreenCategories.Large,  defaultNull},
		{ ScreenCategories.ExtraLarge,  defaultNull},
	};

	/// <summary>
	/// Xaml internal usage
	/// </summary>
	public OnScreenSizeExtension()
	{
		Default = defaultNull;
	}

	/// <summary>
	/// Default value assumed when the other property values were not provided for the current device. 
	/// </summary>
	public object Default { get; set; }


	/// <summary>
	/// Defines a value used when the screen size is categorized as <see cref="ScreenCategories.ExtraSmall"/>
	/// </summary>
	public object ExtraSmall
	{
		get => categoryPropertyValues[ScreenCategories.ExtraSmall]!;
		set => categoryPropertyValues[ScreenCategories.ExtraSmall] = value;
	}
	/// <summary>
	/// Defines a value used when the screen size is categorized as <see cref="ScreenCategories.Small"/>
	/// </summary>
	public object Small
	{
		get => categoryPropertyValues[ScreenCategories.Small]!;
		set => categoryPropertyValues[ScreenCategories.Small] = value;
	}
	
	/// <summary>
	/// Defines a value used when the screen size is categorized as <see cref="ScreenCategories.Medium"/>
	/// </summary>
	public object Medium
	{
		get => categoryPropertyValues[ScreenCategories.Medium]!;
		set => categoryPropertyValues[ScreenCategories.Medium] = value;
	}

	/// <summary>
	/// Defines a value used when the screen size is categorized as <see cref="ScreenCategories.Large"/>
	/// </summary>
	public object Large
	{
		get => categoryPropertyValues[ScreenCategories.Large]!;
		set => categoryPropertyValues[ScreenCategories.Large] = value;
	}

	/// <summary>
	/// Defines a value used when the screen size is categorized as <see cref="ScreenCategories.ExtraLarge"/>
	/// </summary>
	public object ExtraLarge
	{
		get => categoryPropertyValues[ScreenCategories.ExtraLarge]!;
		set => categoryPropertyValues[ScreenCategories.ExtraLarge] = value;
	}


	/// <summary>
	/// Xaml internal usage
	/// </summary>
	public object ProvideValue(IServiceProvider serviceProvider)
	{
		var valueProvider = serviceProvider?.GetService<IProvideValueTarget>() ?? throw new ArgumentException($"Service provided for OnScreenSize is null");

		BindableProperty bp;
		PropertyInfo pi = null!;
		
		if (valueProvider.TargetObject is Setter setter)
		{
			bp = setter.Property;
		}
		else
		{
			bp = (valueProvider.TargetProperty as BindableProperty)!;
			pi = (valueProvider.TargetProperty as PropertyInfo)!;
		}

		if (Manager.Current.IsLogEnabled)
		{
			LogHelpers.WriteLine($"Providing Value using propertyType:\"{(bp?.ReturnType ?? pi?.PropertyType ?? null)}\" and BindableProperty:{(bp ?? null)}", LogLevels.Verbose);
		}
		
		var propertyType = bp?.ReturnType ?? pi?.PropertyType ?? throw new InvalidOperationException("Não foi posivel determinar a propriedade para fornecer o valor.");

		var value = GetScreenCategoryPropertyValue(serviceProvider);


		return value!.ConvertTo(propertyType, bp!);
	}

	/// <summary>
	/// Gets a value from one of the properties that best suites a <see cref="ScreenCategories"/> a device fits in.
	/// </summary>
	/// <param name="serviceProvider"></param>
	/// <returns></returns>
	/// <exception cref="XamlParseException"></exception>
#pragma warning disable IDE0040
	private object GetScreenCategoryPropertyValue(IServiceProvider serviceProvider)
#pragma warning restore IDE0040
	{
		var screenCategory = ScreenCategoryHelper.GetCategory();
		if (screenCategory != ScreenCategories.NotSet)
		{
			if (categoryPropertyValues[screenCategory] != defaultNull)
			{
				return categoryPropertyValues[screenCategory]!;
			}
		}

		if (Default == defaultNull)
		{
			throw new XamlParseException(string.Format("{0} requires property {0}.{1} defined to use as fallback as property {0}.{2} was not set.",nameof(OnScreenSizeExtension),nameof(Default), screenCategory.ToString()));
		}
		return Default;
	}


}