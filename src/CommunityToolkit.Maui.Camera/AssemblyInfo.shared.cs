[assembly: XmlnsDefinition(Constants.XamlNamespace, Constants.CommunityToolkitNamespacePrefix + nameof(CommunityToolkit.Maui.Views))]
[assembly: XmlnsDefinition(Constants.XamlNamespace, Constants.CommunityToolkitNamespace)]

[assembly: Microsoft.Maui.Controls.XmlnsPrefix(Constants.XamlNamespace, "toolkit")]

static class Constants
{
	public const string XamlNamespace = "http://schemas.microsoft.com/dotnet/2022/maui/toolkit";
	public const string CommunityToolkitNamespace = $"{nameof(CommunityToolkit)}.{nameof(CommunityToolkit.Maui)}";
	public const string CommunityToolkitNamespacePrefix = $"{CommunityToolkitNamespace}.";
}