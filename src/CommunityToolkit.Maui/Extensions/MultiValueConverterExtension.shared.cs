using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace CommunityToolkit.Maui.Extensions.Internals;

/// <inheritdoc />
public class MultiValueConverterExtension : IMarkupExtension<IMultiValueConverter>
{
    /// <inheritdoc />
    public IMultiValueConverter ProvideValue(IServiceProvider serviceProvider)
        => (IMultiValueConverter)this;

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
        => ((IMarkupExtension<IMultiValueConverter>)this).ProvideValue(serviceProvider);
}