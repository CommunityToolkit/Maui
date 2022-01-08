using CommunityToolkit.WinUI.Notifications;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace CommunityToolkit.Maui.Alerts;

public partial class Toast
{
	static ToastNotification? _nativeToast;

	/// <summary>
	/// Dismiss Toast
	/// </summary>
	public virtual partial Task Dismiss(CancellationToken token)
	{
		if (_nativeToast is null)
		{
			return Task.CompletedTask;
		}

		token.ThrowIfCancellationRequested();
		ToastNotificationManager.History.Clear();

		_nativeToast.ExpirationTime = DateTimeOffset.Now;

		_nativeToast = null;
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

		_nativeToast = new ToastNotification(xmlDocument)
		{
			ExpirationTime = DateTimeOffset.Now.Add(GetDuration(Duration))
		};

		ToastNotificationManager.CreateToastNotifier().Show(_nativeToast);
	}
}
