﻿[assembly: XmlnsDefinition(Constants.XamlNamespace, Constants.CommunityToolkitNamespacePrefix + nameof(CommunityToolkit.Maui.Alerts))]
[assembly: XmlnsDefinition(Constants.XamlNamespace, Constants.CommunityToolkitNamespacePrefix + nameof(CommunityToolkit.Maui.Behaviors))]
[assembly: XmlnsDefinition(Constants.XamlNamespace, Constants.CommunityToolkitNamespacePrefix + nameof(CommunityToolkit.Maui.Converters))]

class Constants
{
	public const string XamlNamespace = "http://schemas.microsoft.com/dotnet/2022/maui/tookit";

	public const string CommunityToolkitNamespacePrefix = nameof(CommunityToolkit) + "." + nameof(CommunityToolkit.Maui) + ".";
}