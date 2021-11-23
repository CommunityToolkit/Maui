using System;
using CommunityToolkit.Maui.Converters;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace CommunityToolkit.Maui.Extensions.Internals;

/// <inheritdoc />
public class MultiValueConverterExtension : IMarkupExtension<ICommunityToolkitIMultiValueConverter>
{
	/// <inheritdoc />
	public ICommunityToolkitIMultiValueConverter ProvideValue(IServiceProvider serviceProvider)
		=> (ICommunityToolkitIMultiValueConverter)this;

	object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
		=> ((IMarkupExtension<ICommunityToolkitIMultiValueConverter>)this).ProvideValue(serviceProvider);
}