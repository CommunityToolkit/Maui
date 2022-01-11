using CommunityToolkit.WinUI.Notifications;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace CommunityToolkit.Maui.Alerts;

public partial class Toast
{
	static ToastNotification? nativeToast;

	/// <summary>
	/// Dismiss Toast
	/// </summary>
	public virtual partial Task Dismiss(CancellationToken token)
	{
		if (nativeToast is null)
		{
			return Task.CompletedTask;
		}

		token.ThrowIfCancellationRequested();
		ToastNotificationManager.History.Clear();

		nativeToast.ExpirationTime = DateTimeOffset.Now;

		nativeToast = null;
		return Task.CompletedTask;
	}

	/// <summary>
	/// Show Toast
	/// </summary>
	public virtual async partial Task Show(CancellationToken token)
	{
		await Dismiss(token);
		token.ThrowIfCancellationRequested();

		var toastContentBuilder = new ToastContentBuilder()
										.AddText(Text);

		var toastContent = toastContentBuilder.GetToastContent();
		toastContent.ActivationType = ToastActivationType.Background;

		var xmlDocument = new XmlDocument();
		xmlDocument.LoadXml(toastContent.GetContent());

		nativeToast = new ToastNotification(xmlDocument)
		{
			ExpirationTime = DateTimeOffset.Now.Add(GetDuration(Duration))
		};

		ToastNotificationManager.CreateToastNotifier().Show(nativeToast);
	}
}
