using CommunityToolkit.WinUI.Notifications;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace CommunityToolkit.Maui.Alerts;

public partial class Toast
{
	private partial void DismissNative(CancellationToken token)
	{
		if (NativeToast is not null)
		{
			token.ThrowIfCancellationRequested();
			ToastNotificationManager.History.Clear();

			NativeToast.ExpirationTime = DateTimeOffset.Now;
			NativeToast = null;
		}
	}

	private partial void ShowNative(CancellationToken token)
	{
		DismissNative(token);
		token.ThrowIfCancellationRequested();
#if WINDOWS10_0_18362_0_OR_GREATER
		if (Environment.OSVersion.Version.Build >= 18362)
		{
#pragma warning disable CA1416 // Validate platform compatibility
			var toastContentBuilder = new ToastContentBuilder()
				.AddText(Text);

			var toastContent = toastContentBuilder.GetToastContent();
			toastContent.ActivationType = ToastActivationType.Background;

			var xmlDocument = new XmlDocument();
			xmlDocument.LoadXml(toastContent.GetContent());

			NativeToast = new ToastNotification(xmlDocument)
			{
				ExpirationTime = DateTimeOffset.Now.Add(GetDuration(Duration))
			};

			ToastNotificationManager.CreateToastNotifier().Show(NativeToast);
		}
#pragma warning restore CA1416 // Validate platform compatibility
#endif
	}
}
