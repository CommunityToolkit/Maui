using CommunityToolkit.Maui.Converters;

namespace CommunityToolkit.Maui.Extensions;

/// <inheritdoc />
public class MultiValueConverterExtension : IMarkupExtension<ICommunityToolkitMultiValueConverter>
{
	/// <inheritdoc />
	public ICommunityToolkitMultiValueConverter ProvideValue(IServiceProvider serviceProvider)
		=> (ICommunityToolkitMultiValueConverter)this;

	object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
		=> ((IMarkupExtension<ICommunityToolkitMultiValueConverter>)this).ProvideValue(serviceProvider);
}