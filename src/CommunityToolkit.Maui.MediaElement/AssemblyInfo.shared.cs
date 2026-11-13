
[assembly: Microsoft.Maui.Controls.XmlnsDefinition(Constants.XamlNamespace, Constants.CommunityToolkitNamespacePrefix + nameof(CommunityToolkit.Maui.Converters))]
[assembly: Microsoft.Maui.Controls.XmlnsDefinition(Constants.XamlNamespace, Constants.CommunityToolkitNamespacePrefix + nameof(CommunityToolkit.Maui.Core))]
[assembly: Microsoft.Maui.Controls.XmlnsDefinition(Constants.XamlNamespace, Constants.CommunityToolkitNamespacePrefix + nameof(CommunityToolkit.Maui.Core.Handlers))]
[assembly: Microsoft.Maui.Controls.XmlnsDefinition(Constants.XamlNamespace, Constants.CommunityToolkitNamespacePrefix + nameof(CommunityToolkit.Maui.Core.Views))]
[assembly: Microsoft.Maui.Controls.XmlnsDefinition(Constants.XamlNamespace, Constants.CommunityToolkitNamespacePrefix + nameof(CommunityToolkit.Maui.Views))]

[assembly: Microsoft.Maui.Controls.XmlnsPrefix(Constants.XamlNamespace, "toolkit")]

class Constants
{
	public const string XamlNamespace = "http://schemas.microsoft.com/dotnet/2022/maui/toolkit";

	public const string CommunityToolkitNamespacePrefix = $"{nameof(CommunityToolkit)}.{nameof(CommunityToolkit.Maui)}.";
}