using CommunityToolkit.WinUI.Notifications;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace CommunityToolkit.Maui.Alerts;

public partial class Toast
{
	private partial void DismissPlatform(CancellationToken token)
	{
		if (PlatformToast is not null)
		{
			token.ThrowIfCancellationRequested();
			ToastNotificationManager.History.Clear();

			PlatformToast.ExpirationTime = DateTimeOffset.Now;
			PlatformToast = null;
		}
	}

	private partial void ShowPlatform(CancellationToken token)
	{
		DismissPlatform(token);
		token.ThrowIfCancellationRequested();
		var toastContentBuilder = new ToastContentBuilder()
			.AddText(Text);

		var toastContent = toastContentBuilder.GetToastContent();
		toastContent.ActivationType = ToastActivationType.Background;

		var xmlDocument = new XmlDocument();
		xmlDocument.LoadXml(toastContent.GetContent());

		PlatformToast = new ToastNotification(xmlDocument)
		{
			ExpirationTime = DateTimeOffset.Now.Add(GetDuration(Duration))
		};

		ToastNotificationManager.CreateToastNotifier().Show(PlatformToast);
	}
}
