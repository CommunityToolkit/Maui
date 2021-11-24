using System;
using CommunityToolkit.Maui.Converters;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace CommunityToolkit.Maui.Extensions.Internals;

/// <inheritdoc />
public class MultiValueConverterExtension : IMarkupExtension<ICommunityToolkitMultiValueConverter>
{
	/// <inheritdoc />
	public ICommunityToolkitMultiValueConverter ProvideValue(IServiceProvider serviceProvider)
		=> (ICommunityToolkitMultiValueConverter)this;

	object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
		=> ((IMarkupExtension<ICommunityToolkitMultiValueConverter>)this).ProvideValue(serviceProvider);
}