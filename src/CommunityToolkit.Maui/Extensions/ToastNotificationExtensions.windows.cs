using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace CommunityToolkit.Maui.Extensions;

/// <summary>
/// Toast Notification Extensions
/// </summary>
public static class ToastNotificationExtensions
{
	/// <summary>
	/// Build Toast Text Notification
	/// </summary>
	/// <param name="text">Notification text</param>
	/// <returns><see cref="XmlDocument"/></returns>
	public static XmlDocument BuildToastNotificationContent(string text)
	{
		var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText01);
		toastXml.GetElementsByTagName("text").SetContentText(text);
		toastXml.SelectSingleNode("/toast").SetActivationType(true);
		return toastXml;
	}

	/// <summary>
	/// Build Toast Text Notification with actions
	/// </summary>
	/// <param name="text">Notification text</param>
	/// <param name="actionText">Action Button text</param>
	/// <returns><see cref="XmlDocument"/></returns>
	public static XmlDocument BuildToastNotificationContent(string text, string actionText)
	{
		var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText01);
		toastXml.GetElementsByTagName("text").SetContentText(text);
		var toastElement = toastXml.SelectSingleNode("/toast");
		toastElement.SetActivationType(true);
		toastElement.SetAction(actionText);
		return toastXml;
	}

	static void SetActivationType(this IXmlNode xmlNode, bool isBackground)
	{
		var activationTypeAttribute = xmlNode.OwnerDocument.CreateAttribute("activationType");
		activationTypeAttribute.Value = isBackground ? "background" : "foreground";
		xmlNode.Attributes.SetNamedItem(activationTypeAttribute);
	}

	static void SetContentText(this XmlNodeList xmlNodes, string text)
	{
		xmlNodes.ForEach(node => node.InnerText = text);
	}

	static void SetAction(this IXmlNode xmlNode, string text)
	{
		var actions = xmlNode.OwnerDocument.CreateElement("actions");
		xmlNode.AppendChild(actions);
		var action = xmlNode.OwnerDocument.CreateElement("action");
		actions.AppendChild(action);
		action.SetAttribute("content", text);
		action.SetAttribute("arguments", text);
	}
}