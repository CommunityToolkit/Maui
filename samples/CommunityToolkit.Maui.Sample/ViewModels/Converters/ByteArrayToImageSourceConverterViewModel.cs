using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public sealed partial class ByteArrayToImageSourceConverterViewModel(HttpClient client) : BaseViewModel, IDisposable
{
	readonly WeakEventManager imageDownloadFailedEventManager = new();
	readonly HttpClient client = client;

	[ObservableProperty, NotifyCanExecuteChangedFor(nameof(DownloadDotNetBotImageCommand))]
	public partial bool IsDownloadingImage { get; set; }

	[ObservableProperty]
	public partial byte[]? DotNetBotImageByteArray { get; private set; }

	[ObservableProperty]
	public partial string LabelText { get; private set; } = "Tap the Download Image Button to download an Image as a byte[]";

	public event EventHandler<string> ImageDownloadFailed
	{
		add => imageDownloadFailedEventManager.AddEventHandler(value);
		remove => imageDownloadFailedEventManager.RemoveEventHandler(value);
	}

	public void Dispose()
	{
		DotNetBotImageByteArray = null;
	}

	bool CanDownloadDotNetBotImageCommandExecute => !IsDownloadingImage && DotNetBotImageByteArray is null;

	[RelayCommand(CanExecute = nameof(CanDownloadDotNetBotImageCommandExecute))]
	async Task DownloadDotNetBotImage(CancellationToken token)
	{
		IsDownloadingImage = true;

		var maximumDownloadTime = TimeSpan.FromSeconds(5);
		var maximumDownloadTimeCTS = new CancellationTokenSource(maximumDownloadTime);

		// Ensure Activity Indicator appears on screen for a minimum of 1.5 seconds when the user taps the Download Button
		var minimumDownloadTime = TimeSpan.FromSeconds(1.5);
		var minimumDownloadTimeTask = Task.Delay(minimumDownloadTime, maximumDownloadTimeCTS.Token).WaitAsync(token);

		try
		{
			const string dotnetBotImageUrl = "https://user-images.githubusercontent.com/13558917/137551073-ac8958bf-83e3-4ae3-8623-4db6dce49d02.png";
			DotNetBotImageByteArray = await client.GetByteArrayAsync(dotnetBotImageUrl, maximumDownloadTimeCTS.Token).WaitAsync(token).ConfigureAwait(false);

			await minimumDownloadTimeTask.ConfigureAwait(false);

			LabelText = "The above image was downloaded as a byte[]";
		}
		catch (Exception e)
		{
			Trace.TraceError("Error downloading image: {0}", e);
			OnImageDownloadFailed(e.Message);
		}
		finally
		{
			IsDownloadingImage = false;
		}
	}

	void OnImageDownloadFailed(in string message) => imageDownloadFailedEventManager.HandleEvent(this, message, nameof(ImageDownloadFailed));
}