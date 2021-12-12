using System;
using CommunityToolkit.Maui.Converters;
using Microsoft.Maui.Controls.Xaml;

namespace CommunityToolkit.Maui;

/// <inheritdoc />
public abstract class ValueConverterExtension : IMarkupExtension<ICommunityToolkitValueConverter>
{
	/// <inheritdoc />
	public ICommunityToolkitValueConverter ProvideValue(IServiceProvider serviceProvider)
		=> (ICommunityToolkitValueConverter)this;

	object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
		=> ((IMarkupExtension<ICommunityToolkitValueConverter>)this).ProvideValue(serviceProvider);
}